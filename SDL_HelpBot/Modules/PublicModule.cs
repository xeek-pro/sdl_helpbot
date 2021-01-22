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

        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        // Get info on a user, or the user who invoked the command if one is not specified
        [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        // Ban a user
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        // make sure the user invoking the command can ban
        [RequireUserPermission(GuildPermission.BanMembers)]
        // make sure the bot itself can ban
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("echo")]
        public Task EchoAsync([Remainder] string text)
            // Insert a ZWSP before the text to prevent triggering other bots!
            => ReplyAsync('\u200B' + text);

        // 'params' will parse space-separated elements into a list
        [Command("list")]
        public Task ListAsync(params string[] objects)
            => ReplyAsync("You listed: " + string.Join("; ", objects));

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("wiki")]
        public Task Wiki([Remainder] string text)
        {
            // Insert a ZWSP before the text to prevent triggering other bots!

            if (SDLWikiService == null)
            {
                return ReplyAsync("\u200B" + "The internal SDLWikiService did not get created");
            }
            else
            {
                var replySections = SDLWikiService.GetReply(text);
                if(replySections == null && replySections.Count > 0) return ReplyAsync('\u200B' + "The document could not be found or the website is down");
                else
                {
                    int fieldCount = 0;
                    var embedFields = new List<EmbedFieldBuilder>();
                    foreach(var section in replySections)
                    {
                        if (fieldCount >= 25) break;

                        string fieldName = fieldCount == 0 ? "Summary" : section.Key;
                        if (fieldName.Length > 1024)
                        {
                            fieldName = fieldName.Substring(0, 256 - "...".Length) + "...";
                        }

                        string fieldValue = section.Value;
                        if(fieldValue.Length > 1024)
                        {
                            fieldValue = fieldValue.Substring(0, 1024 - "...".Length) + "...";
                        }

                        embedFields.Add(new EmbedFieldBuilder()
                            .WithName(fieldName)
                            .WithValue(fieldValue));

                        fieldCount++;
                    }

                    var embedBuilder = new EmbedBuilder()
                        .WithTitle(replySections.First().Key)
                        .WithFields(embedFields);

                    return ReplyAsync(embed: embedBuilder.Build());
                }
            }
        }
    }
}
