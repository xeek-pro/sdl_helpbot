using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL_HelpBotLibrary.Extensions
{
    public static class DiscordExtensions
    {
        public static string LimitLength(this string text, int max, string suffix = "...")
        {
            if (text.Length <= max)
            {
                return text;
            }
            else
            {
                return text.Substring(0, max - suffix.Length) + suffix;
            }
        }
    }
}
