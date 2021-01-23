using Microsoft.Extensions.Configuration;
using NLog;
using SDL_HelpBotLibrary.Parsers;
using SDL_HelpBot.Interfaces;
using SDL_HelpBotLibrary.Extensions;
using System;
using SDL_HelpBotLibrary.SDLWikiApi;
using System.Collections.Generic;
using Discord;
using System.Linq;
using SDL_HelpBotLibrary.Tools;

namespace SDL_HelpBot.Services
{
    public class SDLWikiService
    {
        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiService));
        private IConfiguration _config;
        private ISDLWikiApiRepository _wikiRepo;
        private SDLWikiParser _parser;

        public SDLWikiService(IConfiguration config, ISDLWikiApiRepository wikiRepo)
        {
            _config = config;
            _wikiRepo = wikiRepo;
            _parser = new SDLWikiParser(new Uri(_config["SDLWikiHostUrl"]));
        }

        public Dictionary<string, string> GetWikiItemReply(string message) =>
            GetWikiItemReply(message, out _);

        public Dictionary<string, string> GetWikiItemReply(string message, out SDLWikiApiItem wikiItem)
        {
            wikiItem = _wikiRepo.GetWikiItem(message, out _);
            if(wikiItem != null)
            {
                return _parser.Parse(wikiItem.RawText, convertToMarkup: true);
            }
            else
            {
                return null;
            }
        }

        public Embed GetWikiItemEmbedReply(string message)
        {
            try
            {
                var replyDict = GetWikiItemReply(message, out var wikiItem);
                if (replyDict == null || !replyDict.Any()) return null;

                int fieldCount = 0;
                var embedFields = new List<EmbedFieldBuilder>();
                foreach (var section in replyDict.Skip(1))
                {
                    if (fieldCount >= DiscordLimits.DISCORD_MAX_FIELD_COUNT) break;

                    string fieldName = section.Key.LimitLength(DiscordLimits.DISCORD_MAX_FIELD_NAME_LENGTH);
                    string fieldValue = section.Value.LimitLength(DiscordLimits.DISCORD_MAX_FIELD_VALUE_LENGTH);

                    if (fieldValue.Length >= DiscordLimits.DISCORD_MAX_FIELD_VALUE_LENGTH && section.Value.Contains("```"))
                    {
                        fieldValue = "This section contains code and is too long for Discord to display.";
                    }

                    embedFields.Add(new EmbedFieldBuilder()
                        .WithName(fieldName)
                        .WithValue(fieldValue));

                    fieldCount++;
                }

                var embedBuilder = new EmbedBuilder()
                    .WithTitle(replyDict.First().Key)
                    .WithUrl(wikiItem.Uri.AbsoluteUri)
                    .WithDescription(replyDict.First().Value)
                    .WithFields(embedFields);

                return embedBuilder.Build();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"An error occurred when building reply for message '{message}'");
                return null;
            }
        }

        public Embed GetWikiSearchEmbedReply(string message)
        {
            var items = _wikiRepo.SearchForWikiItems(message);

            bool maxItemsHit = false;
            int fieldCount = 0;
            var embedFields = new List<EmbedFieldBuilder>();
            foreach (var item in items)
            {
                if (fieldCount >= DiscordLimits.DISCORD_MAX_FIELD_COUNT)
                {
                    maxItemsHit = true;
                    break;
                }

                string fieldValue = string.Empty;
                string itemSummary = _parser.ParseSummary(item.RawText).Trim() + Environment.NewLine;
                string itemUrl = $"**[{item.Uri}]({item.Uri})**";

                if (!string.IsNullOrWhiteSpace(itemSummary))
                {
                    itemSummary = itemSummary.LimitLength(DiscordLimits.DISCORD_MAX_FIELD_VALUE_LENGTH - (itemUrl.Length + 1));
                    fieldValue = itemSummary + itemUrl;
                }
                else
                {
                    fieldValue = itemUrl;
                }

                if(fieldValue.Length >= DiscordLimits.DISCORD_MAX_FIELD_VALUE_LENGTH)
                    fieldValue = itemUrl;

                embedFields.Add(new EmbedFieldBuilder()
                    .WithName(item.Name)
                    .WithValue(fieldValue));

                fieldCount++;
            }

            var description = items.Any() ? $"_Found {items.Count} items matching your query._" : "_No results found._";
            if(maxItemsHit)
            {
                description += Environment.NewLine + $"_Discord only allows displaying a maximum of {DiscordLimits.DISCORD_MAX_FIELD_COUNT} items. Try narrowing your search terms._";
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle($"Searched for '{message.LimitLength(20)}'")
                .WithDescription(description)
                .WithFields(embedFields);

            return embedBuilder.Build();
        }
    }
}
