using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavEncounterSpot
    {
        public NavEncounterSpot() { /* Do nothing */ }

        public NavEncounterSpot(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.AreaID = reader.ReadUInt32();
            this.ParametricDistance = reader.ReadByte();
        }

        public uint AreaID { get; set; }

        public byte ParametricDistance { get; set; }
    }
}
