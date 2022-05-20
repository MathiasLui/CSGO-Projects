using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamShared.Models;

namespace SteamShared
{
    public enum NavDisplayModes { None, Wireframe, Filled }

    public static class NavFile
    {
        public static NavMesh? Parse(byte[] navFile)
        {
            return NavFile.Parse(new MemoryStream(navFile));
        }

        public static NavMesh? Parse(Stream stream)
        {
            NavMesh mesh = new NavMesh();

            // We do this because ZIP file streams are not seekable
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            stream.Close();

            using var reader = new BinaryReader(memStream);
            reader.BaseStream.Position = 0;

            // Header is created when creating the nav mesh
            mesh.Header!.MagicNumber = reader.ReadUInt32();

            if (mesh.Header.MagicNumber != 0xFEEDFACE)
                // Not a NAV file
                return null;

            uint version = mesh.Header.Version = reader.ReadUInt32();

            System.Diagnostics.Debug.WriteLine($"File is NAV file with version {version}");

            if (version >= 10)
                mesh.Header.SubVersion = reader.ReadUInt32();

            if(version >= 4)
                mesh.Header.SaveBspSize = reader.ReadUInt32();

            if (version >= 14)
                mesh.Header.IsAnalyzed = reader.ReadByte();

            if(version >= 5)
            {
                // Callouts ("Places")
                mesh.Header.PlacesCount = reader.ReadUInt16();

                mesh.Header.PlacesNames = new string[mesh.Header.PlacesCount];
                for (int i = 0; i < mesh.Header.PlacesNames.Length; i++)
                {
                    ushort len = reader.ReadUInt16();

                    mesh.Header.PlacesNames[i] = new(reader.ReadChars(len)[..^1]);
                }

                if(version > 11)
                {
                    mesh.Header.HasUnnamedAreas = reader.ReadByte();
                }
            }

            // PreLoadAreas() used here? What does it do? there is no size visible (that's what she said)

            mesh.Header.AreaCount = reader.ReadUInt32();

            mesh.Header.NavAreas = new NavArea[mesh.Header.AreaCount];
            for(int i = 0; i < mesh.Header.NavAreas.Length; i++)
            {
                mesh.Header.NavAreas[i] = new NavArea(version, reader);
                Vector3? median = mesh.Header.NavAreas[i].MedianPosition;

                // Update max and min z for mapping if applying here
                if (mesh.MinZ == null || median!.Z < mesh.MinZ)
                {
                    mesh.MinZ = median!.Z;
                }

                if (mesh.MaxZ == null || median!.Z > mesh.MaxZ)
                {
                    mesh.MaxZ = median!.Z;
                }
            }

            if(version >= 6)
            {
                mesh.Header.LadderCount = reader.ReadUInt32();

                mesh.Header.Ladders = new NavLadder[mesh.Header.LadderCount];
                for (int i = 0; i < mesh.Header.Ladders.Length; i++)
                {
                    mesh.Header.Ladders[i] = new NavLadder(reader);
                }
            }

            return mesh;
        }
    }
}
