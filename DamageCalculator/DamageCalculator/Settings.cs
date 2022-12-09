using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Damage_Calculator
{
    public class Settings : ICloneable
    {
        [XmlIgnore]
        public static readonly string SettingsFileName = "settings.xml";

        public Settings() { /* Do nothing */ }

        // VISUAL SETTINGS

        public REghZyFramework.Themes.ThemesController.ThemeTypes Theme { get; set; } = REghZyFramework.Themes.ThemesController.ThemeTypes.Dark;
        public List<SteamShared.Models.MapCustomOverwriteMapping> MapCoordinateOffsets { get; set; } = new();
        public bool ShowBombSites { get; set; } = true;
        public bool ShowSpawnAreas { get; set; } = true;
        public bool ShowStandardSpawns { get; set; } = true;
        public bool Show2v2Spawns { get; set; } = true;
        public bool ShowHostageSpawns { get; set; } = true;
        public bool AllowNonPrioritySpawns { get; set; } = true;
        public System.Windows.Media.Color NavLowColour { get; set; } = System.Windows.Media.Color.FromArgb(255, 20, 20, 20);
        public System.Windows.Media.Color NavHighColour { get; set; } = System.Windows.Media.Color.FromArgb(140, 255, 255, 255);
        public System.Windows.Media.Color NavHoverColour { get; set; } = System.Windows.Media.Color.FromArgb(140, 255, 165, 0);
        public SteamShared.NavDisplayModes NavDisplayMode { get; set; } = SteamShared.NavDisplayModes.None;
        public double ShowNavAreasAbove { get; set; } = 0;
        public double ShowNavAreasBelow { get; set; } = 1;

        // MAP FILTERS

        public bool ShowDefusalMaps { get; set; } = true;
        public bool ShowHostageMaps { get; set; } = true;
        public bool ShowArmsRaceMaps { get; set; } = true;
        public bool ShowDangerZoneMaps { get; set; } = true;
        public bool ShowMapsMissingBsp { get; set; } = true;
        public bool ShowMapsMissingNav { get; set; } = true;
        public bool ShowMapsMissingAin { get; set; } = true;

        // OTHER

        public ushort NetConPort { get; set; } = 2121;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
