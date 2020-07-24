using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NUnit.Framework;
using SDL_HelpBot.Services;
using System;
using System.Net.Http;

namespace SDL_HelpBot.UnitTests
{
    [TestFixture]
    public class StandardTests
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public async void LoginToDiscord()
        {
            using var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();

            //SDL_HELPBOT_DISCORDTOKEN: ${{ secrets.SDL_HELPBOT_DISCORDTOKEN }}

            var client = services.GetRequiredService<DiscordSocketClient>();
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("SDL_HELPBOT_DISCORDTOKEN"));
        }
    }
}