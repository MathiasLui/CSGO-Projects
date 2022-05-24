using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.SourceConfig
{
    public class SourceCFGCommand
    {
        public string? CommandName { get; set; }

        /// <summary>
        /// Gets or sets the values of this command.
        /// This can be a value or another command, for example when using the 'alias' command.
        /// </summary>
        public List<SourceCFGCommandValue>? CommandValues { get; set; }

        /// <summary>
        /// Gets all values joined with spaces,
        /// use for example when having an echo command with multiple words that's missing quotes
        /// </summary>
        /// <returns></returns>
        public string? GetValuesAsOne()
        {
            if (this.CommandValues == null || this.CommandValues.Count < 1)
                return null;

            return string.Join(' ', this.CommandValues.Select(val => val.Value));
        }
    }
}
