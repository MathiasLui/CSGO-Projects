using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    internal class BSPHeader
    {
        public static int HEADER_LUMPS = 64;

        /// <summary>
        /// The magic bytes that should be VBSP.
        /// </summary>
        public int Ident { get; set; }

        /// <summary>
        /// The BSP file version. (CS:GO uses 21 or 0x15)
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The dictionary of lumps. The size is set as 64, probably in the SDK.
        /// Unusued lumps have all bytes set to 0.
        /// </summary>
        public BSPLump[] Lumps = new BSPLump[HEADER_LUMPS];

        /// <summary>
        /// Version number of map. Might increase every time a map is saved in Hammer.
        /// </summary>
        public int MapRevision { get; set; }
    }
}
