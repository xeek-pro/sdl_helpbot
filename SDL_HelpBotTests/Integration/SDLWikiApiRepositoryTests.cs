using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using SDL_HelpBot.Services;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SDL_HelpBotTests.Integration
{
    [TestFixture]
    public class SDLWikiApiRepositoryTests
    {
        SDLWikiApiRepository repo = null;

        [OneTimeSetUp]
        public void Setup()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"SDLWikiApiCacheFile", "SDLWikiApiCacheFile.json"},
                    {"SDLWikiHostUrl", "https://wiki.libsdl.org/"}
                })
                .Build();

            repo = new SDLWikiApiRepository(config);
        }

        [Test]
        public void CheckLookups()
        {
            Assert.That(repo.Lookups.Any());
        }

        [TestCase("SDL_CreateWindow")]
        [TestCase("sdl_createwindow")]
        [TestCase("createwindow")]
        public void GetWikiItem(string name)
        {
            Assert.That(repo.GetWikiItem(name, out _) != null);
        }
    }
}
