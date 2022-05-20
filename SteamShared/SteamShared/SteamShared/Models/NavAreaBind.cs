using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavAreaBind
    {
        public NavAreaBind() { /* Do nothing */ }

        public NavAreaBind(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.TargetAreaID = reader.ReadUInt32();
            this.AttributeBitField = reader.ReadByte();
        }

        public uint TargetAreaID { get; set; }

        public byte AttributeBitField { get; set; }
    }
}
