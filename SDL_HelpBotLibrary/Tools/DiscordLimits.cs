using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL_HelpBotLibrary.Tools
{
    public static class DiscordLimits
    {
        #region Embed Limits, See: https://discordjs.guide/popular-topics/embeds.html#embed-limits

        public const int DISCORD_MAX_EMBED_TITLE_LENGTH = 256;
        public const int DISCORD_MAX_EMBED_DESC_LENGTH = 2048;
        public const int DISCORD_MAX_FIELD_COUNT = 25;
        public const int DISCORD_MAX_FIELD_NAME_LENGTH = 256;
        public const int DISCORD_MAX_FIELD_VALUE_LENGTH = 1024;
        public const int DISCORD_MAX_EMBED_CHARACTERS = 6000;

        #endregion
    }
}
