using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.SourceConfig
{
    public class SourceCFG
    {
        /// <summary>
        /// Gets or sets a list of concommands or convars in this CFG-file.
        /// </summary>
        public List<SourceCFGCommand> Commands { get; set; }
    }
}
