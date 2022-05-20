using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavEncounterPath
    {
        public NavEncounterPath() { /* Do nothing */ }

        public NavEncounterPath(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.EntryAreaID = reader.ReadUInt32();
            this.EntryDirection = reader.ReadByte();
            this.DestAreaID = reader.ReadUInt32();
            this.DestDirection = reader.ReadByte();

            this.EncounterSpotCount = reader.ReadByte();

            this.EncounterSpots = new NavEncounterSpot[this.EncounterSpotCount];
            for(int i = 0; i < this.EncounterSpots.Length; i++)
            {
                this.EncounterSpots[i] = new NavEncounterSpot(reader);
            }
        }

        public uint EntryAreaID { get; set; }

        public byte EntryDirection { get; set; }

        public uint DestAreaID { get; set; }

        public byte DestDirection { get; set; }


        public byte EncounterSpotCount { get; set; }

        public NavEncounterSpot[]? EncounterSpots { get; set; }
    }
}
