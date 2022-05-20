using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavApproachSpot
    {
        public NavApproachSpot() { /* Do nothing */ }

        public NavApproachSpot(System.IO.BinaryReader reader) 
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.ApproachHereId = reader.ReadUInt32();
            this.ApproachPrevId = reader.ReadUInt32();
            this.ApproachType = reader.ReadByte();
            this.ApproachNextId = reader.ReadUInt32();
            this.ApproachHow = reader.ReadByte();
        }

        public uint ApproachHereId { get; set; }

        public uint ApproachPrevId { get; set; }

        public byte ApproachType { get; set; }

        public uint ApproachNextId { get; set; }

        public byte ApproachHow { get; set; }
    }
}
