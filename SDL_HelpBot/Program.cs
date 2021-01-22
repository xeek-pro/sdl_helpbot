using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using SDL_HelpBot.Services;
using SDL_HelpBot.Modules;
using SDL_HelpBot.Interfaces;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SDL_HelpBot
{
    // This is a minimal example of using Discord.Net's command framework - by no means does it show everything the 
    // framework is capable of.
    //
    // You can find samples of using the command framework:
    // - Here, under the 02_commands_framework sample
    // - https://github.com/foxbot/DiscordBotBase - a bare-bones bot template
    // - https://github.com/foxbot/patek - a more feature-filled bot, utilizing more aspects of the library
    class Program
    {
        public static bool ShouldStop { get; set; }
        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiApiRepository));
        private Logger _loggerDiscord = LogManager.GetLogger("Discord");
        private IConfiguration _config;

        // There is no need to implement IDisposable like before as we are using dependency injection, which handles 
        // calling Dispose for us.
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting bot...");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => {
                Console.WriteLine("Press ESC to stop");
                await new Program().MainAsync();
            });
#pragma warning restore CS4014

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                // Don't unnecessarily peg a CPU just to check for the key press:
                await Task.Yield();
            }

            // Causes Program.MainAsync() to stop.
            ShouldStop = true;
        }

        public async Task MainAsync()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);
            _config = builder.Build();

            // You should dispose a service provider created using ASP.NET when you are finished using it, at the end 
            // of your app's lifetime. If you use another dependency injection framework, you should inspect its 
            // documentation for the best way to do this.
            using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();

            // Get the wiki api cache service going:
            var wikiRepoService = services.GetRequiredService<ISDLWikiApiRepository>();

            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            // Tokens should be considered secret data and never hard-coded.
            // We can read from the environment variable to avoid hardcoding.
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("SDL_HELPBOT_DISCORDTOKEN"));
            await client.StartAsync();

            // Here we initialize the logic required to register our commands.
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            while (!ShouldStop) await Task.Yield();
            client.StopAsync().GetAwaiter().GetResult();
        }

        private Task LogAsync(LogMessage logMessage)
        {
            _loggerDiscord.Info(logMessage.Message);
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(provider => _config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<ISDLWikiApiRepository, SDLWikiApiRepository>()
                .AddSingleton<SDLWikiService>()
                .BuildServiceProvider();
        }
    }
}
