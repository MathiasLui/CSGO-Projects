using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavLadder
    {
        public NavLadder() { /* Do nothing */ }

        public NavLadder(BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(BinaryReader reader)
        {
            this.ID = reader.ReadUInt32();
            this.Width = reader.ReadSingle();
            this.Length = reader.ReadSingle();
            this.Top = new Vector3(reader);
            this.Bottom = new Vector3(reader);
            this.Direction = reader.ReadInt32();
            this.TopForwardAreaID = reader.ReadUInt32();
            this.TopLeftAreaID = reader.ReadUInt32();
            this.TopRightAreaID = reader.ReadUInt32();
            this.TopBehindAreaID = reader.ReadUInt32();
            this.BottomAreaID = reader.ReadUInt32();
        }

        public uint ID { get; set; }

        public float Width { get; set; }

        public float Length { get; set; }

        public Vector3 Top { get; set; }

        public Vector3 Bottom { get; set; }

        public int Direction { get; set; }

        public uint TopForwardAreaID { get; set; }

        public uint TopLeftAreaID { get; set; }

        public uint TopRightAreaID { get; set; }

        public uint TopBehindAreaID { get; set; }

        public uint BottomAreaID { get; set; }
    }
}
