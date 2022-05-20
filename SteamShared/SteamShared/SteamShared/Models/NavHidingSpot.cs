using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    [Flags]
    public enum HidingSpotAttribute 
    {
        IN_COVER = 0x01, // In a corner with good hard cover nearby
        GOOD_SNIPER_SPOT = 0x02, // Had at least one decent sniping corridor
        IDEAL_SNIPER_SPOT = 0x04, // Can see either very far, or a large area, or both
        EXPOSED = 0x08 // Spot in the open, usually on a ledge or cliff
    }

    public class NavHidingSpot
    {
        public NavHidingSpot() { /* Do nothing */ }

        public NavHidingSpot(uint navVersion, System.IO.BinaryReader reader) 
        {
            this.Parse(navVersion, reader);
        }

        public void Parse(uint navVersion, System.IO.BinaryReader reader)
        {
            if(navVersion >= 2)
                this.ID = reader.ReadUInt32();

            if (navVersion >= 1)
                this.Position = new Vector3(reader);

            if (navVersion >= 2)
                this.Attributes = reader.ReadByte();
        }

        public uint ID { get; set; }

        public Vector3? Position { get; set; }

        public byte Attributes { get; set; }
    }
}
