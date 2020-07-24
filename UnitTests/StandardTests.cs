using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
        public void LoginToDiscord()
        {
            using var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();

            //SDL_HELPBOT_DISCORDTOKEN: ${{ secrets.SDL_HELPBOT_DISCORDTOKEN }}

            services
                .GetRequiredService<DiscordSocketClient>()
                .LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("SDL_HELPBOT_DISCORDTOKEN"))
                .GetAwaiter()
                .GetResult();
        }
    }
}