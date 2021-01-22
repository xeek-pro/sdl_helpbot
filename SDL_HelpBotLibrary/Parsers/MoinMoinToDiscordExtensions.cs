using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SDL_HelpBotLibrary.Parsers
{
    public static class MoinMoinToDiscordExtensions
    {
        private const string CATEGORY_SEPARATOR = "----";
        private const string CODEBLOCK_PREFIX = "{{{";
        private const string CODEBLOCK_SUFFIX = "}}}";
        private const string CODEBLOCK_LANGUAGE_KEYWORD = "#!highlight";
        private const string DISCORD_CODEBLOCK_WRAPPER = "```";
        private const string LINK_PREFIX = "[[";
        private const string LINK_SUFFIX = "]]";

        private static string DetectLineEnding(this string text)
        {
            return text.Any(x => x == '\r') ? "\r\n" : "\n";
        }

        private static List<(string Part, bool Ignore)> SeparateIntoPartsBasedOnIgnoredRanges(string text, List<Range> ranges)
        {
            if(ranges == default || !ranges.Any())
            {
                return new List<(string Part, bool Ignore)>() { (text, false) };
            }

            var textParts = new List<(string Part, bool Ignore)>();
            StringBuilder partBuilder = new StringBuilder();
            bool lastIgnore = false;

            for (int n = 0; n < text.Length; n++)
            {
                partBuilder.Append(text[n]);

                bool ignore = ranges.Any(range => n >= range.Start.Value && n <= range.End.Value);

                if (lastIgnore != ignore || n == text.Length - 1)
                {
                    textParts.Add((partBuilder.ToString(), lastIgnore));
                    partBuilder.Clear();
                }

                lastIgnore = ignore;
            }

            return textParts;
        }

        public static string RemoveMoinMoinMacros(this string text, List<Range> ignoreRanges = default)
        {
            if (ignoreRanges != default)
            {
                var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text, ignoreRanges);
                return string.Join(null, textParts.Select(x => x.Ignore ? x.Part : x.Part.RemoveMoinMoinMacros()));
            }
            else
            {
                return Regex.Replace(text, @"<<([^>>]*)>>", string.Empty);
            }
        }

        public static string GenerateEmojis(this string text, List<Range> ignoreRanges = default)
        {
            if (ignoreRanges != default)
            {
                var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text, ignoreRanges);
                return string.Join(null, textParts.Select(x => x.Ignore ? x.Part : x.Part.GenerateEmojis()));
            }
            else
            {
                return text
                    .Replace(@"X -(", ":angry:")
                    .Replace(@":(", ":frowning:")
                    .Replace(@";)", ":wink:")
                    .Replace(@":-?", ":stuck_out_tongue:")
                    .Replace(@":-(", ":frowning:")
                    .Replace(@";-)", ":wink:")
                    .Replace(@"{X}", ":error:")
                    .Replace(@"{1}", ":one:")
                    .Replace(@"{2}", ":two:")
                    .Replace(@"{3}", ":three:")
                    .Replace(@"{i}", ":information_source:")
                    .Replace(@"{OK}", ":thumbsup:")
                    .Replace(@"(!)", ":bulb:")
                    .Replace(@"{o}", ":star:")
                    .Replace(@"<!>", ":bangbang:")
                    .Replace(@"/!\", ":warning:");
            }
        }

        public static string GenerateDiscordCodeBlocks(this string text) => GenerateDiscordCodeBlocks(text, out _);

        public static string GenerateDiscordCodeBlocks(this string text, out List<Range> rangesAfterFormatting)
        {
            rangesAfterFormatting = new List<Range>();

            var prefixRegex = new Regex(@$"(?={CODEBLOCK_PREFIX})");
            var suffixRegex = new Regex(@$"(?<={CODEBLOCK_SUFFIX})");

            var blocks = 
                prefixRegex.Split(text)
                .Select(x => suffixRegex.Split(x, 2))
                .SelectMany(x => x);

            StringBuilder result = new StringBuilder();
            int currentPosition = 0;
            foreach (var block in blocks)
            {
                string formattedText = block;

                if(block.StartsWith(CODEBLOCK_PREFIX) && block.EndsWith(CODEBLOCK_SUFFIX))
                {
                    string highlightLanguage = string.Empty;
                    formattedText = block.Replace(CODEBLOCK_PREFIX, "").Replace(CODEBLOCK_SUFFIX, "").Trim();

                    // Get the syntax highlighting language if it exists:
                    var codeBlockSplit = formattedText.Split(null, 3);
                    if (codeBlockSplit.Length > 2 && string.Compare(codeBlockSplit[0], CODEBLOCK_LANGUAGE_KEYWORD, ignoreCase: true) == 0)
                    {
                        highlightLanguage = codeBlockSplit[1];
                        formattedText = codeBlockSplit[2];
                    }

                    formattedText = formattedText.Replace($"{CODEBLOCK_LANGUAGE_KEYWORD} {highlightLanguage}", "", ignoreCase: true, null).Trim();
                    formattedText = 
                        $"{DISCORD_CODEBLOCK_WRAPPER}{highlightLanguage.ToLower()}" + Environment.NewLine + 
                        $"{formattedText}" + Environment.NewLine +
                        $"{DISCORD_CODEBLOCK_WRAPPER}";

                    rangesAfterFormatting.Add(currentPosition..(currentPosition + formattedText.Length));
                    
                }

                result.Append(formattedText);
                currentPosition = result.Length;
            }

            return result.ToString();
        }

        public static string GenerateDiscordTables(this string text)
        {
            const string moinMoinTableStarter = "||";
            string detectedLineEnding = text.Any(x => x == '\r') ? "\r\n" : "\n";

            // Only handle text that's detected to have columns in it:
            if (text.Contains(moinMoinTableStarter))
            {
                // Split into lines
                var lines = text.Replace("\r", "").Split('\n');

                // For each line, separate the columns, then assume the last item is the description, 
                // and all items before it to be the parameter's name and (or) type:
                var result = lines.Select(line =>
                {
                    // Only hanndle lines that are part of tables:
                    if (!line.TrimStart().StartsWith(moinMoinTableStarter)) return line;
                    line = line.Trim();

                    var columns = line.Split(moinMoinTableStarter, StringSplitOptions.RemoveEmptyEntries);
                    var param = columns.Reverse().Skip(1).Reverse().Select(x => x
                        .Replace("'''", "")         // Get rid of the ''' moinmoin bold formatting
                        .Replace(" **", "\\*\\*")   // Ensure C/C++ double pointers are attached to the type
                        .Replace(" *", "\\*"));     // Ensure C/C++ single pointers are attached to the type
                    var paramName = param.Last();
                    var paramType = string.Join(' ', param.SkipLast(1));
                    var desc = columns.LastOrDefault() ?? line.Replace(moinMoinTableStarter, "");

                    // Both the parameter type and name exists:
                    if (!string.IsNullOrEmpty(paramType) && !string.IsNullOrEmpty(paramName))
                        return $"**` {paramType} `**" + $"*` {paramName} `* - {desc ?? string.Empty}";
                    
                    // Only has the parameter name:
                    else if (string.IsNullOrEmpty(paramType) && !string.IsNullOrEmpty(paramName))
                        return $"**`{paramName}`** - {desc ?? string.Empty}";

                    else return $"*{desc}*";
                });

                return string.Join(detectedLineEnding, result);
            }
           
            return text;
        }

        public static string GenerateDiscordLinks(this string text, string defaultBaseUrl, List<Range> ignoreRanges = default) 
            => GenerateDiscordLinks(text, defaultBaseUrl == default ? default : new Uri(defaultBaseUrl, UriKind.Absolute), ignoreRanges);

        public static string GenerateDiscordLinks(this string text, Uri defaultBaseUri = default, List<Range> ignoreRanges = default)
        {
            if (ignoreRanges != default)
            {
                var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text, ignoreRanges);
                return string.Join(null, textParts.Select(x => x.Ignore ? x.Part : x.Part.GenerateDiscordLinks(defaultBaseUri)));
            }
            else
            {
                string prefix = Regex.Escape(LINK_PREFIX);
                string suffix = Regex.Escape(LINK_SUFFIX).Replace("]", "\\]"); // Regex.Escape doesn't escape ] as it should

                var matches = Regex.Matches(text, $@"{prefix}([^{suffix}]*){suffix}");
                foreach (Match match in matches.Reverse()) // Going in reverse to preserve index locations
                {
                    var foundRange = new Range(match.Index, match.Index + match.Length);
                    var originalText = match.Value;
                    var formattedMatchValue = match.Value.Replace(LINK_PREFIX, string.Empty).Replace(LINK_SUFFIX, "");
                    var parts = formattedMatchValue.Split(new char[] { '|' }, 2);
                    var link = parts[0];
                    var linkText = parts.Length > 1 ? parts[1] : parts[0];

                    // First try to assume it's a fully qualified URI:
                    if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
                    {
                        // Or assume it's a relative URI:
                        if (defaultBaseUri != default) Uri.TryCreate(defaultBaseUri, link, out uri);
                    }

                    string replacement = uri == default ? linkText : $"[{linkText}]({uri})";
                    var textBefore = text[..foundRange.Start].TrimEnd('.'); // Sometimes links start with a . but discord doesn't support bulleted lists
                    var textAfter = text[(foundRange.End.Value)..];

                    text = textBefore + replacement + textAfter;
                }

                return text;
            }
        }

        public static string GenerateBoldAndItalicText(this string text, List<Range> ignoreRanges = default)
        {
            if (ignoreRanges != default)
            {
                var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text, ignoreRanges);
                return string.Join(null, textParts.Select(x => x.Ignore ? x.Part : x.Part.GenerateBoldAndItalicText()));
            }
            else
            {
                return text.Replace("'''", "**").Replace("''", "_");
            }
        }

        public static Dictionary<string, string> SplitMoinMoinSections(this string text, bool includeCategories = true)
        {
            // Remove categories:
            string textWithCategoriesRemoved = text;
            int indexOfCategoriesStart = textWithCategoriesRemoved.LastIndexOf(CATEGORY_SEPARATOR);
            if (indexOfCategoriesStart >= 0)
            {
                textWithCategoriesRemoved = textWithCategoriesRemoved[..indexOfCategoriesStart];
            }

            var result = new Dictionary<string, string>();
            var split = textWithCategoriesRemoved.Trim().Split("\n=", StringSplitOptions.RemoveEmptyEntries);

            foreach(var section in split)
            {
                var parts = section.TrimStart().Split('\n', 2);
                result.Add(parts[0].Replace("=", "").Trim(), parts[1].Trim());
            }

            if(includeCategories)
            {
                var categories = text.GetMoinMoinCategorySection();
                if(!string.IsNullOrWhiteSpace(categories)) result.Add("Categories", categories);
            }

            return result;
        }

        public static string GetMoinMoinCategorySection(this string text)
        {
            int indexOfCategoriesStart = text.LastIndexOf(CATEGORY_SEPARATOR);
            if (indexOfCategoriesStart >= 0)
            {
                return text[(indexOfCategoriesStart + CATEGORY_SEPARATOR.Length)..].Trim();
            }

            return string.Empty;
        }
    }
}
