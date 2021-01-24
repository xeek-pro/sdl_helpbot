using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SDL_HelpBot.Interfaces;
using SDL_HelpBot.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SDL_HelpBot
{
    class Program
    {
        public static bool ShouldStop { get; set; }
        private Logger _logger = LogManager.GetLogger(nameof(SDLWikiApiRepository));
        private Logger _loggerDiscord = LogManager.GetLogger("Discord");
        private IConfiguration _config;

        // There is no need to implement IDisposable like before as we are using dependency injection, which handles 
        // calling Dispose for us.
        static void Main(string[] args)
        {
            Console.WriteLine("Starting bot...");
            Task.Run(async () => {
                Console.WriteLine("Press ESC to stop");
                await new Program().MainAsync();
            });

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                // Don't unnecessarily peg a CPU just to check for the key press:
                Thread.Sleep(50);
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

            while (!ShouldStop) Thread.Sleep(500);
            client.StopAsync().GetAwaiter().GetResult();
        }

        private Task LogAsync(LogMessage logMessage)
        {
            LogLevel convertedLogLevel = LogLevel.Info;
            switch(logMessage.Severity)
            {
                case LogSeverity.Critical: convertedLogLevel = LogLevel.Fatal; break;
                case LogSeverity.Debug: convertedLogLevel = LogLevel.Debug; break;
                case LogSeverity.Error: convertedLogLevel = LogLevel.Error; break;
                case LogSeverity.Info: convertedLogLevel = LogLevel.Info; break;
                case LogSeverity.Verbose: convertedLogLevel = LogLevel.Debug; break;
                case LogSeverity.Warning: convertedLogLevel = LogLevel.Warn; break;
            }

            _loggerDiscord.Log(convertedLogLevel, logMessage.Exception, logMessage.Message);

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
