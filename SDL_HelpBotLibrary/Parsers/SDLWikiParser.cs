using SDL_HelpBotLibrary.Tools;
using SDL_HelpBotLibrary.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static SDL_HelpBotLibrary.Extensions.MoinMoinToDiscordExtensions;
using System.Text.RegularExpressions;

namespace SDL_HelpBotLibrary.Parsers
{
    public class SDLWikiParser
    {
        public Uri HostUri { get; private set; }

        public SDLWikiParser(Uri hostUri)
        {
            HostUri = hostUri;
        }

        public Dictionary<string, string> Parse(string document, bool convertToMarkup = true)
        {
            document = CleanUpDocument(document);
            if (convertToMarkup) document = ConvertToMarkup(document);
            var sections = document.SplitMoinMoinSections();
            return sections;
        }

        public string ParseSummary(string document, bool convertToMarkup = true)
        {
            document = CleanUpDocument(document);
            var sections = document.SplitMoinMoinSections();

            if (sections.Any())
            {
                if (convertToMarkup) return ConvertToMarkup(sections.First().Value);
                else return sections.First().Value;
            }

            return null;
        }

        private static string CleanUpDocument(string document)
        {
            // Split into lines
            var lines = document.Replace("\r", "").Split('\n').Cast<string>();

            // Remove all moinmoin preprocessor directives, lines that start with #
            lines = lines.Where(x => !x.StartsWith('#'));

            // Trim space at the end of lines:
            lines = lines.Select(x => x.TrimEnd());

            // Remove empty starting lines:
            lines = lines.SkipWhile(x => string.IsNullOrWhiteSpace(x));

            // Remove XML-like tags:
            lines = lines.Select(x => Regex.Replace(x, "<<.*?>>", string.Empty));

            return string.Join(Environment.NewLine, lines);
        }

        public string ConvertToMarkup(string document)
        {
            document = string.Join(null, document
                    .GenerateDiscordCodeBlocks(out var codeBlockRanges)
                    // Enabling the ignoring of code blocks for further parsing:
                    .SeparateIntoPartsFromRanges(codeBlockRanges)
                    .GenerateDiscordTables(DiscordLimits.DISCORD_MAX_FIELD_VALUE_LENGTH - 25)
                    .RemoveMoinMoinMacros()
                    .GenerateEmojis()
                    .GenerateDiscordLinks(HostUri)
                    .GenerateBoldAndItalicText()
                    // Sometimes this causes issues, an example is SDL_!CreateWindow and I'm unsure why it's there.
                    .ReplaceWeirdStuff(("_!", "_"))
                    // Transform back into a collection of strings:
                    .Select(x => x.Part)
                );

            return document;
        }
    }
}
