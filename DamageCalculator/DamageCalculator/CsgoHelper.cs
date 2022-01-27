using Damage_Calculator.Models;
using Damage_Calculator.ZatVdfParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damage_Calculator
{
    public class CsgoHelper
    {
        public string CsgoPath { get; set; }

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
            return this.Validate(this.CsgoPath);
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

        public List<CsgoMapOverview> GetMaps()
        {
            List<string> mapTextFiles = Directory.GetFiles(System.IO.Path.Combine(this.CsgoPath, "csgo\\resource\\overviews")).ToList().Where(f => f.ToLower().EndsWith(".txt")).Where(f =>
                this.mapFileNameValid(f)).ToList();

            List<CsgoMapOverview> maps = new List<CsgoMapOverview>();

            foreach (string file in mapTextFiles)
            {
                string potentialRadarFile = System.IO.Path.Combine(this.CsgoPath, "csgo\\resource\\overviews", System.IO.Path.GetFileNameWithoutExtension(file) + "_radar.dds");
                var map = new CsgoMapOverview();
                if (File.Exists(potentialRadarFile))
                {
                    map.MapImagePath = potentialRadarFile;
                }

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

                map.MapFileName = System.IO.Path.GetFileNameWithoutExtension(file).Split('_').Last();
                
                DDSImage image;
                try
                {
                    image = new DDSImage(System.IO.File.ReadAllBytes(map.MapImagePath));
                }
                catch
                {
                    continue;
                }

                if (image.BitmapImage.Width != image.BitmapImage.Height)
                    continue;

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
            string filePath = Path.Combine(this.CsgoPath, "csgo\\scripts\\items\\items_game.txt");
            if (!File.Exists(filePath))
                return null;

            var vdfItems = new VDFFile(filePath);
            Element prefabs = vdfItems["items_game"]?["prefabs"];
            Element items = vdfItems["items_game"]?["items"];

            if (prefabs == null || items == null)
                return null;

            var weapons = new List<CsgoWeapon>();

            foreach(var item in items.Children)
            {
                string itemPrefab = item["prefab"]?.Value;
                string itemName = item["name"].Value;

                if (itemPrefab == null || !itemName.StartsWith("weapon_"))
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

        private bool tryPopulateWeapon(CsgoWeapon weapon, Element prefabs, string prefabName, List<string> prefabTrace = null)
        {
            Element prefab = prefabs[prefabName];

            if (prefab == null)
                // Prefab not existent (example was prefab named "valve csgo_tool")
                return false;

            string nextPrefab = prefab["prefab"]?.Value;

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
                string damage = attributes["damage"]?.Value;
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
                string penetration = attributes["armor ratio"]?.Value;
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
                string dropoff = attributes["range modifier"]?.Value;
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
                string maxrange = attributes["range"]?.Value;
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
                string headshotModifier = attributes["headshot multiplier"]?.Value;
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

            prefabTrace.Add(prefab.Name);

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
    }
}
