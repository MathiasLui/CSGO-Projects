using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Damage_Calculator
{
    /// <summary>
    /// Interaction logic for wndSettings.xaml
    /// </summary>
    public partial class wndSettings : Window
    {
        /// <summary>
        /// The settings of this Window. When creating the Window this is a copy of the current settings to be modified.
        /// </summary>
        private Settings settings;

        /// <summary>
        /// We need this to set the map coordinate offsets for each map respectively.
        /// </summary>
        private SteamShared.Models.CsgoMap currentMap = null;

        private string getCurrentMapDDSName()
        {
            return System.IO.Path.GetFileNameWithoutExtension(currentMap.MapImagePath);
        }

        public wndSettings(SteamShared.Models.CsgoMap currentMap)
        {
            InitializeComponent();
            this.currentMap = currentMap;
            this.MaxHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.settings = (Settings)Globals.Settings.Clone();
            this.fillSettings();
        }

        private void fillSettings()
        {
            // We'll do it the old school way because I have a headache

            // Visuals

            // Theme
            switch (this.settings.Theme)
            {
                case REghZyFramework.Themes.ThemesController.ThemeTypes.Light:
                    this.radioLightTheme.IsChecked = true;
                    break;
                default:
                    this.radioDarkTheme.IsChecked = true;
                    break;
            }

            var mapOverrideItem = this.settings.MapCoordinateOffsets.FirstOrDefault(map => map.DDSFileName == this.getCurrentMapDDSName());
            if (mapOverrideItem != null)
            {
                this.intCurrentMapCoordsOffsetX.Value = (int)mapOverrideItem.CoordOffset.X;
                this.intCurrentMapCoordsOffsetY.Value = (int)mapOverrideItem.CoordOffset.Y;
                this.intCurrentMapMultiplierOverride.Value = mapOverrideItem.MapScale;
            }
            this.txtCurrentMapMultiplier.Content = this.currentMap.MapSizeMultiplier;

            this.mnuShowBombSites.IsChecked = this.settings.ShowBombSites;
            this.mnuShowSpawnAreas.IsChecked = this.settings.ShowSpawnAreas;
            this.mnuShowStandardSpawns.IsChecked = this.settings.ShowStandardSpawns;
            this.mnuShow2v2Spawns.IsChecked = this.settings.Show2v2Spawns;
            this.mnuShowHostageSpawns.IsChecked = this.settings.ShowHostageSpawns;
            this.mnuAllowNonPrioritySpawns.IsChecked = this.settings.AllowNonPrioritySpawns;

            this.colourNavLow.SelectedColor = this.settings.NavLowColour;
            this.colourNavHigh.SelectedColor = this.settings.NavHighColour;
            this.colourNavHover.SelectedColor = this.settings.NavHoverColour;
            
            foreach(string navDisplayMode in Enum.GetNames(typeof(SteamShared.NavDisplayModes)))
            {
                comboNavDisplayModes.Items.Add(navDisplayMode);
                if (navDisplayMode == Enum.GetName(this.settings.NavDisplayMode))
                    comboNavDisplayModes.SelectedItem = navDisplayMode;
            }

            sliderNavAbove.Value = this.settings.ShowNavAreasAbove * 100f;
            sliderNavBelow.Value = this.settings.ShowNavAreasBelow * 100f;

            // Map filter
            this.mnuShowDefusalMaps.IsChecked = this.settings.ShowDefusalMaps;
            this.mnuShowHostageMaps.IsChecked = this.settings.ShowHostageMaps;
            this.mnuShowArmsRaceMaps.IsChecked = this.settings.ShowArmsRaceMaps;
            this.mnuShowDangerZoneMaps.IsChecked = this.settings.ShowDangerZoneMaps;
            this.mnuShowMapsMissingBsp.IsChecked = this.settings.ShowMapsMissingBsp;
            this.mnuShowMapsMissingNav.IsChecked = this.settings.ShowMapsMissingNav;
            this.mnuShowMapsMissingAin.IsChecked = this.settings.ShowMapsMissingAin;
        }

        private void saveSettings()
        {
            // Visuals

            // Theme
            if ((bool)this.radioLightTheme.IsChecked)
                this.settings.Theme = REghZyFramework.Themes.ThemesController.ThemeTypes.Light;
            else
                this.settings.Theme = REghZyFramework.Themes.ThemesController.ThemeTypes.Dark;

            Point newCoords = new Point
            {
                X = this.intCurrentMapCoordsOffsetX.Value ?? 0,
                Y = this.intCurrentMapCoordsOffsetY.Value ?? 0
            };

            var mapOffsetsItem = this.settings.MapCoordinateOffsets.FirstOrDefault(map => map.DDSFileName == this.getCurrentMapDDSName());
            if (mapOffsetsItem != null)
            {
                mapOffsetsItem.CoordOffset = newCoords;
                mapOffsetsItem.MapScale = (float)this.intCurrentMapMultiplierOverride.Value;
            }
            else
            {
                this.settings.MapCoordinateOffsets.Add(new SteamShared.Models.MapCustomOverwriteMapping { DDSFileName = getCurrentMapDDSName(), CoordOffset = newCoords, MapScale = (float)this.intCurrentMapMultiplierOverride.Value });
            }
            this.currentMap.MapOverwrite.CoordOffset = newCoords;
            this.currentMap.MapOverwrite.MapScale = (float)this.intCurrentMapMultiplierOverride.Value;

            this.settings.ShowBombSites = (bool)this.mnuShowBombSites.IsChecked;
            this.settings.ShowSpawnAreas = (bool)this.mnuShowSpawnAreas.IsChecked;
            this.settings.ShowStandardSpawns = (bool)this.mnuShowStandardSpawns.IsChecked;
            this.settings.Show2v2Spawns = (bool)this.mnuShow2v2Spawns.IsChecked;
            this.settings.ShowHostageSpawns = (bool)this.mnuShowHostageSpawns.IsChecked;
            this.settings.AllowNonPrioritySpawns = (bool)this.mnuAllowNonPrioritySpawns.IsChecked;

            this.settings.NavLowColour = this.colourNavLow.SelectedColor ?? Globals.Settings.NavLowColour;
            this.settings.NavHighColour = this.colourNavHigh.SelectedColor ?? Globals.Settings.NavHighColour;
            this.settings.NavHoverColour = this.colourNavHover.SelectedColor ?? Globals.Settings.NavHoverColour;

            this.settings.NavDisplayMode = (SteamShared.NavDisplayModes)Enum.Parse(typeof(SteamShared.NavDisplayModes), comboNavDisplayModes.SelectedItem.ToString());

            this.settings.ShowNavAreasAbove = sliderNavAbove.Value / 100f;
            this.settings.ShowNavAreasBelow = sliderNavBelow.Value / 100f;

            // Map filter
            this.settings.ShowDefusalMaps = (bool)this.mnuShowDefusalMaps.IsChecked;
            this.settings.ShowHostageMaps = (bool)this.mnuShowHostageMaps.IsChecked;
            this.settings.ShowArmsRaceMaps = (bool)this.mnuShowArmsRaceMaps.IsChecked;
            this.settings.ShowDangerZoneMaps = (bool)this.mnuShowDangerZoneMaps.IsChecked;
            this.settings.ShowMapsMissingBsp = (bool)this.mnuShowMapsMissingBsp.IsChecked;
            this.settings.ShowMapsMissingNav = (bool)this.mnuShowMapsMissingNav.IsChecked;
            this.settings.ShowMapsMissingAin = (bool)this.mnuShowMapsMissingAin.IsChecked;

            Globals.Settings = this.settings;
            Globals.SaveSettings();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Tell main window to reload with new settings
            this.saveSettings();
            this.Close();
        }

        private void sliderNav_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsInitialized)
                return;

            if(sender == sliderNavBelow && sliderNavAbove.Value > sliderNavBelow.Value)
                sliderNavAbove.Value = sliderNavBelow.Value;
            else if (sender == sliderNavAbove && sliderNavBelow.Value < sliderNavAbove.Value)
                sliderNavBelow.Value = sliderNavAbove.Value;

            txtNavAbove.Content = $"{this.sliderNavAbove.Value} %";
            txtNavBelow.Content = $"{this.sliderNavBelow.Value} %";
        }
    }
}
