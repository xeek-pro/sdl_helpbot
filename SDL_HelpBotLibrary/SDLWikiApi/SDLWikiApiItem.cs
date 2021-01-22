using NLog;
using Polly;
using SDL_HelpBotLibrary.Tools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SDL_HelpBotLibrary.SDLWikiApi
{
    public class SDLWikiApiItem : IEquatable<SDLWikiApiItem>
    {
        private const string DEFAULT_QUERY = "?action=raw";
        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiApiItem));

        public string Name { get; set; }
        public Uri Uri { get; set; }
        public HashSet<string> Categories { get; set; } = new HashSet<string>();
        public DateTime LastUpdate { get; set; }
        public static TimeSpan Expiration { get; set; } = TimeSpan.FromDays(30);
        public string RawText { get; set; }

        public SDLWikiApiItem(string name = default, Uri uri = default)
        {
            Name = name;
            Uri = uri;
        }

        public bool Equals(SDLWikiApiItem other)
        {
            return string.Compare(Name, other.Name, ignoreCase: true) == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SDLWikiApiItem))
            {
                return false;
            }
            else
            {
                return Equals(obj as SDLWikiApiItem);
            }
        }

        public override int GetHashCode()
        {
            return Name.ToLowerInvariant().GetHashCode();
        }

        public void Update(string pageSource)
        {
            Categories = new HashSet<string>();
            RawText = pageSource;

            try
            {
                int indexOfCategoriesStart = pageSource.LastIndexOf("----");
                if (indexOfCategoriesStart >= 0)
                {
                    string prefix = Regex.Escape("[[");
                    string suffix = Regex.Escape("]]").Replace("]", "\\]"); // Regex.Escape doesn't escape ] as it should
                    string categoriesSource = RawText[indexOfCategoriesStart..];

                    var matches = Regex.Matches(categoriesSource, $@"{prefix}([^{suffix}]*){suffix}");
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1) Categories.Add(match.Groups[1].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to parse categories from the Wiki Item raw text to update categories");
            }

            LastUpdate = DateTime.Now;
        }

        public void Update(WebClient client = default)
        {
            // Don't hammer the website:
            SurgeProtection.CheckBeforeRequest();

            _logger.Info($"Updating {GetType().Name} named '{Name}'");

            string pageSource = string.Empty;
            bool createWebClient = client == default;

            if (createWebClient)
            {
                client = new WebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36");
            }

            try
            {
                Policy
                .Handle<WebException>()
                .WaitAndRetry(2, retryCount =>
                {
                    _logger.Info("MoinMoin Surge Protection likely encountered when downloading Wiki Item");
                    return TimeSpan.FromSeconds(retryCount * 40);
                }).Execute(() =>
                {
                    UriBuilder uriBuilder = new UriBuilder(Uri);
                    if (!Uri.Query.ToLower().Contains(DEFAULT_QUERY)) uriBuilder.Query = DEFAULT_QUERY;
                    pageSource = client.DownloadString(Uri);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to download uri '{Uri}' to update wiki item");
                throw;
            }
            finally
            {
                if(createWebClient) client.Dispose();
            }

            Update(pageSource);
        }
    }
}
