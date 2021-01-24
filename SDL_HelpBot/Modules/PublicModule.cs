using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SDL_HelpBot.Services;

namespace SDL_HelpBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public SDLWikiService SDLWikiService { get; set; }

        [Command("wikiping")]
        [Alias("wikihello")]
        public Task PingAsync() => ReplyAsync("pong!");

        // Get info on a user, or the user who invoked the command if one is not specified
        //[Command("userinfo")]
        //public async Task UserInfoAsync(IUser user = null)
        //{
        //    user ??= Context.User;

        //    await ReplyAsync(user.ToString());
        //}

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand() => ReplyAsync("Nothing to see here!");

        [Command("wiki2")]   
        public Task Wiki([Remainder] string text)
        {
            // Insert a ZWSP before the text to prevent triggering other bots!

            if (SDLWikiService == null)
            {
                return ReplyAsync("\u200B" + "The internal SDLWikiService did not get created");
            }
            else
            {
                var embed = SDLWikiService.GetWikiItemEmbedReply(text);
                if (embed == null) return ReplyAsync('\u200B' + "The document could not be found, the website is down, or a problem occurred");
                else return ReplyAsync(embed: embed);
            }
        }

        [Command("wikisearch")]
        public Task WikiSearch([Remainder] string text)
        {
            const int maxSearchLength = 50;

            if(text.Length > maxSearchLength)
            {
                return ReplyAsync("\u200B" + $"Please limit your search to {maxSearchLength} characters or less");
            }
            if (SDLWikiService == null)
            {
                return ReplyAsync("\u200B" + "The internal SDLWikiService did not get created");
            }
            else
            {
                return ReplyAsync(embed: SDLWikiService.GetWikiSearchEmbedReply(text));
            }
        }
    }
}
