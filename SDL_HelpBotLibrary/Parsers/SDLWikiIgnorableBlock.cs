using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SDL_HelpBotLibrary.Parsers
{
    [DebuggerDisplay("SDLWikiIgnorableBlock Ignore: {Ignore}, Part: {Part}")]
    public class SDLWikiIgnorableBlock
    {
        public string Part { get; set; }
        public bool Ignore { get; set; }

        public SDLWikiIgnorableBlock(string part, bool ignore = false)
        {
            Part = part;
            Ignore = ignore;
        }

        public SDLWikiIgnorableBlock Set(string part, bool? ignore = default)
        {
            Part = part;
            if(ignore != default) Ignore = ignore == true;

            return this;
        }
    }
}
