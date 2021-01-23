using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SDL_HelpBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using SDL_HelpBotLibrary.Parsers;
using SDL_HelpBotLibrary.Extensions;
using NSubstitute;
using NSubstitute.Extensions;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using SDL_HelpBot.Interfaces;

namespace SDL_HelpBotTests.Unit
{
    [TestFixture]
    public class SDLWikiApiRepositoryTests
    {
        ISDLWikiApiRepository _repo;

        [OneTimeSetUp]
        public void SetUpFixture()
        {
            var inMemoryConfig = new Dictionary<string, string>
            {
                { "SDLWikiApiCacheFile", "SDLWikiApiCacheFile.json" },
                { "SDLWikiHostUrl", "http://www.example.com/" },
                { "AutomaticallyUpdateCache", "false" }
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            _repo = Substitute.For<SDLWikiApiRepository>(config, false);
        }

        [Test]
        public void SearchLookup()
        {
            var lookups = new Dictionary<string, Uri>
            {
                { "SDL_CreateWindow", new Uri("https://wiki.libsdl.org/SDL_CreateWindow") },
                { "SDL_DestroyMutex", new Uri("https://wiki.libsdl.org/SDL_DestroyMutex") },
                { "SDL_GL_UnloadLibrary", new Uri("https://wiki.libsdl.org/SDL_GL_UnloadLibrary") },
                { "SDL_GetRGBA", new Uri("https://wiki.libsdl.org/SDL_GetRGBA") },
                { "SDL_GetRGB", new Uri("https://wiki.libsdl.org/SDL_GetRGB") }
            };

            _repo.Lookups.Returns(lookups);

            Assert.That(_repo.Search("getrgb").Count == 2);
            Assert.That(_repo.Search("GETRGB").Count == 2);
            Assert.That(_repo.Search("destroymutex").Count == 1);
            Assert.That(_repo.Search("SDL_").Count == _repo.Lookups.Count);
            Assert.That(_repo.Search("CreateWindow GL_UnloadLibrary rgb").Count == 4);
        }
    }
}
