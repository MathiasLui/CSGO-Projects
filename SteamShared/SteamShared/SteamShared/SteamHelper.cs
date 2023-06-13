using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamShared.ZatVdfParser;
using Microsoft.Win32;
using SteamShared.Models;
using System.Diagnostics;

namespace SteamShared
{
    public class SteamHelper
    {
        private string? steamPath;
        private List<SteamLibrary>? steamLibraries;
        public List<SteamGame>? InstalledGames;

        /// <summary>
        ///     The absolute path to the Steam install directory.
        ///     If it can't be fetched (i.e. Steam is not installed) null is returned.
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
        ///     Gets a list of all Steam libraries, and whether they're existent or not.
        ///     If it can't be fetched (i.e. Steam is not installed) null is returned.
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
        ///     Forcefully tries to update the <see cref="SteamPath"/> property with the current Steam path, even if it should be already set.
        /// </summary>
        public void UpdateSteamPath()
        {
            this.steamPath = this.GetSteamPath();
        }

        /// <summary>
        ///     The path to the Steam install directory. (For external use <see cref="SteamPath"/> is preferred.)
        /// </summary>
        /// <returns>the absolute path to the Steam install directory, or null if it can't be fetched.</returns>
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

            // Usually the config.vdf had "BaseInstallFolder_" entries,
            // now it seems that these entries don't exist anymore with reinstalls, and maybe they're not up-to-date anyways?
            // Now we try reading the "libraryfolders.vdf", which now also contains the default library location (In the Steam folder, by default under C:)
#if NEWLIBRARYLOCATION
            string configFilePath = Path.Combine(this.steamPath, "config", "libraryfolders.vdf");
            if (!File.Exists(configFilePath))
                return null;

            // Fetch all libraries from the config
            var configFile = new VDFFile(configFilePath);
            IEnumerable<string>? foundSteamLibraries = configFile["libraryfolders"]?.Children.Select(c => c["path"]?.Value)!;

            var allLibraries = new List<SteamLibrary>();

            if (foundSteamLibraries?.Any() != true)
            {
                return null;
            }

            foreach (string foundLib in foundSteamLibraries!)
            {
                // All paths in the file are escaped
                allLibraries.Add(new SteamLibrary(foundLib.Replace("\\\\", "\\")));
            }

            return allLibraries;
#else
            string configFilePath = Path.Combine(this.steamPath, "config\\config.vdf");
            if (!File.Exists(configFilePath))
                return null;

            // Fetch additional libraries
            var configFile = new VDFFile(configFilePath);
            IEnumerable<string>? additionalSteamLibraries = configFile["InstallConfigStore"]?["Software"]?["valve"]?["Steam"].Children.Where(c => c.Name!.StartsWith("BaseInstallFolder_")).Select(c => c.Value)!;

            // List libraries plus default Steam directory, because that's the default library
            var allLibraries = new List<SteamLibrary> { new SteamLibrary(this.steamPath) };

            foreach (string addLib in additionalSteamLibraries!)
            {
                // All paths in the file are escaped
                allLibraries.Add(new SteamLibrary(addLib.Replace("\\\\", "\\")));
            }

            return allLibraries;
#endif
        }

        /// <summary>
        ///     Updates the <see cref="InstalledGames">list of installed steam games</see>.
        /// </summary>
        /// <param name="shouldBeFullyInstalled">Whether to only return games, if they're marked as fully installed.</param>
        /// <param name="force">Whether to fetch them again, even if they were fetched before.</param>
        public void UpdateInstalledGames(bool shouldBeFullyInstalled = false, bool force = false)
        {
            if (!force && this.InstalledGames != null)
                return;

            this.InstalledGames = this.GetInstalledGames(shouldBeFullyInstalled);
        }

        /// <summary>
        ///     Gets a list of fully installed Steam games, as seen by the manifest files.
        /// </summary>
        /// <remarks>
        ///     Games are seen as fully installed, when their manifest file exists,
        ///     and the manifest states that the game is fully installed.
        ///     This means, that if the files are deleted manually, it might still be seen as installed,
        ///     because the manifest file might not change.
        /// </remarks>
        /// <param name="shouldBeFullyInstalled">Whether to only return games, if they're marked as fully installed.</param>
        /// <returns>
        ///     a list of installed Steam games, with some manifest data,
        ///     or <see langword="null"/>, if no games could be fetched or found.
        /// </returns>
        public List<SteamGame>? GetInstalledGames(bool shouldBeFullyInstalled = false)
        {
            // Get all steam library paths
            var steamLibraries = this.GetSteamLibraries();

            // If the steam path couldn't be fetched or no libraries exist, we short-circuit
            if (steamLibraries == null)
                return null;

            var allGames = new List<SteamGame>();

            foreach(var library in steamLibraries)
            {
                if (!library.DoesExist || library.Path is null)
                    continue;

                List<string> manifestFiles = Directory.GetFiles(Path.Combine(library.Path, "steamapps"))
                                                                .Where(f => this.isAppManifestFile(f)).ToList();

                foreach (string manifestFile in manifestFiles)
                {
                    var manifestVDF = new VDFFile(manifestFile);

                    if (manifestVDF.RootElements.Count < 1)
                        // App manifest might be still existent but the game might not be installed (happened during testing)
                        continue;

                    var root = manifestVDF["AppState"];

                    if (root == null)
                        // Parse error of manifest, skip it
                        continue;

                    var currGame = new SteamGame();

                    this.populateGameInfo(currGame, root, library.Path);

                    if(shouldBeFullyInstalled
                        && (currGame.GameState & (int)GameState.StateFullyInstalled) != 0)
                    {
                        // Game was fully installed according to steam
                        allGames.Add(currGame);
                    }
                    else if (!shouldBeFullyInstalled)
                    {
                        // Game doesn't need to be fully installed to be added
                        allGames.Add(currGame);
                    }
                }
            }

            return allGames;
        }

