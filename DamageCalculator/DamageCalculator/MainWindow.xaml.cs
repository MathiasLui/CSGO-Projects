using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SteamShared.Models;
using SteamShared.ZatVdfParser;
using System.Xml.Serialization;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Web.Services.Description;
using System.Net.Sockets;
using SteamShared;
using System.Reflection;

namespace Damage_Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string FilesPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSGO Damage Calculator");
        public static readonly string WeaponsFileExtension = ".wpd";

        private readonly string placeholderText = "None";

        private static readonly double eyeLevelStanding = 64.093811;
        private static readonly double eyeLevelCrouching = 46.076218;
        private static readonly int heightStanding = 72;
        private static readonly int heightCrouching = 54;
        private static readonly int playerWidth = 32;

        /// <summary>
        /// Holds current in-game mouse coordinates when hovering over the map
        /// </summary>
        private Vector3 currentMouseCoord = Vector3.Zero;

        /// <summary>
        /// Gets or sets the point that will be there when left-clicking a map in shooting mode.
        /// </summary>
        private MapPoint targetPoint = new MapPoint();

        /// <summary>
        /// Gets or sets the point that will be there when left-clicking a map in bomb mode.
        /// </summary>
        private MapPoint bombPoint = new MapPoint();

        /// <summary>
        /// Gets or sets the point that will be there when right-clicking a map.
        /// </summary>
        private MapPoint playerPoint = new MapPoint();

        // Point and line colours
        private Color leftClickPointColour = Color.FromArgb(140, 255, 0, 0);
        private Color rightClickPointColour = Color.FromArgb(140, 0, 255, 0);
        private Color connectingLineColour = Color.FromArgb(140, 255, 255, 255);

        /// <summary>
        /// The height layer if several NAV areas overlap at the mouse position (0 is the bottom most (in UI should show this + 1), -1 if there is no area at the mouse)
        /// </summary>
        private int currentHeightLayer = -1;
        private List<NavArea> hoveredNavAreas = null;
        private bool userChangedLayer = false;
        private int userHeightLayerOffset = 0;

        private System.Windows.Shapes.Path connectingLine = new System.Windows.Shapes.Path();
        private bool redrawLine = false;

        private eDrawMode DrawMode = eDrawMode.Shooting;

        // Extra icons
        private Image CTSpawnIcon;
        private Image TSpawnIcon;
        private Image ASiteIcon;
        private Image BSiteIcon;

        /// <summary>
        /// The amount of distance in in-game units, that is drawn on the map.
        /// If in bomb drawing mode, this will be the minimum distance that the bomb will calculate the damage at.
        /// </summary>
        private double unitsDistanceMin = -1;

        /// <summary>
        /// The maximum distance that the bomb will calculate the damage at, in units.
        /// -1 if not in bomb mode.
        /// </summary>
        private double unitsDistanceMax = -1;

        /// <summary>
        /// Gets or sets the currently loaded map.
        /// </summary>
        private CsgoMap loadedMap;
        private CsgoWeapon selectedWeapon;

        private BackgroundWorker bgWorker = new BackgroundWorker();
        private List<ComboBoxItem> allMaps;

        private bool lineDrawn = false;

        public MainWindow()
        {
            InitializeComponent();
            Globals.LoadSettings();

            SteamShared.Globals.Settings.CsgoHelper.CsgoPath = SteamShared.Globals.Settings.SteamHelper.GetGamePathFromExactName("Counter-Strike: Global Offensive");
            if (SteamShared.Globals.Settings.CsgoHelper.CsgoPath == null)
            {
                ShowMessage.Error("Make sure you have installed CS:GO and Steam correctly.");
                this.Close();
                return;
            }

            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.WorkerReportsProgress = true;

            this.gridLoading.Visibility = Visibility.Visible;
            bgWorker.RunWorkerAsync();
        }

        #region Canvas Helper Methods
        private bool canvasContains(UIElement element)
        {
            return this.mapCanvas.Children.Contains(element);
        }

        private bool canvasContains(Predicate<UIElement> predicate)
        {
            foreach (UIElement child in this.mapCanvas.Children)
            {
                if (predicate(child))
                    return true;
            }

            return false;
        }

        private void canvasAdd(UIElement element)
        {
            this.mapCanvas.Children.Add(element);
        }

        private void canvasRemove(UIElement element)
        {
            this.mapCanvas.Children.Remove(element);
        }

        private void canvasRemoveWhere(Predicate<UIElement> predicate)
        {
            // Mark elements to remove because you can't continue iterating after you deleted an item
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement child in this.mapCanvas.Children)
            {
                if(predicate(child))
                    elementsToRemove.Add(child);
            }

            // Remove marked elements
            foreach (UIElement element in elementsToRemove)
                this.mapCanvas.Children.Remove(element);
        }

        private void canvasClear()
        {
            this.mapCanvas.Children.Clear();
        }
        #endregion

        private static string calculateMD5(string filename)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void updateMapsWithCurrentFilter()
        {
            if (!this.IsInitialized)
                return;

            string prevSelectedMapName = ((this.comboBoxMaps.SelectedItem as ComboBoxItem)?.Tag as CsgoMap)?.MapFileName;

            // Add maps
            var newMaps = new List<ComboBoxItem>();

            foreach (var mapItem in this.allMaps)
            {
                var newMap = new ComboBoxItem();
                var map = (CsgoMap)mapItem.Tag;

                string mapNameWithPrefix = System.IO.Path.GetFileNameWithoutExtension(map.MapImagePath).ToLower();

                // Filter map type
                if (mapNameWithPrefix.StartsWith("de") && Globals.Settings.ShowDefusalMaps == false)
                    continue;
                if (mapNameWithPrefix.StartsWith("cs") && Globals.Settings.ShowHostageMaps == false)
                    continue;
                if (mapNameWithPrefix.StartsWith("ar") && Globals.Settings.ShowArmsRaceMaps == false)
                    continue;
                if (mapNameWithPrefix.StartsWith("dz") && Globals.Settings.ShowDangerZoneMaps == false)
                    continue;

                // Filter file existence
                if (!map.HasBspFile && !Globals.Settings.ShowMapsMissingBsp)
                    continue;
                if (!map.HasNavFile && !Globals.Settings.ShowMapsMissingNav)
                    continue;
                if (!map.HasAinFile && !Globals.Settings.ShowMapsMissingAin)
                    continue;

                newMap.Tag = map;
                newMap.Content = map.MapFileName;
                newMaps.Add(newMap);
            }

            this.comboBoxMaps.ItemsSource = newMaps.OrderBy(m => m.Content);
            if (newMaps.Count > 0)
            {
                if (prevSelectedMapName != null)
                {
                    // Reselect the map the user had previously selected if it's still in the list
                    ComboBoxItem foundMap = newMaps.FirstOrDefault(item => (item.Tag as CsgoMap)?.MapFileName == prevSelectedMapName);
                    if (foundMap != null)
                    {
                        // Previously selected map is still in the list so select it
                        this.comboBoxMaps.SelectedItem = foundMap;
                    }
                    else
                    {
                        // Map is not in this selection anymore so select the first one
                        this.comboBoxMaps.SelectedIndex = 0;
                    }
                }
                else
                {
                    // User didn't have any map selected (shouldn't be the case, however)
                    this.comboBoxMaps.SelectedIndex = 0;
                }
            }
        }

        #region background worker
        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                // Add maps
                var maps = new List<ComboBoxItem>();

                foreach (var map in e.UserState as List<CsgoMap>)
                {
                    var item = new ComboBoxItem();

                    item.Tag = map;
                    item.Content = map.MapFileName;

                    maps.Add(item);
                }

                this.allMaps = maps;
                this.comboBoxMaps.ItemsSource = maps.OrderBy(m => m.Content);
                if (maps.Count > 0)
                    this.comboBoxMaps.SelectedIndex = 0;

                this.canvasReload();
            }
            else if(e.ProgressPercentage == 1)
            {
                // Add weapons
                var weaponItems = new List<ComboBoxItem>();

                foreach (var wpn in e.UserState as List<CsgoWeapon>)
                {
                    var item = new ComboBoxItem();

                    item.Tag = wpn;
                    item.Content = wpn.ClassName.Substring(wpn.ClassName.IndexOf('_') + 1);

                    weaponItems.Add(item);
                }

                comboWeapons.ItemsSource = weaponItems.OrderBy(w => w.Content);
                if (weaponItems.Count > 0)
                    this.comboWeapons.SelectedIndex = 0;
            }
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.gridLoading.Visibility = Visibility.Collapsed;
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var maps = SteamShared.Globals.Settings.CsgoHelper.GetMaps();
            bgWorker.ReportProgress(0, maps);
            var serializer = new XmlSerializer(typeof(List<CsgoWeapon>));

            List<CsgoWeapon> weapons;

            string itemsFile = System.IO.Path.Combine(SteamShared.Globals.Settings.CsgoHelper.CsgoPath, "csgo\\scripts\\items\\items_game.txt");
            string saveFileDir = MainWindow.FilesPath;
            string currentHash = calculateMD5(itemsFile);

            if (Directory.Exists(saveFileDir))
            {
                string[] files = Directory.GetFiles(saveFileDir);
                string weaponsFileName = files.FirstOrDefault(file => file.ToLower().EndsWith(MainWindow.WeaponsFileExtension));

                if (weaponsFileName != null)
                {
                    // WPD file found, compare hash and file name (which is old hash)
                    string oldHash = System.IO.Path.GetFileNameWithoutExtension(weaponsFileName);

                    if (currentHash == oldHash)
                    {
                        // Weapons didn't update since last time so just load them
                        weapons = (List<CsgoWeapon>)serializer.Deserialize(new FileStream(System.IO.Path.Combine(saveFileDir, currentHash + MainWindow.WeaponsFileExtension), FileMode.Open));
                        bgWorker.ReportProgress(1, weapons);
                        return;
                    }
                    else
                    {
                        // Weapons need to be updated so delete any WPD files (Weapon Parse Data)
                        this.clearWeaponDataFiles(files);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(saveFileDir);
            }

            // We didn't return cause we didn't find an up-to-date WPD file so parse new weapon data
            weapons = SteamShared.Globals.Settings.CsgoHelper.GetWeapons();
            serializer.Serialize(new FileStream(System.IO.Path.Combine(saveFileDir, currentHash + MainWindow.WeaponsFileExtension), FileMode.Create), weapons);
            bgWorker.ReportProgress(1, weapons);
        }
        #endregion

        private void clearWeaponDataFiles(string[] files)
        {
            foreach (string file in files)
            {
                // Delete all files that contain weapon info (other files we wanna keep)
                if (file.ToLower().EndsWith(MainWindow.WeaponsFileExtension))
                    File.Delete(file);
            }
        }

        private void resetCanvas(bool updatedViewSettings = false)
        {
            if (this.IsInitialized)
            {
                if (!updatedViewSettings)
                {
                    this.targetPoint = null;
                    this.playerPoint = null;
                    this.connectingLine = null;
                    this.bombPoint = null;
                    this.unitsDistanceMin = -1;
                    this.unitsDistanceMax = -1;
                    this.textDistanceMetres.Text = "0";
                    this.textDistanceUnits.Text = "0";
                    this.txtResult.Text = "0";
                    this.txtResultArmor.Text = "0";
                    this.txtResultBombMin.Text = "0";
                    this.txtResultBombMedian.Text = "0";
                    this.txtResultBombMax.Text = "0";
                    this.txtResultArmorBombMin.Text = "0";
                    this.txtResultArmorBombMedian.Text = "0";
                    this.txtResultArmorBombMax.Text = "0";
                    this.txtTimeRunning.Text = "0";
                    this.txtTimeWalking.Text = "0";
                    this.txtTimeCrouching.Text = "0";
                }
                this.UpdateLayout();
                this.canvasClear();
                mapCanvas.CacheMode = new BitmapCache((mapCanvas.CacheMode as BitmapCache).RenderAtScale);
            }
        }

        private void loadMap(CsgoMap map)
        {
            if (map.BspFilePath != null)
            {
                // Map radar has an actual existing BSP map file
                this.parseBspData(map);
            }

            // Set indicator checkboxes
            this.chkHasMapFile.IsChecked = map.BspFilePath != null;

            this.chkHasNavFile.IsChecked = map.NavFilePath != null;
            this.chkHasAinFile.IsChecked = map.AinFilePath != null;

            // Set packed indicators for indicator checkboxes
            this.txtNavFilePacked.Visibility = map.NavFileBspPacked ? Visibility.Visible : Visibility.Collapsed;
            this.txtAinFilePacked.Visibility = map.AinFileBspPacked ? Visibility.Visible : Visibility.Collapsed;

            this.resetCanvas();
            this.rightZoomBorder.Reset();
            mapImage.Source = map.MapImage;

            if (map.MapType == CsgoMap.eMapType.Defusal)
            {
                this.radioModeBomb.IsEnabled = true;
                this.txtBombMaxDamage.Text = map.BombDamage.ToString();
                this.txtBombRadius.Text = (map.BombDamage * 3.5f).ToString();
            }
            else
            {
                this.radioModeBomb.IsEnabled = false;
                // Select the only other working one in that case
                this.radioModeShooting.IsChecked = true;
                this.txtBombMaxDamage.Text = this.txtBombRadius.Text = "None";
            }

            this.txtAmountBombTargets.Text = map.AmountBombTargets.ToString(); // We always count these so it will just be 0 if no targets exist

            // Set the map's coordinates offset from the settings file in case we have a manual offset
            var mapOffsetMapping = Globals.Settings.MapCoordinateOffsets.FirstOrDefault(m => m.DDSFileName == System.IO.Path.GetFileNameWithoutExtension(map.MapImagePath));
            if (mapOffsetMapping != null)
                map.MapOverwrite = mapOffsetMapping;

            this.loadedMap = map;
        }

        private void parseBspData(CsgoMap map)
        {
            // Reset values so that they are filled with new values if the map is updated (prevents multiple identical spawns being drawn)
            map.AmountPrioritySpawnsCT = 0;
            map.AmountPrioritySpawnsT = 0;
            map.AmountSpawnsCT = 0;
            map.AmountSpawnsT = 0;
            map.AmountHostages = 0;
            map.SpawnPoints.Clear();

            map.EntityList = SteamShared.Globals.Settings.CsgoHelper.ReadEntityListFromBsp(map.BspFilePath);

            // Current format for one entity is: 
            //
            //  {
            //      "property"  "value"
            //  }
            //
            // but we want this format like in VDF files, so we can use our VDF parser without modifying it (laziness):
            //
            //  "sometext"
            //  {
            //      "property" "value"
            //  }
            //

            map.AmountBombTargets = 0; // Just make sure we're counting from 0

            // Separate all entities, which temporarily removes curly braces from the start and/or end of entities
            string[] entities = map.EntityList.Split(new string[] { "}\n{" }, StringSplitOptions.None);
            for (int i = 0; i < entities.Length; i++)
            {
                // Add start or end curly brace back, if nonexistent, because we removed it during separation
                if (!entities[i].StartsWith("{"))
                    entities[i] = "{" + entities[i];
                else if (!entities[i].EndsWith("}"))
                    entities[i] += "}";

                // Add a generic name for the object, to fool it into complying with normal VDF standards,
                // we'll just call every entity "entity" for simplicity
                entities[i] = "\"entity\"\n" + entities[i];

                VDFFile vdf = new VDFFile(entities[i], parseTextDirectly: true);
                var entityRootVdf = vdf["entity"];
                string className = entityRootVdf["classname"].Value;

                // Check for map parameters entity, which contains stuff like the bomb radius, if custom
                if (className == "info_map_parameters")
                {
                    string bombRadius = entityRootVdf["bombradius"]?.Value;
                    if (bombRadius != null)
                    {
                        // Map has custom bomb radius (which might or might not be the same as the default)
                        if (float.TryParse(bombRadius, out float bombRad) && bombRad >= 0)
                        {
                            // bombradius is valid and not negative
                            map.BombDamage = bombRad;
                        }
                    }
                }

                // Check for amount of bomb sites, if available
                if (className == "func_bomb_target")
                {
                    // Map contains at least one bomb target, meaning it's a defusal map, so count them
                    map.AmountBombTargets++;
                }

                // Check for spawns
                if (className == "info_player_terrorist" || className == "info_player_counterterrorist")
                {
                    // Entity is spawn point
                    var spawn = new PlayerSpawn();
                    spawn.Team = className == "info_player_terrorist" ? ePlayerTeam.Terrorist : ePlayerTeam.CounterTerrorist;
                    spawn.Origin = this.stringToVector3(entityRootVdf["origin"]?.Value) ?? Vector3.Zero;
                    spawn.Angles = this.stringToVector3(entityRootVdf["angles"]?.Value) ?? Vector3.Zero;

                    // highest priority by default. if ALL spawns are high priority, then there are no priority spawns,
                    // in that case we will later check the amount of priority spawns and if it is the same as the total spawns.
                    // This is done for each team separately.
                    int priority = 0;
                    int.TryParse(entityRootVdf["priority"]?.Value, out priority);

                    if (priority < 1) // 0 or nothing means it's highest priority, then counts up as an integer with decreasing priority
                    {
                        spawn.IsPriority = true;
                        // Count into prio spawns
                        if (spawn.Team == ePlayerTeam.CounterTerrorist)
                            map.AmountPrioritySpawnsCT++;
                        else
                            map.AmountPrioritySpawnsT++;
                    }

                    // Count all (prio and normal) spawns
                    if (spawn.Team == ePlayerTeam.CounterTerrorist)
                        map.AmountSpawnsCT++;
                    else
                        map.AmountSpawnsT++;

                    if (entityRootVdf["targetname"]?.Value == "spawnpoints.2v2")
                    {
                        spawn.Type = eSpawnType.Wingman;
                    }

                    map.SpawnPoints.Add(spawn);
                }

                if (className == "info_hostage_spawn" || className == "hostage_entity")
                {
                    // Entity is hostage spawn point (equivalent but "info_hostage_spawn" is CS:GO-specific, "hostage_entity" is the equivalent and also exists in CS:S)
                    var spawn = new PlayerSpawn(); // Technically not a player :D
                    spawn.Origin = this.stringToVector3(entityRootVdf["origin"]?.Value) ?? Vector3.Zero;
                    spawn.Angles = this.stringToVector3(entityRootVdf["angles"]?.Value) ?? Vector3.Zero;
                    spawn.Team = ePlayerTeam.CounterTerrorist; // Just for the colour

                    // Count all hostage spawns
                    map.AmountHostages++;

                    spawn.Type = eSpawnType.Hostage;

                    map.SpawnPoints.Add(spawn);
                }
            }

            if (map.NavFilePath == null || map.AinFilePath == null)
            {
                // If either no NAV or no AIN file has been found, try to update them via the BSP pakfile
                var navFilesFound = SteamShared.Globals.Settings.CsgoHelper.ReadIfPackedNavFilesInBsp(map.BspFilePath);
                if (navFilesFound.Item1)
                {
                    map.NavFileBspPacked = true;
                    map.NavMesh = navFilesFound.Item3;
                    map.NavFilePath = "PACKED";
                }
                if (navFilesFound.Item2)
                {
                    map.AinFileBspPacked = true;
                    map.AinFilePath = "PACKED";
                }
            }

            if(map.NavFilePath != null && !map.NavFileBspPacked)
            {
                // Nav file not packed and a file path for it is existent so parse it here
                map.NavMesh = SteamShared.NavFile.Parse(new FileStream(map.NavFilePath, FileMode.Open));
            }
        }

        private Vector3 stringToVector3(string coords)
        {
            Vector3 vector = new Vector3();
            string[] coordsArray = coords.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (coordsArray.Length != 3)
                return null;

            float x, y, z;

            bool succX = float.TryParse(coordsArray[0], NumberStyles.Any, CultureInfo.InvariantCulture, out x);
            bool succY = float.TryParse(coordsArray[1], NumberStyles.Any, CultureInfo.InvariantCulture, out y);
            bool succZ = float.TryParse(coordsArray[2], NumberStyles.Any, CultureInfo.InvariantCulture, out z);

            if(succX && succY && succZ)
            {
                vector.X = x;
                vector.Y = y;
                vector.Z = z;
                return vector;
            }

            return null;
        }

        #region Conversion Helpers
        private double getUnitsFromPixels(double pixels)
        {
            double mapSizePixels = (this.mapImage.Source as BitmapSource).PixelWidth;
            double mapSizeUnits = mapSizePixels * (this.loadedMap.MapOverwrite.MapScale > 0 ? this.loadedMap.MapOverwrite.MapScale : this.loadedMap.MapSizeMultiplier);
            return Math.Abs(pixels) * mapSizeUnits / mapSizePixels;
        }

        private double getPixelsFromUnits(double units)
        {
            int mapSizePixels = (this.mapImage.Source as BitmapSource).PixelWidth;
            double mapSizeUnits = mapSizePixels * (this.loadedMap.MapOverwrite.MapScale > 0 ? this.loadedMap.MapOverwrite.MapScale : this.loadedMap.MapSizeMultiplier);
            return Math.Abs(units) * this.mapCanvas.ActualWidth / mapSizeUnits;
        }

        private Point getPointFromGameCoords(double x, double y)
        {
            Point p = new Point();
            p.X = this.getPixelsFromUnits(this.loadedMap.UpperLeftWorldXCoordinate - x - this.loadedMap.MapOverwrite.CoordOffset.X);
            p.Y = this.getPixelsFromUnits(this.loadedMap.UpperLeftWorldYCoordinate - y + this.loadedMap.MapOverwrite.CoordOffset.Y);
            return p;
        }

        private Point getGameCoordsFromPoint(double x, double y)
        {
            Point p = new Point();
            p.X = this.loadedMap.UpperLeftWorldXCoordinate + this.getUnitsFromPixels(x) - this.loadedMap.MapOverwrite.CoordOffset.X;
            p.Y = this.loadedMap.UpperLeftWorldYCoordinate - this.getUnitsFromPixels(y) + this.loadedMap.MapOverwrite.CoordOffset.Y;
            return p;
        }
        #endregion

        #region Get UI elements
        private Ellipse getPointEllipse(Color strokeColour)
        {
            Ellipse circle = new Ellipse();
            circle.Fill = null;
            circle.Width = circle.Height = this.getPixelsFromUnits(150);
            circle.Stroke = new SolidColorBrush(strokeColour);
            circle.StrokeThickness = 1;
            circle.IsHitTestVisible = false;

            return circle;
        }

        private Ellipse getBombEllipse(Color strokeColour)
        {
            Ellipse circle = new Ellipse();

            Color fillColour = strokeColour;
            fillColour.A = 50;

            circle.Fill = new SolidColorBrush(fillColour);
            circle.Width = circle.Height = this.getPixelsFromUnits(loadedMap.BombDamage * 3.5 * 2); // * 2 cause radius to width
            circle.Stroke = new SolidColorBrush(strokeColour);
            circle.StrokeThickness = 1;
            circle.IsHitTestVisible = false;

            return circle;
        }

        private ctrlPlayerSpawn getSpawnBlip(PlayerSpawn spawn)
        {
            ctrlPlayerSpawn spawnBlip = new ctrlPlayerSpawn();
            spawnBlip.Width = spawnBlip.Height = this.getPixelsFromUnits(50);
            Color blipColour;

            if (spawn.Team == ePlayerTeam.Terrorist)
                blipColour = Color.FromArgb(255, 252, 151, 0);
            else
                blipColour = Color.FromArgb(255, 0, 57, 245);

            spawnBlip.SetColour(blipColour);
            Color colourPriority = Color.FromArgb(150, 200, 200, 0);
            Color colourNoPriority = Color.FromArgb(150, 20, 20, 0);
            Color colourHostage = Color.FromArgb(150, 255, 0, 0);

            if(spawn.Type == eSpawnType.Hostage)
            {
                // This is a hostage, so fill it red
                spawnBlip.SetEllipseFill(colourHostage);
            }
            // If all are priority spawns, just mark all of them as low prio, for consistency
            else if (spawn.Team == ePlayerTeam.Terrorist && this.loadedMap.HasPrioritySpawnsT || spawn.Team == ePlayerTeam.CounterTerrorist && this.loadedMap.HasPrioritySpawnsCT)
            {
                // This team has at least 1 priority spawn, so only colour the priority ones bright
                spawnBlip.SetEllipseFill(spawn.IsPriority ? colourPriority : colourNoPriority);
            }
            else
            {
                // This team doesn't have any priority spawns, so colour it all dark
                spawnBlip.SetEllipseFill(colourNoPriority);
            }

            spawnBlip.SetRotation(360 - spawn.Angles.Y + 90);

            return spawnBlip;
        }
        #endregion

        private void positionNavMeshes()
        {
            var navMeshesInCanvas = new List<UIElement>();
            foreach (UIElement child in this.mapCanvas.Children)
            {
                if(child is FrameworkElement element && element.Tag is NavArea)
                {
                    // Child is nav mesh path so mark it for removal if the canvas hasn't settled yet
                    navMeshesInCanvas.Add(child);
                }
            }

            if (Globals.Settings.NavDisplayMode != SteamShared.NavDisplayModes.None && navMeshesInCanvas.Count > 0 && this.mapCanvas.ActualWidth > 0 && this.mapCanvas.ActualHeight > 0)
                // Canvas has already settled - no need to redraw
                return;

            // Remove all NAV meshes from canvas
            navMeshesInCanvas.ForEach(navMesh => canvasRemove(navMesh));

            if (Globals.Settings.NavDisplayMode == SteamShared.NavDisplayModes.None)
                // Don't draw and NAV areas if disabled
                return;

            if (this.loadedMap?.NavMesh?.Header?.NavAreas == null)
                return;

            this.loadedMap.NavMesh.Header.NavAreas = this.loadedMap.NavMesh.Header.NavAreas.OrderBy(area => area.MedianPosition.Z).ToArray();

            foreach (var area in this.loadedMap.NavMesh.Header.NavAreas)
            {
                // Don't draw area if it's not in the threshold that's set in the settings
                // First get the percentage of the average area height between the min and max area
                double heightPercentage = SteamShared.Globals.Map(area.MedianPosition.Z, this.loadedMap.NavMesh.MinZ ?? 0, this.loadedMap.NavMesh.MaxZ ?? 0, 0, 1);
                if (heightPercentage < Globals.Settings.ShowNavAreasAbove || heightPercentage > Globals.Settings.ShowNavAreasBelow)
                    continue;

                var path = new System.Windows.Shapes.Path();
                path.Tag = area;

                this.setNavAreaColour(path, area, isHovered: false);

                path.HorizontalAlignment = HorizontalAlignment.Left;
                path.VerticalAlignment = VerticalAlignment.Top;
                
                path.IsHitTestVisible = false; // Make it not catch mouse down events

                var pathGeometry = new PathGeometry(new List<PathFigure>
                {
                    new PathFigure(this.getPointFromGameCoords(area.ActualNorthWestCorner.X, area.ActualNorthWestCorner.Y), new List<PathSegment>
                    {
                        new LineSegment(this.getPointFromGameCoords(area.ActualNorthEastCorner.X, area.ActualNorthEastCorner.Y), isStroked: true),
                        new LineSegment(this.getPointFromGameCoords(area.ActualSouthEastCorner.X, area.ActualSouthEastCorner.Y), isStroked: true),
                        new LineSegment(this.getPointFromGameCoords(area.ActualSouthWestCorner.X, area.ActualSouthWestCorner.Y), isStroked: true),
                    }, closed: true)
                });

                path.Data = pathGeometry;
                this.canvasAdd(path);
            }

            /*NavArea? a = this.loadedMap.NavMesh.Header.NavAreas.FirstOrDefault(area => area.ID == 9);
            if(a != null)
            {
                System.Diagnostics.Debug.Print(this.loadedMap.MapFileName);
                System.Diagnostics.Debug.Print($"NorthWest: {a.ActualNorthWestCorner.X}, {a.ActualNorthWestCorner.Y}, {a.ActualNorthWestCorner.Z}");
                System.Diagnostics.Debug.Print($"NorthEast: {a.ActualNorthEastCorner.X}, {a.ActualNorthEastCorner.Y}, {a.ActualNorthEastCorner.Z}");
                System.Diagnostics.Debug.Print($"SouthWest: {a.ActualSouthWestCorner.X}, {a.ActualSouthWestCorner.Y}, {a.ActualSouthWestCorner.Z}");
                System.Diagnostics.Debug.Print($"SouthEast: {a.ActualSouthEastCorner.X}, {a.ActualSouthEastCorner.Y}, {a.ActualSouthEastCorner.Z}");
            }*/
        }

        private void addMapPointIfMissing(MapPoint point)
        {
            // Check if it's in the canvas
            if (!this.canvasContains(point.Circle))
                // Add it cause it was missing
                this.canvasAdd(point.Circle);

            // Set point scale and position
            Canvas.SetLeft(point.Circle, (point.PercentageX * mapCanvas.ActualWidth / 100f) - (point.Circle.Width / 2));
            Canvas.SetTop(point.Circle, (point.PercentageY * mapCanvas.ActualHeight / 100f) - (point.Circle.Height / 2));
            point.Circle.Width = point.Circle.Height = point.PercentageScale * this.mapCanvas.ActualWidth / 100f;
        }

        private void drawPointsAndConnectingLine()
        {
            // Make sure line is not null
            if (this.connectingLine == null)
                this.connectingLine = new System.Windows.Shapes.Path();

            // Handle the positioning of the circles if they were set (by right or left clicking)
            if (this.targetPoint?.Circle != null)
            {
                // Add to canvas if not in there
                this.addMapPointIfMissing(this.targetPoint);
            }

            if (this.playerPoint?.Circle != null)
            {
                // Add to canvas if not in there
                this.addMapPointIfMissing(this.playerPoint);
            }

            if (this.bombPoint?.Circle != null)
            {
                // Add to canvas if not in there
                this.addMapPointIfMissing(this.bombPoint);
            }

            if ((this.targetPoint?.Circle != null || this.bombPoint?.Circle != null) && this.playerPoint?.Circle != null)
            {
                // Right click cirle exists, and left click target (or bomb) circle exists
                // In other words: Two points were made, which means we can draw a line in between them
                // Note: Only one left click circle should exist, depending on which draw mode we're in

                // The points are on the canvas, because this was handled just before this

                if (this.redrawLine)
                {
                    Point leftClickPos;
                    Point rightClickPos;
                    // Ready to calculate damage and draw the in-between line
                    if (this.DrawMode == eDrawMode.Shooting)
                    {
                        // Update line start pos to left click target circle
                        leftClickPos.X = Canvas.GetLeft(this.targetPoint.Circle) + (this.targetPoint.Circle.Width / 2);
                        leftClickPos.Y = Canvas.GetTop(this.targetPoint.Circle) + (this.targetPoint.Circle.Height / 2);
                    }
                    else // We are in bomb mode. Change this if more modes are added
                    {
                        // Update line start pos to left click bomb circle
                        leftClickPos.X = Canvas.GetLeft(this.bombPoint.Circle) + (this.bombPoint.Circle.Width / 2);
                        leftClickPos.Y = Canvas.GetTop(this.bombPoint.Circle) + (this.bombPoint.Circle.Height / 2);
                    }
                    // Update line end pos to right click player circle
                    rightClickPos.X = Canvas.GetLeft(this.playerPoint.Circle) + (this.playerPoint.Circle.Width / 2);
                    rightClickPos.Y = Canvas.GetTop(this.playerPoint.Circle) + (this.playerPoint.Circle.Height / 2);

                    // Set visuals of the connecting line
                    this.connectingLine.Fill = null;
                    this.connectingLine.Stroke = new SolidColorBrush(this.connectingLineColour); // White, slightly transparent
                    this.connectingLine.StrokeThickness = 2;

                    // Make it not clickable
                    this.connectingLine.IsHitTestVisible = false;

                    this.connectingLine.HorizontalAlignment = HorizontalAlignment.Left;
                    this.connectingLine.VerticalAlignment = VerticalAlignment.Top;

                    var pathGeometry = new PathGeometry(new List<PathFigure>
                                        {
                                            new PathFigure(leftClickPos, new List<PathSegment>
                                            {
                                                new LineSegment(rightClickPos, isStroked: true)
                                            }, closed: false)
                                        });

                    this.connectingLine.Data = pathGeometry;
                    this.connectingLine.Tag = new Point[] { leftClickPos, rightClickPos };

                    if (!this.canvasContains(this.connectingLine))
                    {
                        this.canvasAdd(this.connectingLine);
                        this.lineDrawn = true;
                    }
                    this.redrawLine = false;
                    // Update top right corner distance texts
                    this.unitsDistanceMin = this.calculateDistanceInUnits(isMax: false);

                    if (this.DrawMode == eDrawMode.Bomb)
                    {
                        this.unitsDistanceMax = this.calculateDistanceInUnits(isMax: true);

                        if (this.unitsDistanceMin > this.unitsDistanceMax)
                        {
                            // The min and max will only be min and max if the player is above the bomb.
                            // If he's below the bomb, it will be swapped
                            (this.unitsDistanceMin, this.unitsDistanceMax) = (this.unitsDistanceMax, this.unitsDistanceMin);
                        }
                    }

                    if (this.unitsDistanceMax >= 0)
                    {
                        // We have a min and max value so show it like that as well: "0.000 - 0.000"
                        this.textDistanceUnits.Text = $"{Math.Round(this.unitsDistanceMin, 3).ToString(CultureInfo.InvariantCulture)} - {Math.Round(this.unitsDistanceMax, 3).ToString(CultureInfo.InvariantCulture)}";
                        this.textDistanceMetres.Text = $"{Math.Round(this.unitsDistanceMin / 39.37, 3).ToString(CultureInfo.InvariantCulture)} - {Math.Round(this.unitsDistanceMax / 39.37, 3).ToString(CultureInfo.InvariantCulture)}";
                    }
                    else
                    {
                        // We only have a "Min" distance, which is used for firearms. "0.000"
                        this.textDistanceUnits.Text = Math.Round(this.unitsDistanceMin, 3).ToString(CultureInfo.InvariantCulture);
                        this.textDistanceMetres.Text = Math.Round(this.unitsDistanceMin / 39.37, 3).ToString(CultureInfo.InvariantCulture);
                    }

                    // Recalculate and show damage
                    this.settings_Updated(null, null);
                }
            }
            else
            {
                // No 2 circles are being drawn that need any connection
                this.lineDrawn = false;
            }

            if (this.loadedMap != null)
            {
                this.positionNavMeshes();
                this.positionIcons();
                this.positionSpawns();
            }
        }

        /*private void getPlacePositions()
        {
            var places = new List<(string, Vector3)>();

            if (this.loadedMap?.BspFilePath == null)
                return;

            foreach(var area in this.loadedMap.NavMesh.Header.NavAreas)
            {
                // Add the name and avg position to the list
                if (area.PlaceID > 0 && area.PlaceID < this.loadedMap.NavMesh.Header.PlacesNames.Length)
                    places.Add((this.loadedMap.NavMesh.Header.PlacesNames[area.PlaceID - 1], new Vector3 
                    { 
                        X = (area.NorthWestCorner.X + area.SouthEastCorner.X) / 2, 
                        Y = (area.NorthWestCorner.Y + area.SouthEastCorner.Y) / 2,
                        Z = (area.NorthWestCorner.Z + area.SouthEastCorner.Z) / 2,
                    }));
            }

            // Average all X and Y positions of every place again and put it in a new list
            var placesCorrected = new Dictionary<string, Vector3>();

            foreach(var place in places)
            {
                if (!placesCorrected.ContainsKey(place.Item1))
                {
                    var correspondingPlaces = places.Where(pl => pl.Item1 == place.Item1);
                    float X = correspondingPlaces.Sum(place => place.Item2.X);
                    float Y = correspondingPlaces.Sum(place => place.Item2.Y);
                    float Z = correspondingPlaces.Sum(place => place.Item2.Z);
                    float newX = X / correspondingPlaces.Count();
                    float newY = Y / correspondingPlaces.Count();
                    float newZ = Z / correspondingPlaces.Count();
                    placesCorrected.Add(place.Item1, new Vector3 { X = newX, Y = newY, Z = newZ });
                }
            }
        }*/

        private void positionSpawns()
        {
            double size = this.getPixelsFromUnits(75);
            foreach (var spawn in this.loadedMap.SpawnPoints)
            {
                if (spawn.Type != eSpawnType.Hostage)
                {
                    if (!Globals.Settings.AllowNonPrioritySpawns && (!spawn.IsPriority || (spawn.Team == ePlayerTeam.Terrorist && !this.loadedMap.HasPrioritySpawnsT) || (spawn.Team == ePlayerTeam.CounterTerrorist && !this.loadedMap.HasPrioritySpawnsCT)))
                        continue;
                }

                if (spawn.Type == eSpawnType.Standard && !Globals.Settings.ShowStandardSpawns)
                    continue;
                else if (spawn.Type == eSpawnType.Wingman && !Globals.Settings.Show2v2Spawns)
                    continue;
                else if (spawn.Type == eSpawnType.Hostage && !Globals.Settings.ShowHostageSpawns)
                    continue;

                if (!canvasContains(child => child is Viewbox vb && vb.Tag == spawn))
                {
                    Viewbox box = new Viewbox();

                    // Are viewboxes even needed?
                    var blipControl = this.getSpawnBlip(spawn);
                    box.Tag = spawn;
                    box.Child = blipControl;
                    box.Width = box.Height = size;

                    Point newCoords = this.getPointFromGameCoords(spawn.Origin.X, spawn.Origin.Y);

                    this.canvasAdd(box);
                    Canvas.SetLeft(box, newCoords.X - box.Width / 2);
                    Canvas.SetTop(box, newCoords.Y - box.Height / 2);
                }
            }
        }

        private void drawIconsIfFit(List<(string, Point)> itemsToAdd)
        {
            foreach (var icon in itemsToAdd)
            {
                // Is icon already in canvas?
                if (canvasContains(child => child is FrameworkElement element && element.Tag.ToString() == icon.Item1))
                {
                    // Yip, don't draw it again and check the next
                    continue;
                }

                // Nope, create icon and add it

                var iconImage = new Image();
                iconImage.Source = new BitmapImage(new Uri(icon.Item1, UriKind.RelativeOrAbsolute));
                iconImage.Width = 25;
                iconImage.Height = 25;
                iconImage.Opacity = 0.6;
                iconImage.IsHitTestVisible = false;
                iconImage.Tag = icon.Item1;

                // Get absolute icon coordinates from relative ones (0.0 to 1.0)
                double left = icon.Item2.X * this.mapImage.ActualWidth - (iconImage.ActualWidth / 2);
                double top = icon.Item2.Y * this.mapImage.ActualWidth - (iconImage.ActualHeight / 2);

                // Should icon be drawn outside the canvas?
                if (left < 0 && left > this.mapImage.ActualWidth || top < 0 && top > this.mapImage.ActualHeight)
                {
                    // Yep, don't add it to the canvas and let the GC murder it
                }

                // It's in the image bounds so set position and add it
                Canvas.SetLeft(iconImage, left);
                Canvas.SetTop(iconImage, top);

                canvasAdd(iconImage);
            }
        }

        private void positionIcons()
        {
            if (Globals.Settings.ShowSpawnAreas)
            {
                var iconsToDraw = new List<(string, Point)>
                {
                    // The multipliers are values from 0.0 to 1.0 depending on how far right or down they are relatively to the map
                    ("icon_ct.png", new Point(this.loadedMap.CTSpawnMultiplierX, this.loadedMap.CTSpawnMultiplierY)),
                    ("icon_t.png", new Point(this.loadedMap.TSpawnMultiplierX, this.loadedMap.TSpawnMultiplierY))
                };

                this.drawIconsIfFit(iconsToDraw);
            }

            if (Globals.Settings.ShowBombSites)
            {
                var iconsToDraw = new List<(string, Point)>
                {
                    // The multipliers are values from 0.0 to 1.0 depending on how far right or down they are relatively to the map
                    ("icon_a_site.png", new Point(this.loadedMap.BombAX, this.loadedMap.BombAY)),
                    ("icon_b_site.png", new Point(this.loadedMap.BombBX, this.loadedMap.BombBY))
                };

                this.drawIconsIfFit(iconsToDraw);
            }
        }

        /// <summary>
        /// Calculates the distance from the start to the end of the <see cref="connectingLine"/>.
        /// Bomb distance adds a random factor, which is described as min and max.
        /// </summary>
        /// <param name="isMax">
        /// If we calculate bomb damage, should we get the max possible distance?
        /// Otherwise it will return the min possible distance.
        /// </param>
        /// <returns></returns>
        private double calculateDistanceInUnits(bool isMax = false)
        {
            // left and right point for the X and Y coordinates (in pixels) so we gotta convert those
            Point[] points = this.connectingLine.Tag as Point[];

            // These are not in-game coordinates yet, but the offset from the top left -- but in units instead of pixels
            double leftX = this.getUnitsFromPixels(points[0].X);
            double leftY = this.getUnitsFromPixels(points[0].Y);
            double leftZ;

            double rightX = this.getUnitsFromPixels(points[1].X);
            double rightY = this.getUnitsFromPixels(points[1].Y);
            double rightZ;

            // Make the left and right point in-game coordinates accessible to everyone outside this function
            Point playerCoords = this.getGameCoordsFromPoint(points[1].X, points[1].Y);
            this.playerPoint.X = playerCoords.X;
            this.playerPoint.Y = playerCoords.Y;

            Point targetOrBombCoords = this.getGameCoordsFromPoint(points[0].X, points[0].Y);
            if (this.bombPoint != null)
            {
                this.bombPoint.X = targetOrBombCoords.X;
                this.bombPoint.Y = targetOrBombCoords.Y;
            }
            if(this.targetPoint != null)
            {
                this.targetPoint.X = targetOrBombCoords.X;
                this.targetPoint.Y = targetOrBombCoords.Y;
            }

            // Manage height
            if (this.playerPoint.AssociatedAreaID < 0 ||
                ((this.DrawMode == eDrawMode.Shooting && this.targetPoint.AssociatedAreaID < 0)
                || (this.DrawMode == eDrawMode.Bomb && this.bombPoint.AssociatedAreaID < 0))) 
            {
                // One of the points has no area ID, and thus no Z coordinate
                leftZ = 0;
                rightZ = 0;
            }
            else
            {
                leftZ = this.DrawMode == eDrawMode.Shooting ? this.targetPoint.Z : this.bombPoint.Z;
                rightZ = this.playerPoint.Z;
            }

            // Distance in units in 2D
            double diffUnits2D = Math.Sqrt(Math.Pow(leftX - rightX, 2) + Math.Pow(leftY - rightY, 2));

            if (this.DrawMode == eDrawMode.Bomb)
            {
                float minFactor = 0.7f;
                float maxFactor = 1.0f;

                // Add the appropriate height
                // They use the middle of the oriented bounding box,
                // which should be equal to the axis-aligned bounding box, but with additional yaw in the normal sense
                // Since we already have the X-Y middle, we just add half of the height to get the Z-middle as well
                if (radioPlayerStanding.IsChecked == true)
                    rightZ += isMax ? eyeLevelStanding * maxFactor : eyeLevelStanding * minFactor;
                else if(radioPlayerCrouched.IsChecked == true)
                    rightZ += isMax ? eyeLevelCrouching * maxFactor : eyeLevelCrouching * minFactor;
            }

            // Add Z height to calculation, unless a point has no area ID associated, then it stays 2D
            double diffUnits3D = Math.Sqrt(Math.Pow(diffUnits2D, 2) + Math.Pow(leftZ - rightZ, 2));
  
            return diffUnits3D;
        }

        private void calculateDistanceDuration()
        {
            double timeRunning = this.unitsDistanceMin / this.selectedWeapon.RunningSpeed;
            double timeWalking = this.unitsDistanceMin / (this.selectedWeapon.RunningSpeed * SteamShared.CsgoHelper.WalkModifier);
            double timeCrouching = this.unitsDistanceMin / (this.selectedWeapon.RunningSpeed * SteamShared.CsgoHelper.DuckModifier);

            this.txtTimeRunning.Text = getTimeStringFromSeconds(timeRunning);
            this.txtTimeWalking.Text = getTimeStringFromSeconds(timeWalking);
            this.txtTimeCrouching.Text = getTimeStringFromSeconds(timeCrouching);
        }

        private string getTimeStringFromSeconds(double seconds)
        {
            string timeString = string.Empty;

            int separatedMinutes = (int)(seconds / 60);
            double separatedSeconds = seconds % 60;

            if (separatedMinutes > 0)
                timeString += $"{separatedMinutes} min, ";

            timeString += $"{Math.Round(separatedSeconds, 2).ToString(CultureInfo.InvariantCulture)} sec";

            return timeString;
        }

        private void calculateAndUpdateShootingDamage()
        {
            double damage = this.selectedWeapon.BaseDamage;
            double absorbedDamageByArmor = 0;
            bool wasArmorHit = false;

            if (this.unitsDistanceMin > this.selectedWeapon.MaxBulletRange)
            {
                damage = 0;
                txtResult.Text = txtResultArmor.Text = damage.ToString();
                return;
            }

            // Range
            damage *= Math.Pow(this.selectedWeapon.DamageDropoff, double.Parse((this.unitsDistanceMin / 500f).ToString()));

            switch (this.selectedWeapon.DamageType)
            {
                case DamageType.Shock:
                    // Deals the same damage everywhere
                    break;
                case DamageType.Bullet:
                    // Multipliers and armor penetration
                    if (radioHead.IsChecked == true)
                    {
                        // Headshot
                        damage *= this.selectedWeapon.HeadshotModifier;

                        if (chkHelmet.IsChecked == true)
                        {
                            // Has helmet
                            double previousDamage = damage;
                            damage *= this.selectedWeapon.ArmorPenetration / 100f;
                            absorbedDamageByArmor = previousDamage - damage;
                            wasArmorHit = true;
                        }
                    }
                    else if (radioChestArms.IsChecked == true)
                    {
                        // Chest or arms
                        if (chkKevlar.IsChecked == true)
                        {
                            // Has kevlar
                            double previousDamage = damage;
                            damage *= this.selectedWeapon.ArmorPenetration / 100f;
                            absorbedDamageByArmor = previousDamage - damage;
                            wasArmorHit = true;
                        }
                    }
                    else if (radioStomach.IsChecked == true)
                    {
                        // Stomach
                        damage *= 1.25f;

                        if (chkKevlar.IsChecked == true)
                        {
                            // Has kevlar
                            double previousDamage = damage;
                            damage *= this.selectedWeapon.ArmorPenetration / 100f;
                            absorbedDamageByArmor = previousDamage - damage;
                            wasArmorHit = true;
                        }
                    }
                    else
                    {
                        // Legs
                        damage *= 0.75f;
                    }
                    break;
            }

            txtResult.Text = ((int)damage).ToString();

            txtResultArmor.Text = (wasArmorHit ? Math.Ceiling(absorbedDamageByArmor / 2f) : 0).ToString();

            // TODO: HP and armor and HP and armor left after shot
        }

        private void calculateAndUpdateBombDamage()
        {
            // Now we can calculate the damage...
            double minDamage; 
            double minDamageArmor; 
            this.getBombDamage(this.unitsDistanceMax, armorValue: 100, out minDamage, out minDamageArmor);
            double maxDamage;
            double maxDamageArmor;
            this.getBombDamage(this.unitsDistanceMin, armorValue: 100, out maxDamage, out maxDamageArmor);
            
            txtResultBombMin.Text = ((int)minDamage).ToString();
            txtResultBombMax.Text = ((int)maxDamage).ToString();
            txtResultBombMedian.Text = ((int)((maxDamage + minDamage) / 2f)).ToString();

            txtResultArmorBombMin.Text = minDamageArmor.ToString();
            txtResultArmorBombMax.Text = maxDamageArmor.ToString();
            txtResultArmorBombMedian.Text = ((maxDamageArmor + minDamageArmor) / 2f).ToString();
        }

        private void getBombDamage(double distance, int armorValue, out double hpDamage, out double armorDamage)
        {
            // out params
            hpDamage = 0;
            armorDamage = 0;

            // This is the maximum damage, and the height of the bell curve
            double flDamage = this.loadedMap.BombDamage; // 500 is hard-coded as the default, and also the default in the hammer editor, can be overridden as a map creator
            double flBombRadius = flDamage * 3.5d;

            // First we need to check if the player is within the bomb radius, this isn't done with a sphere, but with a box that has a side length of 2r
            // So basically it's the bounding box, so if you're not directly above, below, left or right of the bomb (as seen from above), the radius increases a bit.
            // Also its calculated via the intersection with the bounding box of the player which is 32x32 units in the horizontal axes

            // Get mins and maxs of player hitbox

            // Mins is X - 16, Y - 16, Z
            Vector3 playerMins = new Vector3 { X = (float)(this.playerPoint.X - (playerWidth / 2d)), Y = (float)(this.playerPoint.Y - (playerWidth / 2d)), Z = (float)this.playerPoint.Z };

            // Head height is the eye level, crouching is smaller, otherwise use standing height
            float headHeight = (float)(this.radioPlayerCrouched.IsChecked == true ? heightCrouching : heightStanding);

            // Maxs is X + 16, Y + 16, Z + head height
            Vector3 playerMaxs = new Vector3 { X = (float)(this.playerPoint.X + (playerWidth / 2d)), Y = (float)(this.playerPoint.Y + (playerWidth / 2d)), Z = (float)this.playerPoint.Z + headHeight };

            // Get mins and maxs of bomb radius
            Vector3 bombMins = new Vector3 { X = (float)(this.bombPoint.X - flBombRadius), Y = (float)(this.bombPoint.Y - flBombRadius), Z = (float)(this.bombPoint.Z - flBombRadius) };

            Vector3 bombMaxs = new Vector3 { X = (float)(this.bombPoint.X + flBombRadius), Y = (float)(this.bombPoint.Y + flBombRadius), Z = (float)(this.bombPoint.Z + flBombRadius) };

            // Check if the two boxes intersect
            bool playerIsInRange = SpatialPartitioningHelper.BoxIntersects(bombMins, bombMaxs, playerMins, playerMaxs);

            if (!playerIsInRange)
            {
                hpDamage = 0;
                armorDamage = 0;
                return;
            }

            // From player origin + offset, to the bomb origin
            double flDistanceToLocalPlayer = distance;
            // This defines the width of the curve, a smaller value gives a steeper curve and a faster falloff
            double fSigma = flBombRadius / 3.0d;
            double fGaussianFalloff = Math.Exp(-flDistanceToLocalPlayer * flDistanceToLocalPlayer / (2.0d * fSigma * fSigma));
            double flAdjustedDamage = flDamage * fGaussianFalloff;

            double flAdjustedDamageBeforeArmor = flAdjustedDamage;

            if (chkArmorAny.IsChecked == true)
            {
                flAdjustedDamage = scaleDamageArmor(flAdjustedDamage, armorValue);
                armorDamage = flAdjustedDamage >= 1 ? Math.Ceiling((flAdjustedDamageBeforeArmor - flAdjustedDamage) / 2f) : 0;
            }

            hpDamage = flAdjustedDamage;
        }

        /// <summary>
        /// Calculates the amount of damage that the bomb deals with a specific amount of armor.
        /// </summary>
        /// <param name="flDamage">The damage that was dealt to the player by the bomb.</param>
        /// <param name="armor_value">The amount of armor that the player had.</param>
        /// <returns>the amount of damage that is actually dealt.</returns>
        double scaleDamageArmor(double flDamage, int armor_value)
        {
            double flArmorRatio = 0.5d;
            double flArmorBonus = 0.5d;
            if (armor_value > 0)
            {
                double flNew = flDamage * flArmorRatio;
                double flArmor = (flDamage - flNew) * flArmorBonus;

                if (flArmor > (double)armor_value)
                {
                    flArmor = (double)armor_value * (1d / flArmorBonus);
                    flNew = flDamage - flArmor;
                }

                flDamage = flNew;
            }
            return flDamage;
        }

        private bool isPointInMap(Point position)
        {
            return position.X >= 0 && position.Y >= 0 && position.X <= mapImage.ActualWidth && position.Y <= mapImage.ActualHeight;
        }

        private void changeTheme(REghZyFramework.Themes.ThemesController.ThemeTypes newTheme)
        {
            REghZyFramework.Themes.ThemesController.SetTheme(newTheme);

            // Additional stuff you want to change manually
            switch (newTheme)
            {
                case REghZyFramework.Themes.ThemesController.ThemeTypes.Dark:
                    rectTop.Fill = rectLeftSide.Fill = rectRightSide.Fill = new SolidColorBrush(Colors.White);
                    txtEasterEggMetres.Text = "Metres:";
                    break;
                case REghZyFramework.Themes.ThemesController.ThemeTypes.Light:
                    rectTop.Fill = rectLeftSide.Fill = rectRightSide.Fill = new SolidColorBrush(Colors.Black);
                    txtEasterEggMetres.Text = "Meters:";
                    break;
            }
        }

        private void canvasReload()
        {
            // Reload map list if map filters changed
            this.updateMapsWithCurrentFilter();

            // Reload visuals
            this.resetCanvas(true);
            this.changeTheme(Globals.Settings.Theme);
        }

        /// <summary>
        /// Takes the given <see cref="NavArea"/> and the height of its 4 points,
        /// and calculates what the height of the given point of (X,Y) must be, with linear interpolation and weights.
        /// </summary>
        /// <param name="x">The given X of point.</param>
        /// <param name="y">The given Y of point.</param>
        /// <param name="area">The area that serves as a height reference.</param>
        /// <returns>the Z coordinate of the given point in respect to the <see cref="NavArea"/>.</returns>
        private float getPointHeightInArea(float x, float y, NavArea area)
        {
            Vector3[][] groups = new Vector3[][] 
            { 
                // Order within nested array: Point that gets weighted => Point that supplies its X (origin for X weight, 0% basically) => Point that supplies its Y in the same manner 
                // So basically the second and third item is respectively the point left/right and above/under the first item (if north is up)
                new Vector3[] { area.ActualNorthWestCorner, area.ActualNorthEastCorner, area.ActualSouthWestCorner },
                new Vector3[] { area.ActualNorthEastCorner, area.ActualNorthWestCorner, area.ActualSouthEastCorner },
                new Vector3[] { area.ActualSouthWestCorner, area.ActualSouthEastCorner, area.ActualNorthWestCorner },
                new Vector3[] { area.ActualSouthEastCorner, area.ActualSouthWestCorner, area.ActualNorthEastCorner } 
            };

            float resultHeight = 0;

            foreach (Vector3[] group in groups)
            {
                float xWeight = SteamShared.Globals.Map(x, group[1].X, group[0].X, 0, 1);
                float yWeight = SteamShared.Globals.Map(y, group[2].Y, group[0].Y, 0, 1);
                float combinedWeight = xWeight * yWeight;

                resultHeight += combinedWeight * group[0].Z;
            }

            return resultHeight;
        }

        private void recalculateCoordinates()
        {
            if (this.mapImage.Source == null)
                return;

            var position = Mouse.GetPosition(mapImage);
            if (this.isPointInMap(position))
            {
                Point posInGame = this.getGameCoordsFromPoint(position.X, position.Y);

                this.currentMouseCoord.X = (float)posInGame.X;
                txtCursorX.Text = Math.Round(posInGame.X, 2).ToString(CultureInfo.InvariantCulture);

                this.currentMouseCoord.Y = (float)posInGame.Y;
                txtCursorY.Text = Math.Round(posInGame.Y, 2).ToString(CultureInfo.InvariantCulture);

                // Height to be displayed further down, depending on area chosen
                float newZ = 0;

                if (this.loadedMap?.NavMesh?.Header?.NavAreas != null)
                {
                    var navAreasFound = new List<NavArea>();

                    foreach (var area in this.loadedMap.NavMesh.Header.NavAreas)
                    {
                        if (posInGame.X < area.ActualNorthWestCorner.X)
                            continue;
                        if (posInGame.X > area.ActualNorthEastCorner.X)
                            continue;
                        if (posInGame.Y > area.ActualNorthWestCorner.Y)
                            continue;
                        if (posInGame.Y < area.ActualSouthWestCorner.Y)
                            continue;

                        // HERE we found an area that the mouse is in. Here we need to count areas in case they overlap
                        navAreasFound.Add(area);
                    }

                    uint previousAreaID = this.getCurrentArea()?.ID ?? 0;

                    // Select layer closest to previous one height-wise
                    // (positionNavAreas() orders the NAV areas from bottom to top when adding so last one will be top one (based on average area height)) 
                    if ((this.currentHeightLayer < 0 || this.currentHeightLayer >= navAreasFound.Count || this.hoveredNavAreas.Count != navAreasFound.Count) && navAreasFound.Count > 0 && !this.userChangedLayer)
                    {
                        // Current height layer selection is undefined or bigger than the amound of areas we have
                        // Or the amount of areas hovered over has changed.
                        // In that case set it to the layer with the lowest Z difference to the previously selected one

                        if (navAreasFound.Count == 1 || this.hoveredNavAreas == null || this.hoveredNavAreas?.Count == 0)
                            this.currentHeightLayer = 0;
                        else
                        {
                            int newHeightLayerIndex = 0;
                            float lowestAreaHeightDifference = -1;
                            for (int i = 0; i < navAreasFound.Count; i++)
                            {
                                float heightDiffToPrevArea = Math.Abs(navAreasFound[i].MedianPosition.Z - this.hoveredNavAreas[this.currentHeightLayer < 0 ? 0 : this.currentHeightLayer].MedianPosition.Z);
                                if (heightDiffToPrevArea < lowestAreaHeightDifference || lowestAreaHeightDifference < 0)
                                {
                                    // Difference of currently looped area and last hovered area is smaller than previous loop iterations
                                    newHeightLayerIndex = i;
                                    lowestAreaHeightDifference = heightDiffToPrevArea;
                                }
                            }

                            this.currentHeightLayer = newHeightLayerIndex;
                        }
                    }

                    if (navAreasFound.Count == 0)
                        this.currentHeightLayer = -1;

                    // Update areas
                    this.hoveredNavAreas = navAreasFound;
                    this.currentHeightLayer += this.userHeightLayerOffset;

                    uint newAreaID = this.getCurrentArea()?.ID ?? 0;

                    if (this.hoveredNavAreas.Count > 0)
                    {
                        // There are areas hovered over
                        newZ = this.getPointHeightInArea(currentMouseCoord.X, currentMouseCoord.Y, this.hoveredNavAreas[this.currentHeightLayer]);
                    }

                    this.fillNavAreaInfo();
                    if (this.userChangedLayer || previousAreaID != newAreaID || newAreaID < 1)
                    {
                        // Hovered area changed so handle content of info box and highlighting of areas

                        if (newAreaID > 0)
                        {
                            if (this.hoveredNavAreas.Count > 0 && this.currentHeightLayer > -1)
                            {
                                // There's a new area to highlight
                                updateNavAreaHovered(newAreaID, setHovered: true);
                            }
                        }
                        if (previousAreaID > 0)
                        {
                            // There's an old area to de-highlight
                            updateNavAreaHovered(previousAreaID, setHovered: false);
                        }
                    }

                    this.userChangedLayer = false;
                    this.userHeightLayerOffset = 0;
                }

                // Display height
                this.currentMouseCoord.Z = newZ;
                txtCursorZ.Text = Math.Round(newZ, 2).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                txtCursorX.Text = txtCursorY.Text = "0";
            }
        }

        private void updateNavAreaHovered(uint areaID, bool setHovered)
        {
            foreach(FrameworkElement element in this.mapCanvas.Children)
            {
                if(element.Tag is NavArea area && area.ID == areaID)
                {
                    setNavAreaColour(element as System.Windows.Shapes.Path, area, setHovered);
                    return;
                }
            }
        }

        private void setNavAreaColour(System.Windows.Shapes.Path pathOfArea, NavArea area, bool isHovered)
        {
            Color newColour;
            if (isHovered)
            {
                newColour = Globals.Settings.NavHoverColour;
            }
            else
            {
                // Map average area height between two configurable colours
                byte newA = (byte)SteamShared.Globals.Map(area.MedianPosition.Z, loadedMap.NavMesh.MinZ ?? 0, loadedMap.NavMesh.MaxZ ?? 0, Globals.Settings.NavLowColour.A, Globals.Settings.NavHighColour.A);
                byte newR = (byte)SteamShared.Globals.Map(area.MedianPosition.Z, loadedMap.NavMesh.MinZ ?? 0, loadedMap.NavMesh.MaxZ ?? 0, Globals.Settings.NavLowColour.R, Globals.Settings.NavHighColour.R);
                byte newG = (byte)SteamShared.Globals.Map(area.MedianPosition.Z, loadedMap.NavMesh.MinZ ?? 0, loadedMap.NavMesh.MaxZ ?? 0, Globals.Settings.NavLowColour.G, Globals.Settings.NavHighColour.G);
                byte newB = (byte)SteamShared.Globals.Map(area.MedianPosition.Z, loadedMap.NavMesh.MinZ ?? 0, loadedMap.NavMesh.MaxZ ?? 0, Globals.Settings.NavLowColour.B, Globals.Settings.NavHighColour.B);
                newColour = Color.FromArgb(newA, newR, newG, newB);
            }

            switch (Globals.Settings.NavDisplayMode)
            {
                case SteamShared.NavDisplayModes.Wireframe:
                    pathOfArea.Stroke = new SolidColorBrush(newColour);
                    pathOfArea.StrokeThickness = 1;
                    pathOfArea.Fill = null;
                    break;
                case SteamShared.NavDisplayModes.Filled:
                    pathOfArea.Stroke = null;
                    pathOfArea.StrokeThickness = 0;
                    pathOfArea.Fill = new SolidColorBrush(newColour);
                    break;
            }
        }

        private NavArea getCurrentArea()
        {
            if (this.hoveredNavAreas?.Count > 0 && this.currentHeightLayer > -1 && this.currentHeightLayer < this.hoveredNavAreas.Count)
                return this.hoveredNavAreas[this.currentHeightLayer];

            return null;
        }

        private void fillNavAreaInfo()
        {
            // Show current area not as index from 0 but starting from 1 (as in 1st layer, 2nd layer) etc.
            if (this.hoveredNavAreas.Count > 0)
            {
                this.txtNavAreasAmount.Text = $"{(this.currentHeightLayer < 0 ? 0 : this.currentHeightLayer + 1)}/{this.hoveredNavAreas.Count}";
                this.txtNavAreaHeightPercentage.Text = Math.Round(SteamShared.Globals.Map(this.hoveredNavAreas[this.currentHeightLayer].MedianPosition.Z, this.loadedMap.NavMesh.MinZ ?? 0, this.loadedMap.NavMesh.MaxZ ?? 0, 0, 100), 1).ToString(CultureInfo.InvariantCulture) + " %";
                this.txtNavAreaID.Text = this.hoveredNavAreas[this.currentHeightLayer].ID.ToString();
                this.txtNavAreaConnectionsAmount.Text = this.hoveredNavAreas[this.currentHeightLayer].ConnectionData.Sum(direction => direction.Count).ToString();
                this.txtNavAreaPlace.Text = this.hoveredNavAreas[this.currentHeightLayer].PlaceID == 0 ? "None" : this.loadedMap.NavMesh.Header.PlacesNames[this.hoveredNavAreas[this.currentHeightLayer].PlaceID - 1];
            }
            else
            {
                this.resetNavInfo();
            }
        }

        /// <summary>
        /// Gets the <see cref="NavArea"/> of the <see cref="loadedMap"/> that is closest to the height of the given point.
        /// The point has to be inside of the X and Y bounds of the NAV area for it to be considered.
        /// This makes it so the Z distance can be anything, as long as it's the closest of any area.
        /// </summary>
        /// <param name="point">The point we want to partner with a NAV area.</param>
        /// <returns>The <see cref="NavArea"/> belonging to the point.</returns>
        private NavArea? getClosestNavAreaToPoint(Vector3 point)
        {
            if (this.loadedMap?.NavMesh?.Header?.NavAreas == null)
            {
                return null;
            }

            NavArea closestNavArea = null;
            float closestDistance = float.MaxValue;

            foreach(NavArea area in this.loadedMap.NavMesh.Header.NavAreas)
            {
                if (point.X < area.ActualNorthWestCorner.X || point.X > area.ActualNorthEastCorner.X
                    || point.Y < area.ActualSouthWestCorner.Y || point.Y > area.ActualNorthWestCorner.Y)
                {
                    // Point is not in this NavArea's X and Y bounds
                    continue;
                }

                float currentZDistance = Math.Abs(area.MedianPosition.Z - point.Z);

                if (currentZDistance < closestDistance)
                {
                    closestDistance = currentZDistance;
                    closestNavArea = area;
                }
            }

            return closestNavArea;
        }

        private void fillWeaponInfo()
        {
            if(this.selectedWeapon != null)
            {
                this.groupWeaponName.Header = this.selectedWeapon.ClassName.Replace('_','-');
                this.txtWeaponBaseDamage.Text = this.selectedWeapon.BaseDamage.ToString(CultureInfo.InvariantCulture);
                this.txtWeaponBaseDamagePerMinute.Text = this.selectedWeapon.FireRate < 0 ? "?" : (this.selectedWeapon.BaseDamage * this.selectedWeapon.FireRate).ToString(CultureInfo.InvariantCulture);
                this.txtWeaponFireRate.Text = this.selectedWeapon.FireRate.ToString(CultureInfo.InvariantCulture);
                this.txtWeaponArmorPenetration.Text = this.selectedWeapon.ArmorPenetration.ToString(CultureInfo.InvariantCulture) + " %";
                this.txtWeaponDamageDropoff.Text = (Math.Round(1d - this.selectedWeapon.DamageDropoff, 2) * 100).ToString(CultureInfo.InvariantCulture) + " %";
                this.txtWeaponMaxRange.Text = this.selectedWeapon.MaxBulletRange.ToString(CultureInfo.InvariantCulture);
                this.txtWeaponHeadshotModifier.Text = this.selectedWeapon.DamageType != DamageType.Shock ? this.selectedWeapon.HeadshotModifier.ToString(CultureInfo.InvariantCulture) : placeholderText;
                this.txtWeaponRunningSpeed.Text = this.selectedWeapon.RunningSpeed.ToString();
            }
            else
            {
                this.resetWeaponInfo();
            }
        }

        private void resetWeaponInfo()
        {
            foreach (TextBlock x in this.stackWeaponInfo.Children)
                x.Text = placeholderText;
        }

        private void resetNavInfo()
        {
            foreach (TextBlock x in this.stackNavInfo.Children)
                x.Text = placeholderText;
        }

        private void setPlayerConnected(bool connected)
        {
            if (connected)
            {
                this.stackInGameConnected.Visibility = Visibility.Visible;
                this.stackInGameDisconnected.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.stackInGameConnected.Visibility = Visibility.Collapsed;
                this.stackInGameDisconnected.Visibility = Visibility.Visible;
            }
        }

        private bool getNetConPort(string input, out ushort port)
        {
            int index = input.IndexOf("-netconport", StringComparison.InvariantCultureIgnoreCase);
            port = 0;

            if (index < 0)
                return false;

            input = input.Substring(index);
            var foundArguments = Regex.Matches(input, SteamShared.Globals.ArgumentPattern, RegexOptions.IgnoreCase);

            if (foundArguments.Count > 1)
            {
                string setPort = foundArguments[1].Value.Replace("\"","");
                if (ushort.TryParse(setPort, out ushort telnetPort))
                {
                    port = telnetPort;
                }
                return true;
            }

            return false;
        }
        
        private bool getNetConPassword(string input, out string password)
        {
            int index = input.IndexOf("-netconpassword", StringComparison.InvariantCultureIgnoreCase);
            password = string.Empty;

            if (index < 0)
                return false;

            input = input.Substring(index);
            var foundArguments = Regex.Matches(input, SteamShared.Globals.ArgumentPattern, RegexOptions.IgnoreCase);

            if (foundArguments.Count > 1)
            {
                string setPassword = foundArguments[1].Value.Replace("\"", "");
                if (!setPassword.StartsWith("-"))
                {
                    password = setPassword;
                }
                return true;
            }

            return false;
        }

        private async void establishCsgoConnection()
        {
            if (SteamShared.Globals.Settings.CsgoSocket.IsConnecting)
            {
                ShowMessage.Error("We're currently trying to connect to CS:GO.\n\nThis should only be 10 seconds after starting to connect.\n\nIf CS:GO was not started yet, this time is 60 seconds.");
                return;
            }

            var mostRecentUser = SteamShared.Globals.Settings.SteamHelper.GetMostRecentSteamUser();

            if (mostRecentUser == null)
            {
                ShowMessage.Error("There was no user found that was marked as most recent.\n\nTry logging into Steam, if it's been a while.");
                return;
            }

            var startOptions = SteamShared.Globals.Settings.CsgoHelper.GetLaunchOptions(mostRecentUser);

            if (startOptions == null)
            {
                ShowMessage.Error($"There was an error while trying to get the CS:GO launch options for the user {mostRecentUser.PersonaName} ({mostRecentUser.AccountName}).");
                return;
            }

            // Because the start options are passed either way, we just want to add what we want additionally
            string additionalStartOptions = string.Empty;
            bool needsAdditionalStartOptions = true;

            // Set own password and port, if no others will be found
            string passwordToUse = string.Empty; // We don't want a password
            ushort portToUse = Globals.Settings.NetConPort;

            // Check if CS:GO is running
            (Process? csgo, string? curCsgoCmdLine) = SteamShared.Globals.Settings.CsgoHelper.GetRunningCsgo();

            // CS:GO wasn't open, so either take the info from the start options or create our own
            // We're gonna get those from the file that stores the current settings, because the game is not running
            ushort foundPort = 0;
            bool portWasFound = this.getNetConPort(csgo == null ? startOptions : curCsgoCmdLine, out foundPort);

            string foundPassword = string.Empty;
            bool passwordWasFound = this.getNetConPassword(csgo == null ? startOptions : curCsgoCmdLine, out foundPassword);

            // Verify found port and password are valid
            if (portWasFound && foundPort > 0)
            {
                portToUse = foundPort;
                needsAdditionalStartOptions = false; // If at least a port has been found, we're happy
            }
            if (passwordWasFound && !string.IsNullOrWhiteSpace(foundPassword))
            {
                passwordToUse = foundPassword; // If we were told to use a password, then we'll do that
            }

            if (needsAdditionalStartOptions)
            {
                additionalStartOptions += $" -netconport {portToUse}";
            }

            if (csgo == null)
            {
                // CS:GO is not opened, just ask if the user wants to start it
                if (MessageBox.Show("Do you want to start CS:GO now?", "Start CS:GO", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SteamShared.Globals.Settings.SteamHelper.StartApp(SteamShared.CsgoHelper.GameID, additionalStartOptions);
                    ShowMessage.Info("CS:GO is currently attempting to start. Retry to connect when you're in-game.\n\nRetrying while on a map will load the corresponding map.");
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                // CS:GO is already running
                if (needsAdditionalStartOptions)
                {
                    // We need to restart the game, in order to set a port
                    if (MessageBox.Show("The game is already running and doesn't seem to have a connection enabled.\n\nDo you want to kill and restart CS:GO now?", "Restart CS:GO", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Wait for CS:GO to close here (with timeout!)
                        csgo.Kill();
                        csgo.WaitForExit(5000);

                        if (csgo.HasExited)
                        {
                            SteamShared.Globals.Settings.SteamHelper.StartApp(SteamShared.CsgoHelper.GameID, additionalStartOptions);

                            ShowMessage.Info("CS:GO is currently attempting to restart. Retry to connect when you're in-game.\n\nRetrying while on a map will load the corresponding map.");
                            return;
                        }
                        else
                        {
                            ShowMessage.Error("CS:GO has not closed after 5 seconds of waiting. Try again when the game has closed.");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var connectResult = await SteamShared.Globals.Settings.CsgoSocket.ConnectAsync(portToUse, null);
            SteamShared.Globals.Settings.CsgoSocket.OnDisconnect += CsgoSocket_OnDisconnect;
            if (connectResult == CsgoSocketConnectResult.Success)
            {
                // Connected
                this.setPlayerConnected(true);

                // Get map name and try to select it
                await this.loadCurrentMap();
            }
        }

        private async Task loadCurrentMap()
        {
            string currentMapName = await SteamShared.Globals.Settings.CsgoSocket.GetMapName();

            if (currentMapName == null)
                return;

            foreach (var mapItem in comboBoxMaps.Items)
            {
                if (mapItem is ComboBoxItem item && item.Tag is CsgoMap map)
                {
                    if (currentMapName.ToLower().Contains(map.MapFileName.ToLower()))
                    {
                        // We found the map in the list, that the player is currently on
                        comboBoxMaps.SelectedItem = mapItem;
                        break;
                    }
                }
            }
        }


        private void setTargetOrBombPoint(Vector3 givenCoords = null)
        {
            if (this.DrawMode == eDrawMode.Shooting)
            {
                if (this.targetPoint == null)
                    this.targetPoint = new MapPoint();

                Point newPointPosPixels = givenCoords == null ? Mouse.GetPosition(this.mapCanvas) : this.getPointFromGameCoords(givenCoords.X, givenCoords.Y);
                this.canvasRemove(this.targetPoint.Circle);

                var circle = this.getPointEllipse(this.leftClickPointColour);

                this.canvasAdd(circle);

                this.targetPoint.PercentageX = newPointPosPixels.X * 100f / this.mapCanvas.ActualWidth;
                this.targetPoint.PercentageY = newPointPosPixels.Y * 100f / this.mapCanvas.ActualHeight;
                this.targetPoint.PercentageScale = circle.Width * 100f / this.mapCanvas.ActualWidth;
                this.targetPoint.Z = givenCoords == null ? this.currentMouseCoord.Z : givenCoords.Z;
                if (this.currentHeightLayer >= 0 && givenCoords == null)
                {
                    // Associate area ID to see if we want just 2D distance in case one point has no area (and with that, Z value) with it
                    this.targetPoint.AssociatedAreaID = (int)this.hoveredNavAreas?[this.currentHeightLayer].ID;
                }
                else if (givenCoords != null)
                {
                    NavArea closestArea = this.getClosestNavAreaToPoint(givenCoords);
                    if (closestArea != null)
                    {
                        this.targetPoint.AssociatedAreaID = (int)closestArea.ID;
                    }
                }
                else
                    this.targetPoint.AssociatedAreaID = -1;

                this.targetPoint.Circle = circle;
                this.redrawLine = true;

                this.drawPointsAndConnectingLine();
            }
            else if (this.DrawMode == eDrawMode.Bomb)
            {
                if (this.bombPoint == null)
                    this.bombPoint = new MapPoint();

                Point newPointPosPixels = givenCoords == null ? Mouse.GetPosition(this.mapCanvas) : this.getPointFromGameCoords(givenCoords.X, givenCoords.Y);
                this.canvasRemove(this.bombPoint.Circle);

                var circle = this.getBombEllipse(this.leftClickPointColour);

                this.canvasAdd(circle);

                this.bombPoint.PercentageX = newPointPosPixels.X * 100f / this.mapCanvas.ActualWidth;
                this.bombPoint.PercentageY = newPointPosPixels.Y * 100f / this.mapCanvas.ActualHeight;
                this.bombPoint.PercentageScale = circle.Width * 100f / this.mapCanvas.ActualWidth;
                this.bombPoint.Z = givenCoords == null ? this.currentMouseCoord.Z : givenCoords.Z;
                if (this.currentHeightLayer >= 0 && givenCoords == null)
                {
                    // Associate area ID to see if we want just 2D distance in case one point has no area (and with that, Z value) with it
                    this.bombPoint.AssociatedAreaID = (int)this.hoveredNavAreas?[this.currentHeightLayer].ID;
                }
                else if (givenCoords != null)
                {
                    NavArea closestArea = this.getClosestNavAreaToPoint(givenCoords);
                    if (closestArea != null)
                    {
                        this.bombPoint.AssociatedAreaID = (int)closestArea.ID;
                    }
                }
                else
                    this.bombPoint.AssociatedAreaID = -1;

                this.bombPoint.Circle = circle;

                this.redrawLine = true;

                this.drawPointsAndConnectingLine();
            }
        }

        private void setPlayerPoint(Vector3 givenCoords = null)
        {
            if (this.playerPoint == null)
                this.playerPoint = new MapPoint();

            Point newPointPosPixels = givenCoords == null ? Mouse.GetPosition(this.mapCanvas) : this.getPointFromGameCoords(givenCoords.X, givenCoords.Y); 
            this.canvasRemove(this.playerPoint.Circle);

            var circle = this.getPointEllipse(this.rightClickPointColour);

            this.canvasAdd(circle);

            this.playerPoint.PercentageX = newPointPosPixels.X * 100f / this.mapCanvas.ActualWidth;
            this.playerPoint.PercentageY = newPointPosPixels.Y * 100f / this.mapCanvas.ActualHeight;
            this.playerPoint.PercentageScale = circle.Width * 100f / this.mapCanvas.ActualWidth;
            this.playerPoint.Z = givenCoords == null ? this.currentMouseCoord.Z : givenCoords.Z;

            if (this.currentHeightLayer >= 0 && givenCoords == null)
            {
                // Associate area ID to see if we want just 2D distance in case one point has no area (and with that, Z value) with it
                this.playerPoint.AssociatedAreaID = (int)this.hoveredNavAreas?[this.currentHeightLayer].ID;
            }
            else if (givenCoords != null)
            {
                NavArea closestArea = this.getClosestNavAreaToPoint(givenCoords);
                if(closestArea != null)
                {
                    this.playerPoint.AssociatedAreaID = (int)closestArea.ID;
                }
            }
            else
                this.playerPoint.AssociatedAreaID = -1;

            this.playerPoint.Circle = circle;

            this.redrawLine = true;

            this.drawPointsAndConnectingLine();
        }



        #region events
        private void CsgoSocket_OnDisconnect(object sender, EventArgs e)
        {
            this.setPlayerConnected(false);
        }

        private void rightZoomBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (rightZoomBorder.IsZoomed)
            {
                rightZoomBorder.Reset();
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                rightZoomBorder.Reset();
            }
        }

        private void radioModeShooting_Checked(object sender, RoutedEventArgs e)
        {
            this.resetCanvas();
            this.DrawMode = eDrawMode.Shooting;
            if (this.IsInitialized)
            {
                this.stackArmorSeparated.Visibility = this.stackAreaHit.Visibility = this.stackWeaponUsed.Visibility = this.stackDamageFirearm.Visibility = Visibility.Visible;
                this.stackPlayerStance.Visibility = this.lblPlayerStance.Visibility = this.chkArmorAny.Visibility = this.stackDamageBomb.Visibility = Visibility.Collapsed;
            }
        }

        private void radioModeBomb_Checked(object sender, RoutedEventArgs e)
        {
            this.resetCanvas();
            this.DrawMode = eDrawMode.Bomb;
            if (this.IsInitialized)
            {
                this.stackArmorSeparated.Visibility = this.stackAreaHit.Visibility = this.stackWeaponUsed.Visibility = this.stackDamageFirearm.Visibility = Visibility.Collapsed;
                this.stackPlayerStance.Visibility = this.lblPlayerStance.Visibility = this.chkArmorAny.Visibility = this.stackDamageBomb.Visibility = Visibility.Visible;
            }
        }

        private void mapImage_LayoutUpdated(object sender, EventArgs e)
        {
            this.drawPointsAndConnectingLine();
        }

        private void mapImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.setTargetOrBombPoint();
        }

        private void mapImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.setPlayerPoint();
        }

        private void comboBoxMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var map = ((sender as ComboBox).SelectedItem as ComboBoxItem)?.Tag as CsgoMap;

            if (map != null)
                this.loadMap(map);
        }

        private void settings_UpdatedRadioButton(object sender, RoutedEventArgs e)
        {
            // Redraw the line to also trigger the distance to be recalculated
            this.redrawLine = true;
            this.drawPointsAndConnectingLine();

            // Route this through to recalculate the bomb damage
            this.settings_Updated(null, null);
        }

        private void settings_Updated(object sender, EventArgs e)
        {
            if ((this.DrawMode == eDrawMode.Shooting && this.selectedWeapon == null) || !this.lineDrawn)
            {
                if(txtResult != null && txtResultArmor != null)
                    txtResult.Text = txtResultArmor.Text = "0";

                return;
            }

            if (this.DrawMode == eDrawMode.Shooting)
                this.calculateAndUpdateShootingDamage();
            else if (this.DrawMode == eDrawMode.Bomb)
                calculateAndUpdateBombDamage();

            this.calculateDistanceDuration();
        }

        private void comboWeapons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var weapon = ((sender as ComboBox).SelectedItem as ComboBoxItem)?.Tag as CsgoWeapon;

            this.selectedWeapon = weapon;
            this.fillWeaponInfo();
            settings_Updated(null, null);
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            bool showThanks = false;

            if (Keyboard.Modifiers == ModifierKeys.Control)
                showThanks = true;

            About about = new About(showThanks);
            about.Owner = this;
            about.ShowDialog();
        }

        private void mnuHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help();
            help.Owner = this;
            help.ShowDialog();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            this.recalculateCoordinates();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Clipboard.SetText($"setpos_exact {this.currentMouseCoord.X.ToString(CultureInfo.InvariantCulture)} {this.currentMouseCoord.Y.ToString(CultureInfo.InvariantCulture)} {(this.currentMouseCoord.Z + 25).ToString(CultureInfo.InvariantCulture)}");
            }
            else if(e.Key == Key.PageUp)
            {
                if(this.currentHeightLayer >= 0 && this.currentHeightLayer < this.hoveredNavAreas.Count - 1)
                {
                    this.userHeightLayerOffset = 1;
                    this.userChangedLayer = true;
                    this.recalculateCoordinates();
                }
            }
            else if(e.Key == Key.PageDown)
            {
                if (this.currentHeightLayer > 0)
                {
                    this.userHeightLayerOffset = -1;
                    this.userChangedLayer = true;
                    this.recalculateCoordinates();
                }
            }

            // Pass it on for spacebar pan start
            this.rightZoomBorder.KeyDown(sender, e);

            if(e.Key == Key.Space)
                // We want space for us alone, so give no child element a piece of dat cake
                e.Handled = true;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Pass it on for spacebar pan stop
            this.rightZoomBorder.KeyUp(sender, e);

            if (e.Key == Key.Space)
                // We want space for us alone, so give no child element a piece of dat cake
                e.Handled = true;
        }

        private void mnuOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new wndSettings(this.loadedMap);
            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool reloadWithNewSettings = settingsWindow.ShowDialog() == true;

            if (reloadWithNewSettings)
            {
                // Settings *might* have changed (User pressed "Save" so just in case, reload with new settings)
                this.canvasReload();
            }
        }

        private void btnConnectToCsgo_Click(object sender, RoutedEventArgs e)
        {
            this.establishCsgoConnection();
        }

        private async void btnSetAsTargetPos_Click(object sender, RoutedEventArgs e)
        {
            // Get player position
            Vector3 playerPos = await SteamShared.Globals.Settings.CsgoSocket.GetPlayerPosition();

            if (playerPos == null)
            {
                ShowMessage.Error("No valid in-game position was found.\n\nMake sure you're not on a server that you're not the admin on.");
                return;
            }

            this.setTargetOrBombPoint(playerPos);
        }

        private async void btnSetAsPlayerPos_Click(object sender, RoutedEventArgs e)
        {
            // Get player position
            Vector3 playerPos = await SteamShared.Globals.Settings.CsgoSocket.GetPlayerPosition();

            if (playerPos == null)
            {
                ShowMessage.Error("No valid in-game position was found.\n\nMake sure you're not on a server that you're not the admin on.");
                return;
            }

            this.setPlayerPoint(playerPos);
        }

        private async void btnLoadCurrentMap_Click(object sender, RoutedEventArgs e)
        {
            await this.loadCurrentMap();
        }

        private async void btnDisconnectFromCsgo_Click(object sender, RoutedEventArgs e)
        {
            await SteamShared.Globals.Settings.CsgoSocket.DisconnectAsync();
            this.setPlayerConnected(false);
        }
        #endregion
    }

    enum eDrawMode { Shooting, Bomb }
}
