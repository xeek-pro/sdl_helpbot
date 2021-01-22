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

        private struct TextUnit
        {
            public TextUnit(string text, bool ignore = false)
            {
                Text = text;
                Ignore = ignore;
            }

            public string Text { get; }
            public bool Ignore { get; }
        }

        private static List<(string Part, bool Ignore)> SeparateIntoPartsBasedOnIgnoredRanges(string text)
        {
            const string codeBlockPrefix = "{{{";
            const string codeBlockSuffix = "}}}";
            var codeBlockMatches = Regex.Matches(text, $@"{Regex.Escape(codeBlockPrefix)}([^{Regex.Escape(codeBlockSuffix)}]*){Regex.Escape(codeBlockSuffix)}");

            var textParts = new List<(string Part, bool Ignore)>();
            string part = string.Empty;
            bool lastIgnore = false;
            for (int n = 0; n < text.Length; n++)
            {
                part += text[n];
                bool ignore = codeBlockMatches.Any(match => n >= match.Index && n <= match.Index + match.Length);

                if (ignore != lastIgnore || n == text.Length - 1)
                {
                    textParts.Add((part, lastIgnore));
                    part = string.Empty;
                }

                lastIgnore = ignore;
            }

            return textParts;
        }

        public static string RemoveMoinMoinMacros(this string text)
        {
            var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text);

            return string.Join(null, textParts.Select(x =>
                !x.Ignore ?
                Regex.Replace(x.Part, @"<<([^>>]*)>>", string.Empty) :
                x.Part
            ));
        }

        public static string GenerateEmojis(this string text)
        {

            var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text);

            return string.Join(null, textParts.Select(x =>
                !x.Ignore ?
                x.Part
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
                    .Replace(@"/!\", ":warning:") 
                : x.Part
            ));
        }

        public static string GenerateDiscordCodeBlocks(this string text) => GenerateDiscordCodeBlocks(text, out _);

        public static string GenerateDiscordCodeBlocks(this string text, out List<Range> foundRanges)
        {
            foundRanges = new List<Range>();
            //<<([^>>]*)>>

            // Syntax Highlighting:
            const string prefix = "{{{";
            const string suffix = "}}}";
            const string languageCommand = "#!highlight";
            const string codeBlockWrap = "```";
            string detectedLineEnding = text.Any(x => x == '\r') ? "\r\n" : "\n";

            int current_pos = 0, prefix_pos = 0, suffix_pos = 0;
            while ((current_pos = text.IndexOf(prefix, current_pos)) != -1)
            {
                prefix_pos = current_pos;
                if ((suffix_pos = text.IndexOf(suffix, prefix_pos + prefix.Length)) != -1)
                {
                    foundRanges.Add(prefix_pos..suffix_pos);
                    current_pos = suffix_pos + suffix.Length;
                }
                else break;
            }

            var matches = Regex.Matches(text, $@"{Regex.Escape(prefix)}([^{Regex.Escape(suffix)}]*){Regex.Escape(suffix)}", RegexOptions.Multiline | RegexOptions.ECMAScript);
            foreach(Match match in matches.Reverse()) // Going in reverse to preserve index locations
            {
                if (match.Groups.Count < 2) continue;

                // Remember the match range and group value:
                var foundRange = new Range(match.Index, match.Index + match.Length);
                var textBlock = match.Groups[1].Value;

                // Get the language if there is one:
                string language = default;
                if(textBlock.StartsWith(languageCommand))
                {
                    var highlightSyntax = textBlock.Split(separator: null, count: 3);
                    if (highlightSyntax.Length > 1) language = highlightSyntax[1];
                    else language = string.Empty;
                    
                    textBlock = textBlock.Replace(languageCommand, "").TrimStart();
                    textBlock = textBlock[language.Length..].TrimStart();
                }

                textBlock = $"{codeBlockWrap}{(language != default ? language + detectedLineEnding : "")}{textBlock}{codeBlockWrap}";
                var textBefore = text[..foundRange.Start];
                var textAfter = text[(foundRange.End.Value)..];

                foundRanges.Add(new Range(textBefore.Length, textBefore.Length + textBlock.Length));

                text = textBefore + textBlock + textAfter;
            }

            return text;
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

        public static string GenerateDiscordLinks(this string text, string defaultBaseUrl) 
            => GenerateDiscordLinks(text, defaultBaseUrl == default ? default : new Uri(defaultBaseUrl, UriKind.Absolute));

        public static string GenerateDiscordLinks(this string text, Uri defaultBaseUri = default)
        {
            string prefix = Regex.Escape("[[");
            string suffix = Regex.Escape("]]").Replace("]", "\\]"); // Regex.Escape doesn't escape ] as it should

            var matches = Regex.Matches(text, $@"{prefix}([^{suffix}]*){suffix}");
            foreach (Match match in matches.Reverse()) // Going in reverse to preserve index locations
            {
                var foundRange = new Range(match.Index, match.Index + match.Length);
                var originalText = match.Value;
                var formattedMatchValue = match.Value.Replace("[[", string.Empty).Replace("]]", "");
                var parts = formattedMatchValue.Split(new char[] { '|' }, 2);
                var link = parts[0];
                var linkText = parts.Length > 1 ? parts[1] : parts[0];

                // First try to assume it's a fully qualified URI:
                if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
                {
                    // Or assume it's a relative URI:
                    if(defaultBaseUri != default) Uri.TryCreate(defaultBaseUri, link, out uri);
                }

                string replacement = uri == default ? linkText : $"[{linkText}]({uri})";
                var textBefore = text[..foundRange.Start].TrimEnd('.'); // Sometimes links start with a . but discord doesn't support bulleted lists
                var textAfter = text[(foundRange.End.Value)..];

                text = textBefore + replacement + textAfter;
            }

            return text;
        }

        public static string GenerateBoldAndItalicText(this string text)
        {
            var textParts = SeparateIntoPartsBasedOnIgnoredRanges(text);

            return string.Join(null, textParts.Select(x =>
                !x.Ignore ?
                x.Part.Replace("'''", "**").Replace("''", "_") :
                x.Part
            ));
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
