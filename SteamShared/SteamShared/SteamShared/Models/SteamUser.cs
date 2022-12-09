using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class SteamUser
    {
        /// <summary>
        /// The name the user logs in with.
        /// </summary>
        public string? AccountName { get; set; }

        /// <summary>
        /// The persona name the user can change.
        /// </summary>
        public string? PersonaName { get; set; }

        /// <summary>
        /// The time of last login in unix time.
        /// </summary>
        public ulong LastLogin { get; set; }

        /// <summary>
        /// The long steam ID (starting with 7656...)
        /// </summary>
        public ulong SteamID64 { get; set; }

        /// <summary>
        /// The account ID, used for trade links or the userdata folder.
        /// It's calculated from the SteamID64.
        /// </summary>
        public ulong AccountID 
        { 
            get 
            {
                // Steps I take to calculate this:
                // The account ID is the lowest 32 bits (half of the total bits),
                // 1 << 32 will result in one bigger than we need, with no correct bitmask (only one 1).
                // Subtracting 1 will give us all 1s where we need them :).
                // Casting it to e.g. a long is necessary, because otherwise it would overflow into some dumb shit and be wrong.
                // The bitwise-& is done last
                return this.SteamID64 & ((long)1 << 32) - 1;
            }
        }

        public string? AbsoluteUserdataFolderPath { get; set; }
    }
}
