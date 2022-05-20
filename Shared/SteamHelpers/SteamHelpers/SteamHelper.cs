using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamShared.ZatVdfParser;
using Microsoft.Win32;
using SteamShared.Models;

namespace SteamShared
{
    public class SteamHelper
    {
        private string? steamPath;
        private List<SteamLibrary>? steamLibraries;

        /// <summary>
        /// Gets the absolute path to the Steam install directory.
        /// If it can't be fetched (i.e. Steam is not installed) null is returned.
        /// </summary>
        public string? SteamPath
        {
            get
            {
                if(this.steamPath == null)
                {
                    this.UpdateSteamPath();
                }
                return this.steamPath;
            }
        }

        /// <summary>
        /// Gets a list of all Steam libraries, and whether they're existent or not.
        /// If it can't be fetched (i.e. Steam is not installed) null is returned.
        /// </summary>
        public List<SteamLibrary>? SteamLibraries
        {
            get
            {
                if (this.steamLibraries == null)
                {
                    this.UpdateSteamLibraries();
                }
                return this.steamLibraries;
            }
        }

        /// <summary>
        /// Forcefully tries to update the <see cref="SteamPath"/> property with the current Steam path, even if it should be already set.
        /// </summary>
        public void UpdateSteamPath()
        {
            this.steamPath = this.GetSteamPath();
        }

        /// <summary>
        /// Gets the path to the Steam install directory. (For external use <see cref="SteamPath"/> is preferred.)
        /// </summary>
        /// <returns>The absolute path to the Steam install directory, or null if it can't be fetched.</returns>
        public string? GetSteamPath()
        {
            var steamKey = Registry.CurrentUser.OpenSubKey("software\\valve\\steam");

            if (steamKey == null)
                return null;

            var steamPath = steamKey.GetValue("SteamPath");

            if (steamPath == null)
                return null;

            return steamPath.ToString();
        }

        /// <summary>
        /// Forcefully tries to update the <see cref="SteamLibraries"/> property with the current Steam libraries, even if they should be already set.
        /// </summary>
        public void UpdateSteamLibraries()
        {
            this.steamLibraries = this.GetSteamLibraries();
        }

        /// <summary>
        /// Fetches a list of Steam libraries, which are deposited in the Steam config, as well as whether the libraries exist on the drive.
        /// </summary>
        /// <returns>A list of all deposited Steam libraries, and if they exist.</returns>
        public List<SteamLibrary>? GetSteamLibraries()
        {
            if (this.steamPath == null)
                this.steamPath = this.GetSteamPath();

            if (this.steamPath == null)
                return null;

            string configFilePath = Path.Combine(this.steamPath, "config\\config.vdf");
            if (!File.Exists(configFilePath))
                return null;

            // Fetch additional libraries
            var configFile = new VDFFile(configFilePath);
            IEnumerable<string>? additionalSteamLibraries = configFile["InstallConfigStore"]?["Software"]?["valve"]?["Steam"].Children.Where(c => c.Name!.StartsWith("BaseInstallFolder_")).Select(c => c.Value)!;

            // List libraries plus default Steam directory, because that's the default library
            var allLibraries = new List<SteamLibrary> { new SteamLibrary(this.steamPath) };
            foreach(string addLib in additionalSteamLibraries!)
            {
                allLibraries.Add(new SteamLibrary(addLib.Replace("\\\\", "\\")));
            }

            return allLibraries;
        }

