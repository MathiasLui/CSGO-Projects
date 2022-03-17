﻿using System;
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
using Damage_Calculator.Models;
using Damage_Calculator.ZatVdfParser;
using System.Xml.Serialization;
using System.Globalization;

namespace Damage_Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets or sets the point that will be there when left-clicking a map.
        /// </summary>
        private MapPoint leftPoint = new MapPoint();
        private Color leftPointColour = Color.FromArgb(140, 255, 0, 0);

        /// <summary>
        /// Gets or sets the point that will be there when right-clicking a map.
        /// </summary>
        private MapPoint rightPoint = new MapPoint();
        private Color rightPointColour = Color.FromArgb(140, 0, 255, 0);

        private Line connectingLine = new Line();

        private MapPoint bombCircle = new MapPoint();

        private eDrawMode DrawMode = eDrawMode.Shooting;

        // Extra icons
        private Image CTSpawnIcon;
        private Image TSpawnIcon;
        private Image ASiteIcon;
        private Image BSiteIcon;

        private double unitsDistance = -1;

        /// <summary>
        /// Gets or sets the currently loaded map.
        /// </summary>
        private CsgoMap loadedMap;
        private CsgoWeapon selectedWeapon;

        private BackgroundWorker bgWorker = new BackgroundWorker();

        private bool lineDrawn = false;

        public MainWindow()
        {
            InitializeComponent();

            Globals.Settings.CsgoHelper.CsgoPath = Globals.Settings.SteamHelper.GetGamePathFromExactName("Counter-Strike: Global Offensive");
            if (Globals.Settings.CsgoHelper.CsgoPath == null)
            {
                MessageBox.Show("Make sure you have installed CS:GO and Steam correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.WorkerReportsProgress = true;

            this.gridLoading.Visibility = Visibility.Visible;
            bgWorker.RunWorkerAsync();
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

                this.comboBoxMaps.ItemsSource = maps.OrderBy(m => m.Content);
                if (maps.Count > 0)
                    this.comboBoxMaps.SelectedIndex = 0;
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

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.gridLoading.Visibility = Visibility.Collapsed;
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var maps = Globals.Settings.CsgoHelper.GetMaps();
            bgWorker.ReportProgress(0, maps);
            var serializer = new XmlSerializer(typeof(List<CsgoWeapon>));

            List<CsgoWeapon> weapons;

            string itemsFile = System.IO.Path.Combine(Globals.Settings.CsgoHelper.CsgoPath, "csgo\\scripts\\items\\items_game.txt");
            string saveFileDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSGO Damage Calculator");
            string currentHash = calculateMD5(itemsFile);

            if (Directory.Exists(saveFileDir))
            {
                string[] files = Directory.GetFiles(saveFileDir);
                if (files.Length == 1)
                {
                    // Compare hashes
                    string oldHash = System.IO.Path.GetFileName(files[0]);

                    if(currentHash == oldHash)
                    {
                        weapons = (List<CsgoWeapon>)serializer.Deserialize(new FileStream(System.IO.Path.Combine(saveFileDir, currentHash), FileMode.Open));
                        bgWorker.ReportProgress(1, weapons);
                        return;
                    }
                    else
                    {
                        foreach (string file in files)
                        {
                            File.Delete(file);
                        }
                    }
                }
                else
                {
                    foreach(string file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(saveFileDir);
            }

            weapons = Globals.Settings.CsgoHelper.GetWeapons();
            serializer.Serialize(new FileStream(System.IO.Path.Combine(saveFileDir, currentHash), FileMode.Create), weapons);
            bgWorker.ReportProgress(1, weapons);
        }
        #endregion

        private void resetCanvas(bool updatedViewSettings = false)
        {
            if (this.IsInitialized)
            {
                this.pointsCanvas.Children.Clear();
                if (!updatedViewSettings)
                {
                    this.leftPoint = null;
                    this.rightPoint = null;
                    this.connectingLine = null;
                    this.bombCircle = null;
                    this.unitsDistance = -1;
                    this.textDistanceMetres.Text = "0";
                    this.textDistanceUnits.Text = "0";
                    this.txtResult.Text = "0";
                    this.txtResultArmor.Text = "0";
                }
            }
        }

        private void loadMap(CsgoMap map)
        {
            mapImage.Source = map.MapImage;

            if (map.BspFilePath != null)
            {
                // Map radar has an actual existing BSP map file
                this.parseBspData(map);
            }

            // Set indicator checkboxes
            this.chkHasMapFile.IsChecked = map.BspFilePath != null;
            this.chkHasNavFile.IsChecked = map.NavFilePath != null;

            this.resetCanvas();
            this.rightZoomBorder.Reset();

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

            this.loadedMap = map;
        }

        private void parseBspData(CsgoMap map)
        {
            map.EntityList = Globals.Settings.CsgoHelper.ReadEntityListFromBsp(map.BspFilePath);

            // Current format for one entity is: 
            //
            //  {
            //      "property"  "value"
            //  }
            //
            // but we want this format like in VDF files, so we can use our VDF parser:
            //
            //  "sometext"
            //  {
            //      "property" "value"
            //  }
            //

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

                // Check for map parameters entity
                if (className == "info_map_parameters")
                {
                    string bombRadius = entityRootVdf["bombradius"]?.Value;
                    if (bombRadius != null)
                    {
                        // Map has custom bomb radius
                        if (float.TryParse(bombRadius, out float bombRad) && bombRad >= 0)
                        {
                            // bombradius is valid and not negative
                            map.BombDamage = bombRad;
                        }
                    }
                }

                if (className == "info_player_terrorist" || className == "info_player_counterterrorist")
                {
                    // Entity is spawn point
                    var spawn = new PlayerSpawn();
                    spawn.Team = className == "info_player_terrorist" ? ePlayerTeam.Terrorist : ePlayerTeam.CounterTerrorist;
                    spawn.Origin = this.stringToVector3(entityRootVdf["origin"]?.Value) ?? Vector3.Empty;
                    spawn.Angles = this.stringToVector3(entityRootVdf["angles"]?.Value) ?? Vector3.Empty;

                    // highest priority by default. if ALL spawns are high priority, then there are no priority spawns,
                    // in that case we will later check the amount of priority spawns and if it is the same as the total spawns.
                    // This is done for each team separately.
                    int priority = 0;
                    int.TryParse(entityRootVdf["priority"]?.Value, out priority);

                    if (priority < 1) // 0 or nothing means it's highest priority, then counts up as an integer with decreasing priority
                    {
                        spawn.IsPriority = true;
                        // Count prio spawns
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
                    // Entity is hostage spawn point (equivalent but latter is csgo specific)
                    var spawn = new PlayerSpawn();
                    spawn.Origin = this.stringToVector3(entityRootVdf["origin"]?.Value) ?? Vector3.Empty;
                    spawn.Angles = this.stringToVector3(entityRootVdf["angles"]?.Value) ?? Vector3.Empty;

                    // Count all hostage spawns
                    map.AmountHostages++;

                    spawn.Type = eSpawnType.Hostage;

                    map.SpawnPoints.Add(spawn);
                }
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

        private double getPixelsFromUnits(double units)
        {
            int mapSizePixels = (this.mapImage.Source as BitmapSource).PixelWidth;
            double mapSizeUnits = mapSizePixels * this.loadedMap.MapSizeMultiplier;
            return Math.Abs(units) * this.pointsCanvas.ActualWidth / mapSizeUnits;
        }

        private Point getPointFromGameCoords(double x, double y)
        {
            Point p = new Point();
            p.X = this.getPixelsFromUnits(this.loadedMap.UpperLeftWorldXCoordinate - x);
            p.Y = this.getPixelsFromUnits(this.loadedMap.UpperLeftWorldYCoordinate - y);
            return p;
        }

        private Ellipse getPointEllipse(Color strokeColour)
        {
            Ellipse circle = new Ellipse();
            circle.Fill = null;
            circle.Width = circle.Height = this.getPixelsFromUnits(150);
            circle.Stroke = new SolidColorBrush(strokeColour);
            circle.StrokeThickness = 2;
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
            circle.StrokeThickness = 3;
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

        private void updateCirclePositions()
        {
            if (this.connectingLine == null)
                this.connectingLine = new Line();

            if (this.leftPoint?.Circle != null)
            {
                if (pointsCanvas.Children.IndexOf(this.leftPoint.Circle) == -1 && this.mnuShowDrawnMarkers.IsChecked == true)
                    pointsCanvas.Children.Add(this.leftPoint.Circle);

                Canvas.SetLeft(this.leftPoint.Circle, (this.leftPoint.PercentageX * pointsCanvas.ActualWidth / 100f) - (this.leftPoint.Circle.Width / 2));
                Canvas.SetTop(this.leftPoint.Circle, (this.leftPoint.PercentageY * pointsCanvas.ActualHeight / 100f) - (this.leftPoint.Circle.Height / 2));
                this.leftPoint.Circle.Width = this.leftPoint.Circle.Height = this.leftPoint.PercentageScale * this.pointsCanvas.ActualWidth / 100f;
            }

            if (this.rightPoint?.Circle != null)
            {
                if (pointsCanvas.Children.IndexOf(this.rightPoint.Circle) == -1 && this.mnuShowDrawnMarkers.IsChecked == true)
                    pointsCanvas.Children.Add(this.rightPoint.Circle);

                Canvas.SetLeft(this.rightPoint.Circle, (this.rightPoint.PercentageX * pointsCanvas.ActualWidth / 100f) - (this.rightPoint.Circle.Width / 2));
                Canvas.SetTop(this.rightPoint.Circle, (this.rightPoint.PercentageY * pointsCanvas.ActualHeight / 100f) - (this.rightPoint.Circle.Height / 2));
                this.rightPoint.Circle.Width = this.rightPoint.Circle.Height = this.rightPoint.PercentageScale * this.pointsCanvas.ActualWidth / 100f;
            }

            if (this.bombCircle?.Circle != null)
            {
                if (pointsCanvas.Children.IndexOf(this.bombCircle.Circle) == -1 && this.mnuShowDrawnMarkers.IsChecked == true)
                    pointsCanvas.Children.Add(this.bombCircle.Circle);

                Canvas.SetLeft(this.bombCircle.Circle, (this.bombCircle.PercentageX * pointsCanvas.ActualWidth / 100f) - (this.bombCircle.Circle.Width / 2));
                Canvas.SetTop(this.bombCircle.Circle, (this.bombCircle.PercentageY * pointsCanvas.ActualHeight / 100f) - (this.bombCircle.Circle.Height / 2));
                this.bombCircle.Circle.Width = this.bombCircle.Circle.Height = this.bombCircle.PercentageScale * this.pointsCanvas.ActualWidth / 100f;
            }

            if((this.leftPoint?.Circle != null || this.bombCircle?.Circle != null) && this.rightPoint?.Circle != null)
            {
                // Right click cirle exists, and left click circle or left click bomb circle exists
                // Ready to calculate damage and draw the in-between line
                if (this.DrawMode == eDrawMode.Shooting)
                {
                    // Update line start pos to left click circle
                    this.connectingLine.X1 = Canvas.GetLeft(this.leftPoint.Circle) + (this.leftPoint.Circle.Width / 2);
                    this.connectingLine.Y1 = Canvas.GetTop(this.leftPoint.Circle) + (this.leftPoint.Circle.Height / 2);
                }
                else
                {
                    // Update line start pos to left click bomb circle
                    this.connectingLine.X1 = Canvas.GetLeft(this.bombCircle.Circle) + (this.bombCircle.Circle.Width / 2);
                    this.connectingLine.Y1 = Canvas.GetTop(this.bombCircle.Circle) + (this.bombCircle.Circle.Height / 2);
                }
                // Update line end pos to right click circle
                this.connectingLine.X2 = Canvas.GetLeft(this.rightPoint.Circle) + (this.rightPoint.Circle.Width / 2);
                this.connectingLine.Y2 = Canvas.GetTop(this.rightPoint.Circle) + (this.rightPoint.Circle.Height / 2);

                this.connectingLine.Fill = null;
                this.connectingLine.Stroke = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)); // White, slightly transparent
                this.connectingLine.StrokeThickness = 2;
                // Make it not clickable
                this.connectingLine.IsHitTestVisible = false;

                int indexLine = pointsCanvas.Children.IndexOf(this.connectingLine);
                if (indexLine < 0 && this.mnuShowDrawnMarkers.IsChecked == true)
                {
                    pointsCanvas.Children.Add(this.connectingLine);
                    this.lineDrawn = true;
                }

                // Update top right corner distance texts
                this.unitsDistance = this.calculateDotDistanceInUnits();

                this.textDistanceUnits.Text = Math.Round(this.unitsDistance, 2).ToString();
                this.textDistanceMetres.Text = Math.Round(this.unitsDistance / 39.37, 2).ToString();

                // Recalculate damage
                this.settings_Updated(null, null);
            }
            else
            {
                // No 2 circles are being drawn that need any connection
                this.lineDrawn = false;
            }

            if(this.loadedMap != null)
            {
                this.positionIcons();
                this.positionSpawns();
            }
        }

        private void positionSpawns()
        {
            double size = this.getPixelsFromUnits(70);
            foreach (var spawn in this.loadedMap.SpawnPoints)
            {
                if (!spawn.IsPriority && mnuAllowNonPrioritySpawns.IsChecked == false)
                    continue;

                if (spawn.Type == eSpawnType.Standard && mnuShowStandardSpawns.IsChecked == false)
                    continue;
                else if (spawn.Type == eSpawnType.Wingman && mnuShow2v2Spawns.IsChecked == false)
                    continue;
                else if (spawn.Type == eSpawnType.Hostage && mnuShowHostageSpawns.IsChecked == false)
                    continue;

                var existingViewBox = this.pointsCanvas.Children.OfType<Viewbox>().FirstOrDefault(v => v.Tag == spawn);
                if (existingViewBox == null)
                {
                    Viewbox box = new Viewbox();

                    // Are viewboxes even needed?
                    var blipControl = this.getSpawnBlip(spawn);
                    box.Tag = spawn;
                    box.Child = blipControl;
                    box.Width = box.Height = size;

                    Point newCoords = this.getPointFromGameCoords(spawn.Origin.X, spawn.Origin.Y);

                    pointsCanvas.Children.Add(box);
                    Canvas.SetLeft(box, newCoords.X - box.Width / 2);
                    Canvas.SetTop(box, newCoords.Y - box.Height / 2);
                }
            }
        }

        private void positionIcons()
        {
            double left;
            double top;

            if (mnuShowSpawnAreas.IsChecked == true)
            {
                // CT Icon
                if (this.CTSpawnIcon == null)
                {
                    this.CTSpawnIcon = new Image();
                    this.CTSpawnIcon.Source = new BitmapImage(new Uri("icon_ct.png", UriKind.RelativeOrAbsolute));
                    this.CTSpawnIcon.Width = 25;
                    this.CTSpawnIcon.Height = 25;
                    this.CTSpawnIcon.Opacity = 0.6;
                    this.CTSpawnIcon.IsHitTestVisible = false;
                }

                if (pointsCanvas.Children.IndexOf(CTSpawnIcon) == -1 && this.loadedMap.CTSpawnMultiplierX >= 0 && this.loadedMap.CTSpawnMultiplierY >= 0)
                    pointsCanvas.Children.Add(CTSpawnIcon);

                left = this.loadedMap.CTSpawnMultiplierX * this.mapImage.ActualWidth - (CTSpawnIcon.ActualWidth / 2);
                top = this.loadedMap.CTSpawnMultiplierY * this.mapImage.ActualWidth - (CTSpawnIcon.ActualHeight / 2);
                if (left >= 0 && left <= this.mapImage.ActualWidth && top >= 0 && top <= this.mapImage.ActualHeight)
                {
                    Canvas.SetLeft(CTSpawnIcon, left);
                    Canvas.SetTop(CTSpawnIcon, top);
                }

                // T Icon
                if (this.TSpawnIcon == null)
                {
                    this.TSpawnIcon = new Image();
                    this.TSpawnIcon.Source = new BitmapImage(new Uri("icon_t.png", UriKind.RelativeOrAbsolute));
                    this.TSpawnIcon.Width = 25;
                    this.TSpawnIcon.Height = 25;
                    this.TSpawnIcon.Opacity = 0.6;
                    this.TSpawnIcon.IsHitTestVisible = false;
                }

                if (pointsCanvas.Children.IndexOf(TSpawnIcon) == -1 && this.loadedMap.TSpawnMultiplierX >= 0 && this.loadedMap.TSpawnMultiplierY >= 0)
                    pointsCanvas.Children.Add(TSpawnIcon);

                left = this.loadedMap.TSpawnMultiplierX * this.mapImage.ActualWidth - (TSpawnIcon.ActualWidth / 2);
                top = this.loadedMap.TSpawnMultiplierY * this.mapImage.ActualWidth - (TSpawnIcon.ActualHeight / 2);
                if (left >= 0 && left <= this.mapImage.ActualWidth && top >= 0 && top <= this.mapImage.ActualHeight)
                {
                    Canvas.SetLeft(TSpawnIcon, left);
                    Canvas.SetTop(TSpawnIcon, top);
                }
            }

            if (mnuShowBombSites.IsChecked == true)
            {
                // Bomb A Icon
                if (this.ASiteIcon == null)
                {
                    this.ASiteIcon = new Image();
                    this.ASiteIcon.Source = new BitmapImage(new Uri("icon_a_site.png", UriKind.RelativeOrAbsolute));
                    this.ASiteIcon.Width = 25;
                    this.ASiteIcon.Height = 25;
                    this.ASiteIcon.Opacity = 0.6;
                    this.ASiteIcon.IsHitTestVisible = false;
                }

                if (pointsCanvas.Children.IndexOf(ASiteIcon) == -1 && this.loadedMap.BombAX >= 0 && this.loadedMap.BombAY >= 0)
                    pointsCanvas.Children.Add(ASiteIcon);

                left = this.loadedMap.BombAX * this.mapImage.ActualWidth - (ASiteIcon.ActualWidth / 2);
                top = this.loadedMap.BombAY * this.mapImage.ActualWidth - (ASiteIcon.ActualHeight / 2);
                if (left >= 0 && left <= this.mapImage.ActualWidth && top >= 0 && top <= this.mapImage.ActualHeight)
                {
                    Canvas.SetLeft(ASiteIcon, left);
                    Canvas.SetTop(ASiteIcon, top);
                }

                // Bomb B Icon
                if (this.BSiteIcon == null)
                {
                    this.BSiteIcon = new Image();
                    this.BSiteIcon.Source = new BitmapImage(new Uri("icon_b_site.png", UriKind.RelativeOrAbsolute));
                    this.BSiteIcon.Width = 25;
                    this.BSiteIcon.Height = 25;
                    this.BSiteIcon.Opacity = 0.6;
                    this.BSiteIcon.IsHitTestVisible = false;
                }

                if (pointsCanvas.Children.IndexOf(BSiteIcon) == -1 && this.loadedMap.BombBX >= 0 && this.loadedMap.BombBY >= 0)
                    pointsCanvas.Children.Add(BSiteIcon);

                left = this.loadedMap.BombBX * this.mapImage.ActualWidth - (BSiteIcon.ActualWidth / 2);
                top = this.loadedMap.BombBY * this.mapImage.ActualWidth - (BSiteIcon.ActualHeight / 2);
                if (left >= 0 && left <= this.mapImage.ActualWidth && top >= 0 && top <= this.mapImage.ActualHeight)
                {
                    Canvas.SetLeft(BSiteIcon, left);
                    Canvas.SetTop(BSiteIcon, top);
                }
            }
        }

        private double calculateDotDistanceInUnits()
        {
            double leftX = this.connectingLine.X1;
            double leftY = this.connectingLine.Y1;

            double rightX = this.connectingLine.X2;
            double rightY = this.connectingLine.Y2;

            // Distance in shown pixels
            double diffPixels = Math.Sqrt(Math.Pow(Math.Abs(leftX - rightX), 2) + Math.Pow(Math.Abs(leftY - rightY), 2));

            // Percentage on shown pixels
            double diffPerc = diffPixels * 100f / this.mapImage.ActualWidth;

            // Distance on original pixel scales
            double diffPixelsOriginal = diffPerc * (this.mapImage.Source as BitmapSource).PixelWidth / 100f;

            // times scale multiplier
            double unitsDifference = diffPixelsOriginal * this.loadedMap.MapSizeMultiplier;

            return unitsDifference;
        }

        private void calculateAndUpdateShootingDamage()
        {
            double damage = this.selectedWeapon.BaseDamage;
            double absorbedDamageByArmor = 0;
            bool wasArmorHit = false;

            if (this.unitsDistance > this.selectedWeapon.MaxBulletRange)
            {
                damage = 0;
                txtResult.Text = txtResultArmor.Text = damage.ToString();
                return;
            }

            // Range
            damage *= Math.Pow(this.selectedWeapon.DamageDropoff, double.Parse((this.unitsDistance / 500f).ToString()));

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
                    absorbedDamageByArmor = previousDamage - (int)damage;
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
                    absorbedDamageByArmor = previousDamage - (int)damage;
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
                    absorbedDamageByArmor = previousDamage - (int)damage;
                    wasArmorHit = true;
                }
            }
            else
            {
                // Legs
                damage *= 0.75f;
            }

            txtResult.Text = ((int)damage).ToString();

            txtResultArmor.Text = (wasArmorHit ? (int)(absorbedDamageByArmor / 2) : 0).ToString();

            // TODO: HP and armor and HP and armor left after shot
        }

        private void calculateAndUpdateBombDamage()
        {
            const double damagePercentage = 1.0d;

            double flDamage = this.loadedMap.BombDamage; // 500 - default, if radius is not written on the map https://i.imgur.com/mUSaTHj.png
            double flBombRadius = flDamage * 3.5d;
            double flDistanceToLocalPlayer = (double)this.unitsDistance;// ((c4bomb origin + viewoffset) - (localplayer origin + viewoffset))
            double fSigma = flBombRadius / 3.0d;
            double fGaussianFalloff = Math.Exp(-flDistanceToLocalPlayer * flDistanceToLocalPlayer / (2.0d * fSigma * fSigma));
            double flAdjustedDamage = flDamage * fGaussianFalloff * damagePercentage;

            bool wasArmorHit = false;
            double flAdjustedDamageBeforeArmor = flAdjustedDamage;

            if (chkArmorAny.IsChecked == true)
            {
                flAdjustedDamage = scaleDamageArmor(flAdjustedDamage, 100);
                wasArmorHit = true;
            }

            txtResult.Text = ((int)flAdjustedDamage).ToString();

            txtResultArmor.Text = (wasArmorHit ? (int)((flAdjustedDamageBeforeArmor - flAdjustedDamage) / 2) : 0).ToString();
        }

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

        #region events
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

        private void visibilityMenu_CheckChanged(object sender, RoutedEventArgs e)
        {
            this.resetCanvas(true);
            this.UpdateLayout();
        }

        private void radioModeShooting_Checked(object sender, RoutedEventArgs e)
        {
            this.resetCanvas();
            this.DrawMode = eDrawMode.Shooting;
            if (this.IsInitialized)
            {
                this.stackArmorSeparated.Visibility = this.stackAreaHit.Visibility = this.stackWeaponUsed.Visibility = Visibility.Visible;
                this.chkArmorAny.Visibility = Visibility.Collapsed;
            }
        }

        private void radioModeBomb_Checked(object sender, RoutedEventArgs e)
        {
            this.resetCanvas();
            this.DrawMode = eDrawMode.Bomb;
            if (this.IsInitialized)
            {
                this.stackArmorSeparated.Visibility = this.stackAreaHit.Visibility = this.stackWeaponUsed.Visibility = Visibility.Collapsed;
                this.chkArmorAny.Visibility = Visibility.Visible;
            }
        }

        private void mapImage_LayoutUpdated(object sender, EventArgs e)
        {
            this.updateCirclePositions();
        }

        private void mapImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.DrawMode == eDrawMode.Shooting)
            {
                if (this.leftPoint == null)
                    this.leftPoint = new MapPoint();

                Point mousePos = Mouse.GetPosition(this.pointsCanvas);
                this.pointsCanvas.Children.Remove(this.leftPoint.Circle);

                var circle = this.getPointEllipse(this.leftPointColour);

                this.pointsCanvas.Children.Add(circle);

                this.leftPoint.PercentageX = mousePos.X * 100f / this.pointsCanvas.ActualWidth;
                this.leftPoint.PercentageY = mousePos.Y * 100f / this.pointsCanvas.ActualHeight;
                this.leftPoint.PercentageScale = circle.Width * 100f / this.pointsCanvas.ActualWidth;

                this.leftPoint.Circle = circle;

                this.updateCirclePositions();
            }
            else if (this.DrawMode == eDrawMode.Bomb)
            {
                if (this.bombCircle == null)
                    this.bombCircle = new MapPoint();

                Point mousePos = Mouse.GetPosition(this.pointsCanvas);
                this.pointsCanvas.Children.Remove(this.bombCircle.Circle);

                var circle = this.getBombEllipse(this.leftPointColour);

                this.pointsCanvas.Children.Add(circle);

                this.bombCircle.PercentageX = mousePos.X * 100f / this.pointsCanvas.ActualWidth;
                this.bombCircle.PercentageY = mousePos.Y * 100f / this.pointsCanvas.ActualHeight;
                this.bombCircle.PercentageScale = circle.Width * 100f / this.pointsCanvas.ActualWidth;

                this.bombCircle.Circle = circle;

                this.updateCirclePositions();
            }
        }

        private void mapImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.rightPoint == null)
                this.rightPoint = new MapPoint();

            Point mousePos = Mouse.GetPosition(this.pointsCanvas);
            this.pointsCanvas.Children.Remove(this.rightPoint.Circle);

            var circle = this.getPointEllipse(this.rightPointColour);

            this.pointsCanvas.Children.Add(circle);

            this.rightPoint.PercentageX = mousePos.X * 100f / this.pointsCanvas.ActualWidth;
            this.rightPoint.PercentageY = mousePos.Y * 100f / this.pointsCanvas.ActualHeight;
            this.rightPoint.PercentageScale = circle.Width * 100f / this.pointsCanvas.ActualWidth;

            this.rightPoint.Circle = circle;

            this.updateCirclePositions();
        }

        private void changeTheme_Click(object sender, RoutedEventArgs e)
        {
            switch (int.Parse(((MenuItem)sender).Uid))
            {
                case 0: REghZyFramework.Themes.ThemesController.SetTheme(REghZyFramework.Themes.ThemesController.ThemeTypes.Dark);
                    rectTop.Fill = rectSide.Fill = new SolidColorBrush(Colors.White);
                    txtEasterEggMetres.Text = "Metres:";
                    break;
                case 1: REghZyFramework.Themes.ThemesController.SetTheme(REghZyFramework.Themes.ThemesController.ThemeTypes.Light);
                    rectTop.Fill = rectSide.Fill = new SolidColorBrush(Colors.Black);
                    txtEasterEggMetres.Text = "Meters:";
                    break;
            }
            e.Handled = true;
        }

        private void comboBoxMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var map = ((sender as ComboBox).SelectedItem as ComboBoxItem)?.Tag as CsgoMap;

            if (map != null)
                this.loadMap(map);
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
        }

        private void comboWeapons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var weapon = ((sender as ComboBox).SelectedItem as ComboBoxItem)?.Tag as CsgoWeapon;

            this.selectedWeapon = weapon;
            settings_Updated(null, null);
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
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
            if (this.mapImage.Source == null)
                return;

            var position = Mouse.GetPosition(mapImage);
            if (position.X >= 0 && position.Y >= 0 && position.X <= mapImage.ActualWidth && position.Y <= mapImage.ActualHeight)
            {
                // Percentage on shown pixels
                double diffPercX = position.X * 100f / this.mapImage.ActualWidth;
                // Distance on original pixel scales
                double diffPixelsOriginalX = diffPercX * (this.mapImage.Source as BitmapSource).PixelWidth / 100f;
                // times scale multiplier
                double unitsDifferenceX = diffPixelsOriginalX * this.loadedMap.MapSizeMultiplier;
                txtCursorX.Text = Math.Round(this.loadedMap.UpperLeftWorldXCoordinate + unitsDifferenceX, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);

                // Percentage on shown pixels
                double diffPercY = position.Y * 100f / this.mapImage.ActualWidth;
                // Distance on original pixel scales
                double diffPixelsOriginalY = diffPercY * (this.mapImage.Source as BitmapSource).PixelWidth / 100f;
                // times scale multiplier
                double unitsDifferenceY = diffPixelsOriginalY * this.loadedMap.MapSizeMultiplier;
                txtCursorY.Text = Math.Round(this.loadedMap.UpperLeftWorldYCoordinate - unitsDifferenceY, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                txtCursorX.Text = txtCursorY.Text = "0";
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Clipboard.SetText(txtCursorX.Text + " " + txtCursorY.Text);
            }

            // Pass it on for spacebar pan start
            this.rightZoomBorder.KeyDown(sender, e);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // Pass it on for spacebar pan stop
            this.rightZoomBorder.KeyUp(sender, e);
        }
        #endregion
    }

    enum eDrawMode { Shooting, Bomb }
}
