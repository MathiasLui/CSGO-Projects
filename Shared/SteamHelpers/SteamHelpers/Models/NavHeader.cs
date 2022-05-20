using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavHeader
    {
        public uint MagicNumber { get; set; }

        public uint Version { get; set; }

        /// <summary>
        /// Version >= 10
        /// </summary>
        public uint SubVersion { get; set; }

        /// <summary>
        /// Version >= 4
        /// </summary>
        public uint SaveBspSize { get; set; }

        /// <summary>
        /// Version >= 14
        /// </summary>
        public byte IsAnalyzed { get; set; }

        /// <summary>
        /// Version >= 5
        /// </summary>
        public ushort PlacesCount { get; set; }

        /// <summary>
        /// Version >= 5
        /// </summary>
        public string[]? PlacesNames { get; set; }

        /// <summary>
        /// Version > 11
        /// </summary>
        public byte HasUnnamedAreas { get; set; }

        public uint AreaCount { get; set; }

        public NavArea[]? NavAreas { get; set; }

        public uint LadderCount { get; set; }

        public NavLadder[]? Ladders { get; set; }
    }
}
