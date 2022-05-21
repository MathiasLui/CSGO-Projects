using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.SourceConfig
{
    public class SourceCFGCommand : SourceCFGCommandBase
    {
        public string? CommandName { get; set; }

        /// <summary>
        /// Gets or sets the values of this command.
        /// This can be a value or another command, for example when using the 'alias' command.
        /// </summary>
        public List<SourceCFGCommandBase> CommandValues { get; set; }
    }
}
