using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavConnectionData
    {
        public NavConnectionData(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.Count = reader.ReadUInt32();

            this.AreaIDs = new uint[this.Count];
            for(int i = 0; i < this.AreaIDs.Length; i++)
            {
                this.AreaIDs[i] = reader.ReadUInt32();
            }
        }

        public uint Count { get; set; }

        public uint[]? AreaIDs { get; set; }
    }
}
