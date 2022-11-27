using SteamShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Damage_Calculator
{
    internal class Voxel
    {
        private uint x;
        private uint y;
        private uint z;
        private uint voxelId;

        public uint X 
        { 
            get => this.x; 
            set 
            {
                this.x = value & 0x7FF; // Maximum value is 11 bits, make double sure
                this.updateVoxelId();
            }
        }

        public uint Y
        {
            get => this.y;
            set
            {
                this.y = value & 0x7FF; // Maximum value is 11 bits, make double sure
                this.updateVoxelId();
            }
        }

        public uint Z
        {
            get => this.z;
            set
            {
                this.z = value & 0x3FF; // Maximum value is 10 bits, make double sure
                this.updateVoxelId();
            }
        }

        public uint VoxelId
        {
            get => this.voxelId;
            set
            {
                this.voxelId = value;

                // VoxelID consists of X=11, Y=11 and Z=10 bits in little endian like this ZZZZZZZZZZYYYYYYYYYYYXXXXXXXXXXX
                // so extract them

                // No need to bitshift since it's already on the lowest bit, but & it so that Y and Z are 0
                this.x = value & 0x7FF;

                // & it to only get the Y value in the middle and then shift it to the rightmost spot
                this.y = (value & 0x3FF800) >> 11;

                // This has no values higher than it so no & is needed, but shift it to the rightmost spot, the lower values will be discarded anyways
                this.z = value >> 22;
            }
        }

        private void updateVoxelId()
        {
            this.voxelId = 0;

            this.voxelId |= this.x;

            this.voxelId |= this.y << 11;

            this.voxelId |= this.z << 22;
        }
    }

    internal static class SpatialPartitioningHelper
    {
        const int MAX_COORD_INTEGER = 16384;
        const int MIN_COORD_INTEGER = -MAX_COORD_INTEGER;
        const float MAX_COORD_FLOAT = MAX_COORD_INTEGER;
        const float MIN_COORD_FLOAT = -MAX_COORD_FLOAT;
        const int COORD_EXTENT = 2 * MAX_COORD_INTEGER;

        const int SPHASH_LEVEL_SKIP = 2;

        const int SPHASH_VOXEL_SIZE = 256; // Must be power of 2
        const int SPHASH_VOXEL_SHIFT = 8;

        const float SPHASH_EPS = 0.03125f;

        static readonly Vector3 voxelOrigin = new Vector3 { X = MIN_COORD_FLOAT, Y = MIN_COORD_FLOAT, Z = MIN_COORD_FLOAT };

        static int levelShift;
        static int levelCount;

        static int GetVoxelSize(int level)
        {
            return SPHASH_VOXEL_SIZE << (SPHASH_LEVEL_SKIP * level);
        }

        static void UpdateLevelShift(int level)
        {
            levelShift = SPHASH_VOXEL_SHIFT + (SPHASH_LEVEL_SKIP * level);
        }

        static void UpdateLevelCount()
        {
            levelCount = 0;
            while (ComputeVoxelCountAtLevel(levelCount) > 2)
            {
                // From level 0 to 3 it will be 128, 32, 8, 2
                ++levelCount;
            }

            // And then add one to have the count instead of the maximum level index
            ++levelCount;
        }

        static int ComputeVoxelCountAtLevel(int level)
        {
            int nVoxelCount = COORD_EXTENT >> SPHASH_VOXEL_SHIFT;
            nVoxelCount >>= (SPHASH_LEVEL_SKIP * level);

            return (nVoxelCount > 0) ? nVoxelCount : 1;
        }

        static Vector3 VoxelIndexFromPoint(Vector3 worldPoint)
        {
            Vector3 voxel = new Vector3();

            voxel.X = (int)(worldPoint.X - voxelOrigin.X) >> levelShift;
            voxel.Y = (int)(worldPoint.Y - voxelOrigin.Y) >> levelShift;
            voxel.Z = (int)(worldPoint.Z - voxelOrigin.Z) >> levelShift;

            return voxel;
        }

        public static bool BoxIntersects(Vector3 boxMins, Vector3 boxMaxs, Vector3 otherMins, Vector3 otherMaxs)
        {
            return (otherMins.X <= boxMaxs.X) && (otherMaxs.X >= boxMins.X) &&
                (otherMins.Y <= boxMaxs.Y) && (otherMaxs.Y >= boxMins.Y) &&
                (otherMins.Z <= boxMaxs.Z) && (otherMaxs.Z >= boxMins.Z);
        }
    }
}
