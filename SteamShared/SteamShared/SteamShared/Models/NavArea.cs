using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class NavArea
    {
        public NavArea() { /* Do nothing */ }

        public NavArea(uint navVersion, BinaryReader reader)
        {
            this.Parse(navVersion, reader);
        }

        public void Parse(uint navVersion, BinaryReader reader)
        {
            this.ID = reader.ReadUInt32();

            if (navVersion <= 8)
                this.AttributeBitField = BitConverter.GetBytes((short)reader.ReadByte()); // .NET 6 would cast the byte to a short, .NET 7 will be ambiguous between short and Half
            else if (navVersion <= 12)
                this.AttributeBitField = BitConverter.GetBytes(reader.ReadUInt16());
            else if (navVersion >= 13)
                this.AttributeBitField = BitConverter.GetBytes(reader.ReadUInt32());

            this.NorthWestCorner = new Vector3(reader);
            this.SouthEastCorner = new Vector3(reader);
            this.NorthEastZ = reader.ReadSingle();
            this.SouthWestZ = reader.ReadSingle();

            for(int i = 0; i < this.ConnectionData!.Length; i++)
            {
                this.ConnectionData[i] = new NavConnectionData(reader);
            }

            // === HIDING SPOTS === //

            this.HidingSpotCount = reader.ReadByte();

            this.HidingSpots = new NavHidingSpot[this.HidingSpotCount];
            for(int i = 0; i < this.HidingSpots.Length; i++)
            {
                this.HidingSpots[i] = new NavHidingSpot(navVersion, reader);
            }

            // === APPROACH SPOTS === //

            if (navVersion < 15)
            {
                this.ApproachSpotCount = reader.ReadByte();

                this.ApproachSpots = new NavApproachSpot[this.ApproachSpotCount];
                for (int i = 0; i < this.ApproachSpots.Length; i++)
                {
                    this.ApproachSpots[i] = new NavApproachSpot(reader);
                }
            }

            // === ENCOUNTER PATHS === //

            this.EncounterPathCount = reader.ReadUInt32();

            this.EncounterPaths = new NavEncounterPath[this.EncounterPathCount];
            for (int i = 0; i < this.EncounterPaths.Length; i++)
            {
                this.EncounterPaths[i] = new NavEncounterPath(reader);
            }

            this.PlaceID = reader.ReadUInt16();

            for(int i = 0; i < this.LadderIDSequence!.Length; i++)
            {
                this.LadderIDSequence[i] = new NavLadderIDSequence(reader);
            }

            for (int i = 0; i < this.EarliestOccupyTimes!.Length; i++)
            {
                this.EarliestOccupyTimes[i] = reader.ReadSingle();
            }

            if (navVersion >= 16)
            {
                for (int i = 0; i < this.LightIntensity!.Length; i++)
                {
                    this.LightIntensity[i] = reader.ReadSingle();
                }
            }

            // Called "Visible areas" in Valve's code
            this.AreaBindCount = reader.ReadUInt32();

            this.AreaBindSequence = new NavAreaBind[this.AreaBindCount];
            for(int i = 0; i < this.AreaBindSequence.Length; i++)
            {
                this.AreaBindSequence[i] = new NavAreaBind(reader);
            }

            this.InheritVisibilityFromAreaID = reader.ReadUInt32();

            //this.CustomData = new IntPtr(BitConverter.ToInt64(reader.ReadBytes(IntPtr.Size)));
            byte garbageCount = reader.ReadByte();

            reader.BaseStream.Position += garbageCount * 14;
        }

        public uint ID { get; set; }

        /// <summary>
        /// Version <= 8: unsigned char (1 byte)
        /// Version <= 12: unsigned short (2 bytes)
        /// Version >= 8: unsigned int (4 bytes)
        /// </summary>
        public byte[]? AttributeBitField { get; set; } = new byte[4];

        public Vector3? MedianPosition 
        { 
            get
            {
                float newX = (this.ActualNorthWestCorner!.X + this.ActualNorthEastCorner!.X + this.ActualSouthEastCorner!.X + this.ActualSouthWestCorner!.X) / 4;
                float newY = (this.ActualNorthWestCorner!.Y + this.ActualNorthEastCorner!.Y + this.ActualSouthEastCorner!.Y + this.ActualSouthWestCorner!.Y) / 4;
                float newZ = (this.ActualNorthWestCorner!.Z + this.ActualNorthEastCorner!.Z + this.ActualSouthEastCorner!.Z + this.ActualSouthWestCorner!.Z) / 4;
                return new Vector3 { X = newX, Y = newY, Z = newZ };
            } 
        }

        // I believe the corners are actually treated as if North was on the left of the map, I just changed the namings here to work with them more accurately
        public Vector3? ActualNorthWestCorner { get => this.NorthEastCorner; } 
        public Vector3? ActualNorthEastCorner { get => this.SouthEastCorner; }
        public Vector3? ActualSouthEastCorner { get => this.SouthWestCorner; }
        public Vector3? ActualSouthWestCorner { get => this.NorthWestCorner; }

        /// <summary>
        /// Actually South West?
        /// </summary>
        public Vector3? NorthWestCorner { get; set; } = Vector3.Zero;

        /// <summary>
        /// Actually North East?
        /// </summary>
        public Vector3? SouthEastCorner { get; set; } = Vector3.Zero;

        /// <summary>
        /// Actually North West?
        /// </summary>
        public Vector3? NorthEastCorner 
        { 
            get
            {
                return new Vector3 
                { 
                    X = this.NorthWestCorner?.X ?? 0, 
                    Y = this.SouthEastCorner?.Y ?? 0, 
                    Z = this.SouthWestZ
                };
            } 
        }

        /// <summary>
        /// Actually South East?
        /// </summary>
        public Vector3? SouthWestCorner
        {
            get
            {
                return new Vector3 
                { 
                    X = this.SouthEastCorner?.X ?? 0, 
                    Y = this.NorthWestCorner?.Y ?? 0, 
                    Z = this.NorthEastZ
                };
            }
        }

        public float NorthEastZ { get; set; }

        public float SouthWestZ { get; set; }

        public NavConnectionData[]? ConnectionData { get; set; } = new NavConnectionData[4];

        public byte HidingSpotCount { get; set; }

        public NavHidingSpot[]? HidingSpots { get; set; }

        /// <summary>
        /// Version < 15
        /// </summary>
        public byte ApproachSpotCount { get; set; }

        /// <summary>
        /// Version < 15
        /// </summary>
        public NavApproachSpot[]? ApproachSpots { get; set; }

        public uint EncounterPathCount { get; set; }

        public NavEncounterPath[]? EncounterPaths { get; set; }

        public ushort PlaceID { get; set; }

        public NavLadderIDSequence[]? LadderIDSequence { get; set; } = new NavLadderIDSequence[2];

        public float[]? EarliestOccupyTimes { get; set; } = new float[2];

        public float[]? LightIntensity { get; set; } = new float[4];

        public uint AreaBindCount { get; set; }

        public NavAreaBind[]? AreaBindSequence { get; set; }

        public uint InheritVisibilityFromAreaID { get; set; }

        public IntPtr CustomData { get; set; }
    }
}
