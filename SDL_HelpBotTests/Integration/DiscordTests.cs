using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SDL_HelpBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Humanizer;
using System.Reflection.Metadata.Ecma335;

namespace SDL_HelpBotTests.Integration
{
    [TestFixture]
    public class DiscordTests
    {
        private const string DiscordTokenEnvName = "SDL_HELPBOT_DISCORDTOKEN";
        private string DiscordToken { get; set; }

        [SetUp]
        public void SetUp()
        {
            /*
             * The .github workflow file should contain the lines in between the following hyphen 
             * separators:
             * ------------------------------------------------------------------------------------
             * 
             *    - name: Test
             *      env:
             *          SDL_HELPBOT_DISCORDTOKEN: ${{ secrets.SDL_HELPBOT_DISCORDTOKEN }}
             *      run: dotnet test --no-restore --verbosity normal
             *      
             * ------------------------------------------------------------------------------------
             * If this is being executed on a system not from GitHub Actions, then you will need
             * to create an environment variable named SDL_HELPBOT_DISCORDTOKEN with your key.
             */
            DiscordToken = Environment.GetEnvironmentVariable(DiscordTokenEnvName);
        }

        [Test]
        public void ValidateDiscordToken()
        {   /* 
             * If this continues to fail repeatedly after you've setup the environment variable 
             * there are two possibilities:
             * 
             *  1.  You need to restart Visual Studio, the shell, power shell, or the command 
             *      prompt that you're running these tests from so that the environment variables 
             *      get refreshed.
             *
             *  2.  You haven't created the GitHub Secret for your repository or the GitHub 
             *      Workflow file in the .github folder does not have the env section for your 
             *      GitHub Action.
             */

            Assert.IsNotNull(
                DiscordToken,
                $"The discord token doesn't exist, it's likely you forgot to create an environment variable named {DiscordTokenEnvName}."
            );

            Assert.IsNotEmpty(
                DiscordToken,
                $"The discord token exists, but it's empty, you probably need to set the environment variable named {DiscordTokenEnvName}" + 
                " to your token by finding it in the bot category of your Discord application." +
                " Go here: https://discord.com/developers/applications"
            );
        }

        [Test]
        [TestOf(typeof(DiscordSocketClient))]
        public void LoginToDiscord()
        {
            using var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<SDLWikiService>()
                .BuildServiceProvider();

            DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
            Assert.DoesNotThrowAsync(() => client.LoginAsync(TokenType.Bot, DiscordToken), 
                "Failed to login to Discord, is your token correct?");

            // Not that this matters in this specific test
            client.StopAsync().GetAwaiter().GetResult();
        }
    }
}
