﻿using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;
using SDL_HelpBotLibrary.SDLWikiApi;
using SDL_HelpBotLibrary.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SDL_HelpBot.Interfaces;

namespace SDL_HelpBot.Services
{

    public class SDLWikiApiRepository : ISDLWikiApiRepository
    {
        public SDLWikiApiCache Cache { get; private set; } = new SDLWikiApiCache();
        public Dictionary<string, Uri> Lookups { get; } = new Dictionary<string, Uri>();
        public Uri HostUri { get; private set; }
        public string CacheFile { get; private set; }
        public bool EnableAutomaticUpdate { get; set; } = false;

        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiApiRepository));
        private readonly IConfiguration _config;
        private static readonly WebClient _webClient = new WebClient();

        public SDLWikiApiRepository(IConfiguration config)
        {
            _logger.Info("Starting SDL Wiki API Repository Service");

            _config = config;
            _webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36");
            CacheFile = _config["SDLWikiApiCacheFile"];
            HostUri = new Uri(_config["SDLWikiHostUrl"]);

            try
            {
                Import(CacheFile == default ? $"{nameof(SDLWikiApiCache)}.json" : CacheFile);
                _logger.Info($"Loaded the cache containing {Cache.Count} items");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"The file '{CacheFile}' passed to {nameof(SDLWikiApiRepository)}() doesn't exist because of an exception so the cache will be empty");
            }

            // Update Lookups:
            UpdateLookups();

            if (!EnableAutomaticUpdate)
            {
                _logger.Info($"Automatic Wiki API Cache Update on start-up disabled, last update on: {Cache.LastUpdate}");
            }
            else Task.Run(() => UpdateCache());
        }

        ~SDLWikiApiRepository()
        {
            if (CacheFile != null) Export(CacheFile);
            _webClient.Dispose();
        }

        public SDLWikiApiItem GetWikiItem(string name, out string errorMessage)
        {
            if(!Cache.TryGetItem(name, out SDLWikiApiItem item) || DateTime.Now - item.LastUpdate >= SDLWikiApiItem.Expiration)
            {
                // Maybe the casing is wrong, or maybe there's a partial match:
                if (Lookups.Any())
                {
                    var foundCaseInsensitive = Lookups.Keys.FirstOrDefault(key => string.Compare(key, name, ignoreCase: true) == 0);
                    if(foundCaseInsensitive != default)
                    {
                        return GetWikiItem(foundCaseInsensitive, out errorMessage);
                    }

                    var foundPartial = Lookups.Keys.FirstOrDefault(key => key.ToLowerInvariant().Contains(name.ToLowerInvariant()));
                    if (foundPartial != default)
                    {
                        return GetWikiItem(foundPartial, out errorMessage);
                    }
                }

                // Don't hammer the website:
                SurgeProtection.CheckBeforeRequest();

                // Download the document:
                string documentRawText;
                Uri documentUri = new Uri(HostUri, $"{name.Replace("sdl_", "SDL_")}?action=raw");
                try
                {
                    documentRawText = _webClient.DownloadString(documentUri);
                }
                catch (WebException ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }

                // The site responds with content indicating the Wiki document doesn't exist:
                string notFoundText = $"Page {name} not found";
                if (string.Concat(documentRawText.Take(notFoundText.Length + 1)).ToLowerInvariant().Contains(notFoundText))
                {
                    errorMessage = documentRawText;
                    return null;
                }

                // The page should not be valid HTML since ?action=raw should return MoinMoin markup:
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(documentRawText);
                if(!htmlDoc.ParseErrors.Any())
                {
                    errorMessage = $"Couldn't receive the correct document because the content was in HTML rather than Markup";
                    return null;
                }

                item = new SDLWikiApiItem(name);
                item.Update(documentRawText);
                Cache.AddOrUpdate(item);
            }

            errorMessage = string.Empty;
            return item;
        }

        private void UpdateLookups()
        {
            _logger.Info("Updating lookup dictionary");
            Lookups.Clear();

            // Don't hammer the website:
            SurgeProtection.CheckBeforeRequest();

            Uri categoryApiUri = new Uri(HostUri, "CategoryAPI");
            string categoryApiPageSource;
            try
            {
                categoryApiPageSource = _webClient.DownloadString(categoryApiUri);
                _logger.Debug($"Downloaded '{categoryApiUri}'");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"The file '{categoryApiUri}' page failed to download because of an exception");
                return;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(categoryApiPageSource);
            var nodes = doc.DocumentNode.SelectNodes(@"//*[@id='content']//*[contains(@class,'searchresults')]//a");
            _logger.Debug($"Found {nodes.Count} categories for updating the lookup dictionary");

            if (nodes.Any())
            {
                foreach (var node in nodes)
                {
                    string name = node.InnerText;
                    string link = node.GetAttributeValue("href", string.Empty);
                    int linkQueryIndex = link.IndexOf('?');

                    // Skip anything that looks like a category but has an invalid URI
                    if (!Uri.TryCreate(HostUri, link.Substring(0, linkQueryIndex >= 0 ? linkQueryIndex : link.Length), out Uri uri))
                    {
                        _logger.Warn($"Skipping category named '{name}' for lookup dictionary because it has an invalid URI: {link}");
                        continue;
                    }

                    Lookups.Add(name, uri);
                }
            }
            else
            {
                _logger.Warn("There weren't any items on the categories by name page to fill the lookup dictionary");
            }
        }

        private void UpdateCache()
        {
            if (DateTime.Now - Cache.LastUpdate >= SDLWikiApiCache.Expiration)
            {
                _logger.Info($"Updating the Wiki API Cache since it has expired (age: {(DateTime.Now - Cache.LastUpdate).TotalDays} days)");
            }
            else return;

            if (HostUri == null)
            {
                _logger.Error($"Aborting Wiki API Cache Update, the Host URI for the {GetType().Name} is invalid or not set");
                return;
            }

            if (Lookups.Any())
            {
                int updatedNodeCount = 0;
                DateTime lastFileUpdateTime = DateTime.Now; // Start with now so that the first save isn't immediate
                foreach (var lookupItem in Lookups)
                {
                    var name = lookupItem.Key;
                    var uri = lookupItem.Value;

                    // Save the cache every minute whle it's updating:
                    if (DateTime.Now - lastFileUpdateTime >= TimeSpan.FromMinutes(1))
                    {
                        if (!string.IsNullOrWhiteSpace(CacheFile)) Export(CacheFile);
                    }

                    if(!Cache.TryGetItem(name, out SDLWikiApiItem apiItem))
                    {
                        apiItem = new SDLWikiApiItem() { Name = name, Uri = uri };
                        Cache.AddOrUpdate(apiItem);
                    }

                    if (DateTime.Now - apiItem.LastUpdate >= SDLWikiApiItem.Expiration)
                    {
                        apiItem.Update(_webClient);
                        Cache.Update();
                        updatedNodeCount++;
                    }
                    else
                    {
                        _logger.Info($"Skipping category named '{name}' because it has not expired (age: {(DateTime.Now - apiItem.LastUpdate).TotalDays} days)");
                        continue;
                    }
                }
            }
            else
            {
                _logger.Warn("The lookups are empty so the cache cannot be automatically upadted");
            }

            if (!string.IsNullOrWhiteSpace(CacheFile)) Export(CacheFile);
        }

        public void Import(string filePath)
        {
            string json = System.IO.File.ReadAllText(filePath);
            Cache = JsonConvert.DeserializeObject<SDLWikiApiCache>(json);
        }

        public void Export(string filePath, bool indented = false)
        {
#if DEBUG
            indented = true;
#endif
            string json = JsonConvert.SerializeObject(Cache, indented ? Formatting.Indented : Formatting.None);
            System.IO.File.WriteAllText(filePath, json);
        }
    }
}
