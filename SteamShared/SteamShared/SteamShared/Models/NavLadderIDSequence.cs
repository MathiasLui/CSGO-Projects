using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavLadderIDSequence
    {
        public NavLadderIDSequence() { /* Do nothing */ }

        public NavLadderIDSequence(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.LadderCount = reader.ReadUInt32();

            this.LadderIDs = new uint[this.LadderCount];
            for(int i = 0; i < this.LadderIDs.Length; i++)
            {
                this.LadderIDs[i] = reader.ReadUInt32();
            }
        }

        public uint LadderCount { get; set; }

        public uint[]? LadderIDs { get; set; }
    }
}
