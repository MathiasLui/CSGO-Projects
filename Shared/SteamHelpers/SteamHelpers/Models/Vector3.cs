using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Vector3
    {
        public Vector3() { /* Do nothing */ }

        public Vector3(System.IO.BinaryReader reader)
        {
            this.Parse(reader);
        }

        public void Parse(System.IO.BinaryReader reader)
        {
            this.X = reader.ReadSingle();
            this.Y = reader.ReadSingle();
            this.Z = reader.ReadSingle();
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public static Vector3 Zero = new Vector3 { X = 0, Y = 0, Z = 0 };
    }
}