        public List<SteamGame>? GetInstalledGames()
        {
            var steamLibraries = this.GetSteamLibraries();

            if (steamLibraries == null)
                return null;

            var allGames = new List<SteamGame>();

            foreach(var library in steamLibraries)
            {
                if (!library.DoesExist)
                    continue;

                List<string> manifestFiles = Directory.GetFiles(Path.Combine(library.Path!, "steamapps")).ToList().Where(f => this.isAppManifestFile(f)).ToList();

                foreach (string manifestFile in manifestFiles)
                {
                    var manifestVDF = new VDFFile(manifestFile);

                    if (manifestVDF.RootElements.Count < 1)
                        // App manifest might be still existent but the game might not be installed (happened during testing)
                        continue;

                    var root = manifestVDF["AppState"];

                    var currGame = new SteamGame();

                    this.populateGameInfo(currGame, root!);

                    if((currGame.GameState & (int)GameState.StateFullyInstalled) != 0)
                    {
                        // Game was fully installed according to steam
                        allGames.Add(currGame);
                    }
                }
            }

            return allGames;
        }

        public string? GetGamePathFromExactName(string gameName)
        {
            var steamLibraries = this.GetSteamLibraries();

            if (steamLibraries == null)
                return null;

            var allGames = new List<SteamGame>();

            foreach (var library in steamLibraries)
            {
                if (!library.DoesExist)
                    continue;

                List<string> manifestFiles = Directory.GetFiles(Path.Combine(library.Path!, "steamapps")).ToList().Where(f => this.isAppManifestFile(f)).ToList();

                foreach (string manifestFile in manifestFiles)
                {
                    var manifestVDF = new VDFFile(manifestFile);

                    if (manifestVDF.RootElements.Count < 1)
                        // App manifest might be still existent but the game might not be installed (happened during testing)
                        continue;

                    var root = manifestVDF["AppState"];

                    if(root!["name"].Value!.Trim().ToLower() != gameName.Trim().ToLower())
                    {
                        // Not our wanted game, skip
                        continue;
                    }

                    var currGame = new SteamGame();

                    this.populateGameInfo(currGame, root);

                    if ((currGame.GameState & (int)GameState.StateFullyInstalled) != 0)
                    {
                        // Game was fully installed according to steam
                        return Path.Combine(library.Path!, "steamapps", "common", currGame.InstallFolderName!);
                    }
                }
            }

            return null;
        }

        #region Private Methods
        private bool isAppManifestFile(string filePath)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(filePath.Split(new[] { '\\', '/' }).Last(), "appmanifest_\\d+.acf"); ;
        }

        private DateTime fromUnixFormat(long unixFormat)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            return dateTime.AddSeconds(unixFormat);
        }

        private void populateGameInfo(SteamGame game, Element appStateVdf)
        {
            game.Name = appStateVdf["name"].Value;

            game.InstallFolderName = appStateVdf["installdir"].Value;

            if (int.TryParse(appStateVdf["appid"].Value, out int appId))
            {
                game.AppId = appId;
            }

            if (int.TryParse(appStateVdf["StateFlags"].Value, out int stateFlags))
            {
                game.GameState = stateFlags;
            }

            if (long.TryParse(appStateVdf["LastUpdated"].Value, out long lastUpdated))
            {
                game.LastUpdated = fromUnixFormat(lastUpdated);
            }

            if (long.TryParse(appStateVdf["LastOwner"].Value, out long lastOwner))
            {
                game.LastOwnerSteam64Id = lastOwner;
            }

            if (long.TryParse(appStateVdf["BytesToDownload"].Value, out long bytesToDownload))
            {
                game.BytesToDownload = bytesToDownload;
            }

            if (long.TryParse(appStateVdf["BytesDownloaded"].Value, out long bytesDownloaded))
            {
                game.BytesDownloaded = bytesDownloaded;
            }

            if (long.TryParse(appStateVdf["BytesToStage"].Value, out long bytesToStage))
            {
                game.BytesToStage = bytesToStage;
            }

            if (long.TryParse(appStateVdf["BytesStaged"].Value, out long bytesStaged))
            {
                game.BytesStaged = bytesStaged;
            }

            game.KeepAutomaticallyUpdated = appStateVdf["AutoUpdateBehavior"].Value != "0";

            game.AllowOtherUpdatesWhileRunning = appStateVdf["AllowOtherDownloadsWhileRunning"].Value != "0";
        }
        #endregion
    }
}
