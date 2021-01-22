using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SDL_HelpBotLibrary.SDLWikiApi;

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

            // Remove XML tags:
            //lines = lines.Select(x => Regex.Replace(x, "<.*?>", string.Empty));

            return string.Join(Environment.NewLine, lines);
        }

        public string ConvertToMarkup(string document)
        {
            return document
                .RemoveMoinMoinMacros()
                .GenerateDiscordTables()
                .GenerateDiscordCodeBlocks()
                .GenerateEmojis()
                .GenerateDiscordLinks(HostUri)
                .GenerateBoldAndItalicText();
        }
    }
}