        /// <summary>
        ///     Gets the absolute path of the game name (not folder) provided.
        /// </summary>
        /// <param name="gameName">The name of the game. The case, as well as leading and trailing whitespaces don't matter.</param>
        /// <param name="shouldBeFullyInstalled">Whether to only return it, if it's marked as fully installed.</param>
        /// <param name="folderShouldExist">Whether to only return it, if the folder it's installed in actually exists.</param>
        /// <returns>
        ///     the absolute path of the game, or <see langword="null"/> if not found,
        ///     the game's folder doesn't exist, or it wasn't marked as fully installed, when required to be.
        /// </returns>
        public string? GetGamePathFromExactName(string gameName, bool shouldBeFullyInstalled = false, bool folderShouldExist = true)
        {
            // Will not update, if already updated once before
            this.UpdateInstalledGames(shouldBeFullyInstalled);

            if (this.InstalledGames is null)
                // User is broke or something
                return null;

            gameName = gameName.Trim();

            var foundGame = this.InstalledGames.Where(game => game.Name is not null && game.Name.Trim().Equals(gameName, StringComparison.OrdinalIgnoreCase))
                                                .FirstOrDefault();

            if (foundGame is null)
                return null;

            if (folderShouldExist && !foundGame.GameFolderExists)
                return null;

            if (shouldBeFullyInstalled
                && (foundGame.GameState & (int)GameState.StateFullyInstalled) == 0)
                return null;

            // Match the name while ignoring leading and trailing whitespaces, as well as upper/lower case.
            return foundGame.FullInstallPath;
        }

        /// <summary>
        ///     Gets the most recently logged in Steam user, based on the "MostRecent" value.
        /// </summary>
        /// <returns>
        ///     the most recent logged in Steam user, or <see langword="null"/>, if none has been found or an error has occurred.
        /// </returns>
        public SteamUser? GetMostRecentSteamUser()
        {
            List<SteamUser>? steamUsers = this.GetSteamUsers();

            if (steamUsers == null)
                return null;

            // Gets the user that has logged in most recently.
            // We do this instead of checking, which user has the "MostRecent" VDF property set to 1
            SteamUser? mostRecentLoggedInSteamUser = steamUsers.OrderByDescending(user => user.LastLogin).FirstOrDefault();

            return mostRecentLoggedInSteamUser;
        }

        /// <summary>
        ///     Gets all Steam users from the loginusers.vdf file.
        /// </summary>
        /// <returns>
        ///     a list of users, or <see langword="null"/>, if no users exist or there was an error.
        /// </returns>
        public List<SteamUser>? GetSteamUsers()
        {
            string? steamPath = this.SteamPath;

            // SteamPath couldn't be fetched.
            // This would break if Steam was installed to a different directory between fetching and using the steam path.
            if (steamPath == null)
                return null;

            // The path that probably contains all users that have logged in since Steam was installed
            string usersFilePath = Path.Combine(steamPath, "config", "loginusers.vdf");

            if (!File.Exists(usersFilePath))
                // Where file? 🦧
                return null;

            VDFFile vdf = new VDFFile(usersFilePath);

            var users = vdf?["users"]?.Children;

            if (users == null)
                return null;

            List<SteamUser>? steamUsers = null;

            // users may be empty here
            foreach (var user in users)
            {
                // Create the list if we have at least one *potential* user
                if (steamUsers == null)
                    steamUsers = new List<SteamUser>();

                var steamUser = new SteamUser();

                // This is not the user name, but the name of the VDF element, which in this case should be the steam ID 64
                if (ulong.TryParse(user.Name, out ulong steamID64))
                {
                    steamUser.SteamID64 = steamID64;
                }

                steamUser.AccountName = user["AccountName"].Value;
                steamUser.PersonaName = user["PersonaName"].Value;

                // "MostRecent" can later be found by getting the largest Timestamp
                if (ulong.TryParse(user["Timestamp"].Value, out ulong lastLoginUnixTime))
                {
                    steamUser.LastLogin = lastLoginUnixTime;
                }

                // The needed AccountID (Last part of the SteamId3) is calculated automatically from the SteamId64
                steamUser.AbsoluteUserdataFolderPath = Path.Combine(steamPath, "userdata", steamUser.AccountID.ToString());

                steamUsers.Add(steamUser);
            }

            return steamUsers;
        }

