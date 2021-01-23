using SDL_HelpBotLibrary.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SDL_HelpBotLibrary.Extensions
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

        public static IEnumerable<SDLWikiIgnorableBlock> SeparateIntoPartsFromRanges(this string text, List<Range> ranges)
        {
            if (ranges == default || !ranges.Any())
            {
                yield return new SDLWikiIgnorableBlock(text, false);
                yield break;
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
                    yield return new SDLWikiIgnorableBlock(partBuilder.ToString(), lastIgnore);
                    partBuilder.Clear();
                }

                lastIgnore = ignore;
            }
        }

        public enum CodeBlockRangeType { MoinMoin, Discord }
        public static IEnumerable<Range> GetCodeBlockRanges(this string text, CodeBlockRangeType codeBlockType)
        {
            if (codeBlockType == CodeBlockRangeType.MoinMoin)
            {
                var prefixRegex = new Regex(@$"(?={CODEBLOCK_PREFIX})");
                var suffixRegex = new Regex(@$"(?<={CODEBLOCK_SUFFIX})");

                var blocks =
                    prefixRegex.Split(text)
                    .Select(x => suffixRegex.Split(x, 2))
                    .SelectMany(x => x);

                int currentPosition = 0;
                foreach (var block in blocks)
                {
                    if (block.StartsWith(CODEBLOCK_PREFIX) && block.EndsWith(CODEBLOCK_SUFFIX))
                    {
                        yield return currentPosition..(currentPosition + block.Length);
                    }

                    currentPosition = block.Length;
                }
            }
            else if (codeBlockType == CodeBlockRangeType.Discord)
            {
                int currentPosition = 0;
                while ((currentPosition = text.IndexOf(DISCORD_CODEBLOCK_WRAPPER, currentPosition)) != -1)
                {
                    int codeStartIndex = currentPosition;
                    int codeEndIndex = text.IndexOf(DISCORD_CODEBLOCK_WRAPPER, codeStartIndex);

                    if (codeEndIndex > codeStartIndex)
                    {
                        yield return codeStartIndex..(codeEndIndex + DISCORD_CODEBLOCK_WRAPPER.Length);
                    }

                    currentPosition = codeEndIndex;
                }
            }
        }

        private static IEnumerable<SDLWikiIgnorableBlock> EnumerateConversionMethod(this IEnumerable<SDLWikiIgnorableBlock> blocks, Func<string, string> method)
        {
            return blocks.Select(x => x.Ignore ? x : x.Set(method(x.Part)));
        }

        public static IEnumerable<SDLWikiIgnorableBlock> RemoveMoinMoinMacros(this IEnumerable<SDLWikiIgnorableBlock> blocks) =>
            blocks.EnumerateConversionMethod(RemoveMoinMoinMacros);

        public static string RemoveMoinMoinMacros(this string text)
        {
            return Regex.Replace(text, @"<<([^>>]*)>>", string.Empty);
        }

        public static IEnumerable<SDLWikiIgnorableBlock> GenerateEmojis(this IEnumerable<SDLWikiIgnorableBlock> blocks) =>
            blocks.EnumerateConversionMethod(GenerateEmojis);

        public static string GenerateEmojis(this string text)
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

                if (block.StartsWith(CODEBLOCK_PREFIX) && block.EndsWith(CODEBLOCK_SUFFIX))
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
                currentPosition = result.Length - 1;
            }

            return result.ToString();
        }

        public static IEnumerable<SDLWikiIgnorableBlock> GenerateDiscordTables(this IEnumerable<SDLWikiIgnorableBlock> blocks, int maxLength = -1) =>
            blocks.Select(x => x.Ignore ? x : x.Set(GenerateDiscordTables(x.Part, maxLength)));

        public static string GenerateDiscordTables(this string text, int maxLength = -1)
        {
            const string moinMoinTableStarter = "||";
            string detectedLineEnding = text.Any(x => x == '\r') ? "\r\n" : "\n";

            // Only handle text that's detected to have columns in it:
            if (text.Contains(moinMoinTableStarter))
            {
                int characterCount = 0;
                var lines = text.Replace("\r", "").Split('\n');

                // For each line, separate the columns, then assume the last item is the description, and all items 
                // before it to be the parameter's name and (or) type:
                var result = lines.Select(line =>
                {
                    // Only handle lines that are part of tables:
                    if (line.TrimStart().StartsWith(moinMoinTableStarter))
                    {
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
                            line = $"**` {paramType} `**" + $"*` {paramName} `* - {desc ?? string.Empty}";

                        // Only has the parameter name:
                        else if (string.IsNullOrEmpty(paramType) && !string.IsNullOrEmpty(paramName))
                            line = $"**`{paramName}`** - {desc ?? string.Empty}";

                        else line = $"*{desc}*";

                        characterCount += line.Length;
                        if (maxLength >= 0) line = characterCount > maxLength ? string.Empty : line;
                    }

                    return line;
                });

                return string.Join(detectedLineEnding, result);
            }

            return text;
        }

        public static IEnumerable<SDLWikiIgnorableBlock> GenerateDiscordLinks(this IEnumerable<SDLWikiIgnorableBlock> blocks, string defaultBaseUrl) =>
            blocks.Select(x => x.Ignore ? x : x.Set(GenerateDiscordLinks(x.Part, defaultBaseUrl)));

        public static IEnumerable<SDLWikiIgnorableBlock> GenerateDiscordLinks(this IEnumerable<SDLWikiIgnorableBlock> blocks, Uri defaultBaseUrl) =>
            blocks.Select(x => x.Ignore ? x : x.Set(GenerateDiscordLinks(x.Part, defaultBaseUrl)));

        public static string GenerateDiscordLinks(this string text, string defaultBaseUrl)
            => GenerateDiscordLinks(text, defaultBaseUrl == default ? default : new Uri(defaultBaseUrl, UriKind.Absolute));

        public static string GenerateDiscordLinks(this string text, Uri defaultBaseUri = default)
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

        public static IEnumerable<SDLWikiIgnorableBlock> GenerateBoldAndItalicText(this IEnumerable<SDLWikiIgnorableBlock> blocks) =>
            blocks.EnumerateConversionMethod(GenerateBoldAndItalicText);

        public static string GenerateBoldAndItalicText(this string text)
        {
            return text
                .Replace("'''", "**")
                .Replace("''", "_");
        }

        public static IEnumerable<SDLWikiIgnorableBlock> ReplaceWeirdStuff(this IEnumerable<SDLWikiIgnorableBlock> blocks, params (string Old, string New)[] weirdStuff) =>
            blocks.Select(x => x.Ignore ? x : x.Set(ReplaceWeirdStuff(x.Part, weirdStuff)));

        public static string ReplaceWeirdStuff(this string text, params (string Old, string New)[] weirdStuff)
        {
            weirdStuff.ToList().ForEach(x => text = text.Replace(x.Old, x.New));
            return text;
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

            var sectionRegex = new Regex(@"(?=\n\=)");
            var result = new Dictionary<string, string>();
            var split = sectionRegex
                .Split(textWithCategoriesRemoved.Trim())
                .Where(section => section.TrimStart().StartsWith("="));

            foreach (var section in split)
            {
                if (string.IsNullOrWhiteSpace(section)) continue;

                var parts = section.TrimStart().Split('\n', 2);
                var sectionName = parts[0].Replace("=", "").Trim();
                var sectionValue = parts[1].Trim();
                result.Add(sectionName, sectionValue);
            }

            if (includeCategories)
            {
                var categories = text.GetMoinMoinCategorySection();
                if (!string.IsNullOrWhiteSpace(categories))
                {
                    result.Add("Categories", categories);
                }
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
