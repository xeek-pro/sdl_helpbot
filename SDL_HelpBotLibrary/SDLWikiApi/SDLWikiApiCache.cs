using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SDL_HelpBotLibrary.SDLWikiApi
{
    public class SDLWikiApiCache
    {
        [JsonProperty] 
        private ConcurrentDictionary<string, SDLWikiApiItem> Cache { get; set; } = new ConcurrentDictionary<string, SDLWikiApiItem>();

        [JsonProperty] 
        public DateTime LastUpdate { get; private set; } = new DateTime();

        [JsonIgnore] 
        public int Count => Cache.Count;

        public static TimeSpan Expiration { get; set; } = TimeSpan.FromDays(30);

        [JsonConstructor]
        public SDLWikiApiCache(HashSet<SDLWikiApiItem> cache = default, DateTime? lastUpdate = default)
        {
            Update(cache, lastUpdate);
        }

        public void Update(IEnumerable<SDLWikiApiItem> cache = default, DateTime? lastUpdate = default)
        {
            if (cache != default)
            {
                Cache = new ConcurrentDictionary<string, SDLWikiApiItem>(cache.ToDictionary(x => x.Name, x => x));
            }

            LastUpdate = lastUpdate ?? DateTime.Now;
        }

        public void AddOrUpdate(SDLWikiApiItem item)
        {
            Cache.AddOrUpdate(item.Name, item, (k, v) => item);
            LastUpdate = DateTime.Now;
        }

        public SDLWikiApiItem this[string name]
        {
            get
            {
                if(TryGetItem(name, out SDLWikiApiItem item))
                {
                    return item;
                }
                else
                {
                    throw new KeyNotFoundException($"Failed to get the SDLWikiApiItem named '{name}' because it's not in the cache");
                }
            }
            set
            {
                if (Cache.ContainsKey(name))
                {
                    AddOrUpdate(value);
                }
                else
                {
                    throw new KeyNotFoundException($"Failed to update the SDLWikiApiItem named '{name}' because it's not in the cache");
                }
            }
        }

        public bool TryGetItem(string name, out SDLWikiApiItem item)
        {
            return Cache.TryGetValue(name, out item);
        }

        public IEnumerable<SDLWikiApiItem> Enumerate()
        {
            foreach (var kv in Cache)
            {
                yield return kv.Value;
            }
        }
    }
}
