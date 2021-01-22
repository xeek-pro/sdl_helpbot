using Microsoft.Extensions.Configuration;
using NLog;
using SDL_HelpBotLibrary.Parsers;
using SDL_HelpBot.Interfaces;
using System;
using SDL_HelpBotLibrary.SDLWikiApi;
using System.Collections.Generic;

namespace SDL_HelpBot.Services
{
    public class SDLWikiService
    {
        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiApiRepository));
        private IConfiguration _config;
        private ISDLWikiApiRepository _wikiRepo;
        private SDLWikiParser _parser;

        public SDLWikiService(IConfiguration config, ISDLWikiApiRepository wikiRepo)
        {
            _config = config;
            _wikiRepo = wikiRepo;
            _parser = new SDLWikiParser(new Uri(_config["SDLWikiHostUrl"]));
        }

        public Dictionary<string, string> GetReply(string message)
        {
            SDLWikiApiItem wikiItem = _wikiRepo.GetWikiItem(message, out _);
            if(wikiItem != null)
            {
                return _parser.Parse(wikiItem.RawText, convertToMarkup: true);
            }
            else
            {
                return null;
            }
        }
    }
}
