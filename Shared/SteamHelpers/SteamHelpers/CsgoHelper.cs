using Shared.Models;
using Shared.ZatVdfParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class CsgoHelper
    {
        public string? CsgoPath { get; set; }

        /// <summary>
        /// Gets the prefixes allowed for maps when using <see cref="GetMaps"/>.
        /// </summary>
        private readonly string[] validMapPrefixes = new[]
        {
            "de",
            "cs",
            "dz",
            "ar"
        };

        /// <summary>
        /// Gets the files relative to the CS:GO install path that are checked when <see cref="Validate(string)">validating</see>.
        /// </summary>
        private readonly string[] filesToValidate = new[]
        {
            "csgo\\scripts\\items\\items_game.txt" // Item info (weapon stats etc.)
        };

        /// <summary>
        /// Gets the directories relative to the CS:GO install path that are checked when <see cref="Validate(string)">validating</see>.
        /// </summary>
        private readonly string[] directoriesToValidate = new[]
        {
            "csgo\\resource\\overviews" // Map overviews
        };

        public CsgoHelper()
        {
            // Nothing to do
        }

        public CsgoHelper(string csgoPath)
        {
            this.CsgoPath = csgoPath;
        }

        /// <summary>
        /// Validates files and directories for CS:GO installed in the <see cref="CsgoPath">path</see>.
        /// </summary>
        /// <returns>whether the files and directories exist.</returns>
        public bool Validate()
        {
            return this.Validate(this.CsgoPath!);
        }


        /// <summary>
        /// Validates files and directories for CS:GO installed in the given path.
        /// </summary>
        /// <param name="csgoPath">The path to the CS:GO install directory, in which the executable resides.</param>
        /// <returns>whether the files and directories exist.</returns>
        public bool Validate(string csgoPath)
        {
            foreach (string file in this.filesToValidate)
            {
                if (!File.Exists(Path.Combine(csgoPath, file)))
                    return false;
            }

            foreach (string dir in this.directoriesToValidate)
            {
                if (!Directory.Exists(Path.Combine(csgoPath, dir)))
                    return false;
            }

            return true;
        }

        public List<CsgoMap> GetMaps()
        {
            List<string> mapTextFiles = Directory.GetFiles(System.IO.Path.Combine(this.CsgoPath!, "csgo\\resource\\overviews")).ToList().Where(f => f.ToLower().EndsWith(".txt")).Where(f =>
                this.mapFileNameValid(f)).ToList();

            List<CsgoMap> maps = new List<CsgoMap>();

            foreach (string file in mapTextFiles)
            {
                var map = new CsgoMap();

                // Save path to radar file if available
                string potentialRadarFile = System.IO.Path.Combine(this.CsgoPath!, "csgo\\resource\\overviews", System.IO.Path.GetFileNameWithoutExtension(file) + "_radar.dds");
                if (File.Exists(potentialRadarFile))
                {
                    map.MapImagePath = potentialRadarFile;
                }

                // Save path to BSP file if available
                string potentialBspFile = System.IO.Path.Combine(this.CsgoPath!, "csgo\\maps", System.IO.Path.GetFileNameWithoutExtension(file) + ".bsp");
                if (File.Exists(potentialBspFile))
                {
                    map.BspFilePath = potentialBspFile;
                }

                // Save path to NAV file if available
                string potentialNavFile = System.IO.Path.Combine(this.CsgoPath!, "csgo\\maps", System.IO.Path.GetFileNameWithoutExtension(file) + ".nav");
                if (File.Exists(potentialNavFile))
                {
                    map.NavFilePath = potentialNavFile;
                }

                // Save path to AIN file if available
                string potentialAinFile = System.IO.Path.Combine(this.CsgoPath!, "csgo\\maps\\graphs", System.IO.Path.GetFileNameWithoutExtension(file) + ".ain");
                if (File.Exists(potentialAinFile))
                {
                    map.AinFilePath = potentialAinFile;
                }

                // Set map type
                switch (System.IO.Path.GetFileNameWithoutExtension(file).Split('_').First().ToLower())
                {
                    case "de":
                        map.MapType = CsgoMap.eMapType.Defusal;
                        break;
                    case "cs":
                        map.MapType = CsgoMap.eMapType.Hostage;
                        break;
                    case "dz":
                        map.MapType = CsgoMap.eMapType.DangerZone;
                        break;
                    case "ar":
                        map.MapType = CsgoMap.eMapType.ArmsRace;
                        break;
                    default:
                        map.MapType = CsgoMap.eMapType.Undefined;
                        break;
                }

                // Get properties from accompanying text file
                var vdf = new VDFFile(file);
                if (vdf.RootElements.Count > 0)
                {
                    var rootNode = vdf.RootElements.First();
                    if (float.TryParse(rootNode["scale"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale))
                    {
                        map.MapSizeMultiplier = scale;
                    }
                    if (float.TryParse(rootNode["pos_x"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float posX))
                    {
                        map.UpperLeftWorldXCoordinate = posX;
                    }
                    if (float.TryParse(rootNode["pos_y"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float posY))
                    {
                        map.UpperLeftWorldYCoordinate = posY;
                    }
                    if (float.TryParse(rootNode["CTSpawn_x"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ctX))
                    {
                        map.CTSpawnMultiplierX = ctX;
                    }
                    if (float.TryParse(rootNode["CTSpawn_y"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ctY))
                    {
                        map.CTSpawnMultiplierY = ctY;
                    }
                    if (float.TryParse(rootNode["TSpawn_x"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tX))
                    {
                        map.TSpawnMultiplierX = tX;
                    }
                    if (float.TryParse(rootNode["TSpawn_y"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tY))
                    {
                        map.TSpawnMultiplierY = tY;
                    }
                    if (float.TryParse(rootNode["bombA_x"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bombAX))
                    {
                        map.BombAX = bombAX;
                    }
                    if (float.TryParse(rootNode["bombA_y"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bombAY))
                    {
                        map.BombAY = bombAY;
                    }
                    if (float.TryParse(rootNode["bombB_x"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bombBX))
                    {
                        map.BombBX = bombBX;
                    }
                    if (float.TryParse(rootNode["bombB_y"]?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bombBY))
                    {
                        map.BombBY = bombBY;
                    }
                }

                // Save map name without prefix
                map.MapFileName = System.IO.Path.GetFileNameWithoutExtension(file).Split('_').Last();
                
                DDSImage image;
                try
                {
                    // Read actual radar
                    image = new DDSImage(System.IO.File.ReadAllBytes(map.MapImagePath!));
                }
                catch
                {
                    continue;
                }

                if (image.BitmapImage.Width != image.BitmapImage.Height)
                    // We only want square map images, which should normally always be given
                    continue;

                // Some workaround I found online for some thread error I forgot
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    map.MapImage = Globals.BitmapToImageSource(image.BitmapImage);
                });

                maps.Add(map);
            }

            return maps;
        }

        public List<CsgoWeapon> GetWeapons()
        {
            string filePath = Path.Combine(this.CsgoPath!, "csgo\\scripts\\items\\items_game.txt");
            if (!File.Exists(filePath))
                return null!;

            var vdfItems = new VDFFile(filePath);
            Element prefabs = vdfItems["items_game"]?["prefabs"]!;
            Element items = vdfItems["items_game"]?["items"]!;

            if (prefabs == null || items == null)
                return null!;

            var weapons = new List<CsgoWeapon>();

            foreach(var item in items.Children)
            {
                string? itemPrefab = item["prefab"]?.Value!;
                string? itemName = item["name"].Value;

                if (itemPrefab == null || !itemName!.StartsWith("weapon_"))
                    continue;

                var weapon = new CsgoWeapon();
                weapon.ClassName = itemName;

                if(this.tryPopulateWeapon(weapon, prefabs, itemPrefab))
                {
                    weapons.Add(weapon);
                }
            }

            return weapons;
        }

        private bool tryPopulateWeapon(CsgoWeapon weapon, Element prefabs, string prefabName, List<string>? prefabTrace = null)
        {
            Element prefab = prefabs[prefabName];

            if (prefab == null)
                // Prefab not existent (example was prefab named "valve csgo_tool")
                return false;

            string nextPrefab = prefab["prefab"]?.Value!;

            if (prefab == null || (nextPrefab == null && prefabTrace?.FirstOrDefault(pr => pr == "primary" || pr == "secondary") == null))
                // We've reached the end of abstraction but it wasn't found to be primary nor secondary
                return false;

            bool gatheredAllInfo = true;

            Element attributes = prefab["attributes"];

            if (attributes == null)
                return false;

            // =========================== ATTRIBUTES =========================== //

            // Base damage
            if (weapon.BaseDamage == -1) 
            {
                string damage = attributes["damage"]?.Value!;
                if (damage != null)
                {
                    // damage field exists
                    if (int.TryParse(damage, out int dmg))
                    {
                        weapon.BaseDamage = dmg;
                    }
                }
                else
                    gatheredAllInfo = false;
            }

            // Armor penetration
            if (weapon.ArmorPenetration == -1)
            {
                string penetration = attributes["armor ratio"]?.Value!;
                if (penetration != null)
                {
                    // Armor penetration field exists
                    if (float.TryParse(penetration, NumberStyles.Any, CultureInfo.InvariantCulture, out float pen))
                    {
                        weapon.ArmorPenetration = pen * 100f / 2f;
                    }
                }
                else
                    gatheredAllInfo = false;
            }

            // Damage dropoff
            if (weapon.DamageDropoff == -1)
            {
                string dropoff = attributes["range modifier"]?.Value!;
                if (dropoff != null)
                {
                    // Damage dropoff field exists
                    if (double.TryParse(dropoff, NumberStyles.Any, CultureInfo.InvariantCulture, out double drop))
                    {
                        weapon.DamageDropoff = drop;
                    }
                }
                else
                    gatheredAllInfo = false;
            }

            // Max range
            if (weapon.MaxBulletRange == -1)
            {
                string maxrange = attributes["range"]?.Value!;
                if (maxrange != null)
                {
                    // Max range field exists
                    if (int.TryParse(maxrange, out int range))
                    {
                        weapon.MaxBulletRange = range;
                    }
                }
                else
                    gatheredAllInfo = false;
            }

            // Headshot modifier
            if (weapon.HeadshotModifier == -1)
            {
                string headshotModifier = attributes["headshot multiplier"]?.Value!;
                if (headshotModifier != null)
                {
                    // Headshot modifier field exists
                    if (float.TryParse(headshotModifier, NumberStyles.Any, CultureInfo.InvariantCulture, out float hs))
                    {
                        weapon.HeadshotModifier = hs;
                    }
                }
                else
                    gatheredAllInfo = false;
            }

            // ================================================================== //

            if (gatheredAllInfo || nextPrefab == null)
                return true; // ?

            if (prefabTrace == null)
                prefabTrace = new List<string>();

            prefabTrace.Add(prefab.Name!);

            return this.tryPopulateWeapon(weapon, prefabs, nextPrefab, prefabTrace);
        }

        private bool mapFileNameValid(string mapPath)
        {
            string fileName = Path.GetFileName(mapPath.ToLower());

            foreach(string prefix in this.validMapPrefixes)
            {
                if (fileName.StartsWith(prefix.ToLower()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Reads entity list from uncompressed BSP file.
        /// </summary>
        /// <param name="bspFilePath">The absolute path to the BSP file.</param>
        /// <returns>the entity list, null if actual length differed from length specified in file, or a general error occurred.</returns>
        public string ReadEntityListFromBsp(string bspFilePath)
        {
            using(var bspFile = File.OpenRead(bspFilePath))
            {
                using(var reader = new BinaryReader(bspFile))
                {
                    reader.BaseStream.Position = 8; // Skip magic bytes and file version
                    int offset = reader.ReadInt32(); // Lump data offset from beginning of file
                    int length = reader.ReadInt32(); // Length of lump data

                    reader.BaseStream.Position = offset;
                    char[] chars = new char[length];
                    int charsRead = reader.Read(chars, 0, length);

                    if(charsRead == length)
                    {
                        // Everything was read
                        return new string(chars);
                    }
                }
            }
            return null!;
        }

        /// <summary>
        /// Reads packed files from a BSP and returns whether any 1. NAV or 2. AIN files were found.
        /// </summary>
        /// <param name="bspFilePath">The absolute path to the BSP file.</param>
        /// <returns>A tuple containing whether nav or ain files were found, in that order.</returns>
        public (bool, bool, NavMesh) ReadIfPackedNavFilesInBsp(string bspFilePath)
        {
            bool navFound = false;
            bool ainFound = false;
            byte[] readZipBytes = null!;

            using (var bspFile = File.OpenRead(bspFilePath))
            {
                using (var reader = new BinaryReader(bspFile))
                {
                    // Stuff before lumps + pakfile index * lump array item length
                    reader.BaseStream.Position = 8 + (40 * 16);

                    // Get lump pos and size
                    int offset = reader.ReadInt32();
                    int length = reader.ReadInt32();

                    // Read zip file
                    reader.BaseStream.Position = offset;
                    readZipBytes = reader.ReadBytes(length);
                }
            }

            if (readZipBytes == null)
                return (false, false, null!);

            using (var stream = new MemoryStream(readZipBytes))
            {
                using(var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    NavMesh? nav = null;
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.FullName.EndsWith(".nav"))
                        {
                            // Found a packed NAV file
                            navFound = true;
                            nav = NavFile.Parse(entry.Open());
                        }
                        if(entry.FullName.EndsWith(".ain"))
                            // Found a packed AIN file
                            ainFound = true;

                        if (navFound && ainFound)
                            // If both already found, return prematurely
                            return (true, true, nav!);
                    }
                    return (navFound, ainFound, nav!);
                }
            }
        }

        private bool isLumpUnused(byte[] lump)
        {
            for(int i = 0; i < lump.Length; i++)
            {
                if (lump[i] != 0)
                    return false;
            }
            return true;
        }
    }
}
