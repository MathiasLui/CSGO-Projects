using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    internal class BSPLump
    {
        /// <summary>
        /// The offset of the lump block from the beginning of the file.
        /// It's rounded up to the nearest 4-byte boundary, as is the corresponding data lump.
        /// </summary>
        public int LumpBlockOffset { get; set; }

        /// <summary>
        /// The length of the lump block in bytes.
        /// </summary>
        public int LumpBlockLength { get; set; }

        /// <summary>
        /// Version of the format of the lump, usually 0.
        /// </summary>
        public int LumpVersion { get; set; }

        /// <summary>
        /// The four CC identifier, that is usually all 0s. For compressed lumps it's the uncompressed lump data size as int.
        /// </summary>
        public char[] FourCC { get; set; } = new char[4];
    }
}