        /// <summary>
        ///     Starts the given steam game, with the given additional arguments, if possible.
        /// </summary>
        /// <param name="gameID">The ID of the game (e.g. 730 for CS:GO/CS2).</param>
        /// <param name="additionalArgs">
        ///     The arguments passed to that game.
        ///     Note, that the default arguments set by the user in the UI are also passed to the app,
        ///     these are just additional to that.
        /// </param>
        public void StartSteamApp(int gameID, string additionalArgs)
        {
            string? steamPath = this.SteamPath;
            this.UpdateInstalledGames(); // Won't force update, if already set

            SteamGame? gameToStart = this.InstalledGames?.FirstOrDefault(game => game.AppId == gameID);

            if (steamPath == null || gameToStart == null)
                return;

            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false; // Make double sure
            startInfo.CreateNoWindow = false;
            startInfo.FileName = Path.Combine(steamPath, "steam.exe");

            string extraArgs = String.IsNullOrWhiteSpace(additionalArgs) ? string.Empty : $" {additionalArgs}";

            // The "-applaunch" argument will automatically add the args stored by the user. We add our own ones to that, if required.
            startInfo.Arguments = $"-applaunch {gameID}" + extraArgs;

            // Fire and forget!
            Process.Start(startInfo);
        }

        #region Private Methods
        /// <summary>
        ///     Checks, if the file at the given absolute path is considered an appmanifest, by the looks of it.
        /// </summary>
        /// <remarks>
        ///     App manifest have the format "appmanifest_GAMEID.acf"
        /// </remarks>
        /// <param name="filePath">The absolute path of the app manifest (acf) file.</param>
        /// <returns>
        ///     whether the file name matches the app manifest description.
        /// </returns>
        private bool isAppManifestFile(string filePath)
        {
            string[] splitFilePath = filePath.Split(new[] { '\\', '/' });
            
            if (splitFilePath.Length < 1)
                // Doesn't seem to be a valid path
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(splitFilePath.Last(), "appmanifest_\\d+.acf"); ;
        }

        /// <summary>
        ///     Converts a unix time in seconds to a <see cref="DateTime"/> using the specified <see cref="DateTimeKind"/>.
        /// </summary>
        /// <param name="unixSeconds">The unix seconds.</param>
        /// <param name="dateTimeKind">The type of time zone, UTC by default.</param>
        /// <returns>
        ///     the <see cref="DateTime"/> that was created from the given seconds.
        /// </returns>
        private DateTime fromUnixFormat(long unixSeconds, DateTimeKind dateTimeKind = DateTimeKind.Utc)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, dateTimeKind);
            return dateTime.AddSeconds(unixSeconds);
        }

        /// <summary>
        ///     Takes a game object and populates it with the info from the app manifest file,
        ///     which is specified by the given VDF element.
        /// </summary>
        /// <param name="game">The game to populate with info.</param>
        /// <param name="appStateVdf">The app state VDF element, which contains the required information.</param>
        /// <param name="steamLibraryPath">The absolute path to the steam library containing this game.</param>
        private void populateGameInfo(SteamGame game, Element appStateVdf, string steamLibraryPath)
        {
            game.Name = appStateVdf["name"]?.Value;

            // Setting these two properties enables the ability to fetch the FullInstallPath
            game.InstallFolderName = appStateVdf["installdir"]?.Value;
            game.LinkedSteamLibraryPath = steamLibraryPath;

            if (int.TryParse(appStateVdf["appid"]?.Value, out int appId))
            {
                game.AppId = appId;
            }

            if (int.TryParse(appStateVdf["StateFlags"]?.Value, out int stateFlags))
            {
                game.GameState = stateFlags;
            }

            if (long.TryParse(appStateVdf["LastUpdated"]?.Value, out long lastUpdated))
            {
                // It's unix time, but the time is in the local time zone, and not in UTC.
                game.LastUpdated = fromUnixFormat(lastUpdated, DateTimeKind.Local);
            }

            if (long.TryParse(appStateVdf["LastOwner"]?.Value, out long lastOwner))
            {
                game.LastOwnerSteam64Id = lastOwner;
            }

            if (long.TryParse(appStateVdf["BytesToDownload"]?.Value, out long bytesToDownload))
            {
                game.BytesToDownload = bytesToDownload;
            }

            if (long.TryParse(appStateVdf["BytesDownloaded"]?.Value, out long bytesDownloaded))
            {
                game.BytesDownloaded = bytesDownloaded;
            }

            if (long.TryParse(appStateVdf["BytesToStage"]?.Value, out long bytesToStage))
            {
                game.BytesToStage = bytesToStage;
            }

            if (long.TryParse(appStateVdf["BytesStaged"]?.Value, out long bytesStaged))
            {
                game.BytesStaged = bytesStaged;
            }

            game.KeepAutomaticallyUpdated = appStateVdf["AutoUpdateBehavior"]?.Value != "0";

            game.AllowOtherUpdatesWhileRunning = appStateVdf["AllowOtherDownloadsWhileRunning"]?.Value != "0";
        }
        #endregion
    }
}
