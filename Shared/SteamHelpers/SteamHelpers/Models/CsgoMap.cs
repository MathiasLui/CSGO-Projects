﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Shared.Models
{
    public class CsgoMap
    {
        /// <summary>
        /// The types of maps.
        /// </summary>
        public enum eMapType { Undefined, Defusal, Hostage, DangerZone, ArmsRace }

        /// <summary>
        /// The actual radar image of the map.
        /// </summary>
        public BitmapSource? MapImage { get; set; }

        /// <summary>
        /// The type of gamemode that's played on this map by default.
        /// </summary>
        public eMapType MapType { get; set; }

        /// <summary>
        /// The absolute path to the DDS map radar file.
        /// </summary>
        public string? MapImagePath { get; set; }

        /// <summary>
        /// The absolute path to the actual BSP map file.
        /// </summary>
        public string? BspFilePath { get; set; }

        /// <summary>
        /// The absolute path to the file that holds this map's navigation meshes and callouts.
        /// This might not always be existent, because it is generated by the map builder, but can be packed inside the BSP. In that case its value is "PACKED".
        /// It is always created with maps that are in the main game, because they need callouts and bot movements.
        /// </summary>
        public string? NavFilePath { get; set; }

        /// <summary>
        /// Indicates whether the NAV file was packed inside of the BSP PAKFILE lump.
        /// </summary>
        public bool NavFileBspPacked { get; set; }

        /// <summary>
        /// The absolute path to the file that holds some additional navigational.
        /// This might not always be existent, because it is generated by the map builder, but can be packed inside the BSP. In that case its value is "PACKED".
        /// It *might* always be created with maps that are in the main game.
        /// </summary>
        public string? AinFilePath { get; set; }

        /// <summary>
        /// Indicates whether the AIN file was packed inside of the BSP PAKFILE lump.
        /// </summary>
        public bool AinFileBspPacked { get; set; }

        /// <summary>
        /// The map name as given in the file name, but without the prefix.
        /// </summary>
        public string? MapFileName { get; set; }

        /// <summary>
        /// The multiplier that is stored in the text file with each map.
        /// The pixels will get multiplied with this multiplier to get in-game units.
        /// </summary>
        public float MapSizeMultiplier { get; set; }

        /// <summary>
        /// The X coordinate that is in the upper left hand corner of the radar.
        /// This is used to position some things according to their coordinates, such as player spawns and nav meshes.
        /// </summary>
        public float UpperLeftWorldXCoordinate { get; set; } = -1;

        /// <summary>
        /// The Y coordinate that is in the upper left hand corner of the radar.
        /// This is used to position some things according to their coordinates, such as player spawns and nav meshes.
        /// </summary>
        public float UpperLeftWorldYCoordinate { get; set; } = -1;

        /// <summary>
        /// The X position of the CT spawn icon on the map (used in the loading screen).
        /// Floating point, 0 is left and 1 is right.
        /// </summary>
        public float CTSpawnMultiplierX { get; set; } = -1;

        /// <summary>
        /// The Y position of the CT spawn icon on the map (used in the loading screen).
        /// Floating point, 0 is top and 1 is bottom.
        /// </summary>
        public float CTSpawnMultiplierY { get; set; } = -1;

        /// <summary>
        /// The X position of the T spawn icon on the map (used in the loading screen).
        /// Floating point, 0 is left and 1 is right.
        /// </summary>
        public float TSpawnMultiplierX { get; set; } = -1;

        /// <summary>
        /// The Y position of the T spawn icon on the map (used in the loading screen).
        /// Floating point, 0 is top and 1 is bottom.
        /// </summary>
        public float TSpawnMultiplierY { get; set; } = -1;

        /// <summary>
        /// The X position of the bomb site A icon on the map (used in the loading screen).
        /// Floating point, 0 is left and 1 is right.
        /// </summary>
        public float BombAX { get; set; } = -1;

        /// <summary>
        /// The Y position of the bomb site A icon on the map (used in the loading screen).
        /// Floating point, 0 is top and 1 is bottom.
        /// </summary>
        public float BombAY { get; set; } = -1;

        /// <summary>
        /// The X position of the bomb site B icon on the map (used in the loading screen).
        /// Floating point, 0 is left and 1 is right.
        /// </summary>
        public float BombBX { get; set; } = -1;

        /// <summary>
        /// The Y position of the bomb site B icon on the map (used in the loading screen).
        /// Floating point, 0 is top and 1 is bottom.
        /// </summary>
        public float BombBY { get; set; } = -1;

        /// <summary>
        /// The bomb damage in this map.
        /// If not specified in a map, the default value is used, which is 500 units.
        /// </summary>
        public float BombDamage { get; set; } = 500;

        /// <summary>
        /// Raw list of entities in this map, as stored in the BSP file.
        /// </summary>
        public string? EntityList { get; set; }

        /// <summary>
        /// Amount of CT spawns on this map, that have priority over other spawns.
        /// (For example getting filled first when playing competitive)
        /// </summary>
        public int AmountPrioritySpawnsCT { get; set; }

        /// <summary>
        /// Amount of total CT spawns on this map.
        /// </summary>
        public int AmountSpawnsCT { get; set; }

        /// <summary>
        /// Amount of T spawns on this map, that have priority over other spawns.
        /// (For example getting filled first when playing competitive)
        /// </summary>
        public int AmountPrioritySpawnsT { get; set; }

        /// <summary>
        /// Amount of total T spawns on this map.
        /// </summary>
        public int AmountSpawnsT { get; set; }

        /// <summary>
        /// Amount of possible hostages on this map.
        /// </summary>
        public int AmountHostages { get; set; }

        /// <summary>
        /// X and Y offset of the coordinates relative to the map's given coordinates as well as a new map size multiplier.
        /// This is used to correct for inaccurate coordinates and scale in the map's associated text file.
        /// </summary>
        public MapCustomOverwriteMapping MapOverwrite { get; set; } = new();

        /// <summary>
        /// Gets whether or not there are any terrorist spawns that have
        /// a higher priority than others.
        /// </summary>
        public bool HasPrioritySpawnsT
        {
            get
            {
                // If there are no spawns with higher priority,
                // then all of them are marked as priority.
                return this.AmountPrioritySpawnsT < this.AmountSpawnsT;
            }
        }

        /// <summary>
        /// Gets whether or not there are any counter-terrorist spawns that have
        /// a higher priority than others.
        /// </summary>
        public bool HasPrioritySpawnsCT
        {
            get
            {
                // If there are no spawns with higher priority,
                // then all of them are marked as priority.
                return this.AmountPrioritySpawnsCT < this.AmountSpawnsCT;
            }
        }

        /// <summary>
        /// Gets whether this map has a NAV file associated with it.
        /// Will only account for BSP-packed NAV file after calling parseBspData().
        /// </summary>
        public bool HasNavFile 
        { 
            get => this.BspFilePath != null || this.NavFileBspPacked;
        }

        /// <summary>
        /// Gets whether this map has a AIN file associated with it.
        /// Will only account for BSP-packed AIN file after calling parseBspData().
        /// </summary>
        public bool HasAinFile
        {
            get => this.AinFilePath != null || this.AinFileBspPacked;
        }

        /// <summary>
        /// Gets whether this map has a BSP map file associated with it.
        /// </summary>
        public bool HasBspFile
        {
            get => this.BspFilePath != null;
        }

        /// <summary>
        /// Gets or sets the player spawns for this map, for both teams.
        /// </summary>
        public List<PlayerSpawn> SpawnPoints { get; set; } = new List<PlayerSpawn>();

        /// <summary>
        /// Gets or sets the navigation mesh for bots (NAV files).
        /// </summary>
        public NavMesh? NavMesh { get; set; }

        /// <summary>
        /// Gets or sets the amount of bomb target brushes (bomb sites) for this map.
        /// </summary>
        public int AmountBombTargets { get; set; } = 0;
    }
}
