using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class PlayerSpawn
    {
        /// <summary>
        /// The team of the player spawn.
        /// </summary>
        public ePlayerTeam Team { get; set; }

        /// <summary>
        /// If this spawn is priority. If a team has priority spawns, fill them first.
        /// </summary>
        public bool IsPriority { get; set; }

        /// <summary>
        /// The world position of the player spawn.
        /// </summary>
        public Vector3? Origin { get; set; }

        /// <summary>
        /// The rotation of the player spawn.
        /// Y is used most here, 0 is East, goes to 360 clockwise.
        /// </summary>
        public Vector3? Angles { get; set; }

        /// <summary>
        /// The type of spawn (standard, 2v2, hostage, ...)
        /// </summary>
        public eSpawnType Type { get; set; }
    }

    public enum ePlayerTeam { Terrorist, CounterTerrorist }

    public enum eSpawnType 
    { 
        /// <summary>
        /// Standard spawnpoints, also including all types that are not separate otherwise.
        /// </summary>
        Standard, 

        /// <summary>
        /// A 2v2 spawn.
        /// </summary>
        Wingman, 

        /// <summary>
        /// A hostage spawn.
        /// </summary>
        Hostage }
}
