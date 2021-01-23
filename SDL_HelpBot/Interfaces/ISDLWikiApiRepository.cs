using SDL_HelpBotLibrary.SDLWikiApi;
using System;
using System.Collections.Generic;

namespace SDL_HelpBot.Interfaces
{
    public interface ISDLWikiApiRepository
    {
        SDLWikiApiCache Cache { get; }
        Dictionary<string, Uri> Lookups { get; }
        Uri HostUri { get; }
        string CacheFile { get; }
        bool EnableAutomaticUpdate { get; set; }
        HashSet<SDLWikiApiItem> SearchForWikiItems(string nameQuery);
        SDLWikiApiItem GetWikiItem(string name, out string errorMessage);
        void Import(string filePath);
        void Export(string filePath, bool indented = false);
    }
}
