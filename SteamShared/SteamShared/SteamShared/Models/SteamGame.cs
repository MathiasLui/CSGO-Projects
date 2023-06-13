using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    /// <summary>
    ///     A game (or app) which contains mainly app manifest data.
    /// </summary>
    public class SteamGame
    {
        /// <summary>
        ///     The name of this game.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        ///     The name of the folder this game's files are installed in,
        ///     e.g. for CS:GO it would be "Counter-Strike Global Offensive".
        /// </summary>
        public string? InstallFolderName { get; set; }

        /// <summary>
        ///     The absolute path of the steam library, which contains this game.
        /// </summary>
        public string? LinkedSteamLibraryPath { get; set; }

        /// <summary>
        ///     The absolute installation path of this game, including the <see cref="InstallFolderName"/>,
        ///     or <see langword="null"/>, if a puzzle piece of it is not specified.
        /// </summary>
        /// <remarks>
        ///     The path might look like this on Windows:               <br/><br/>
        ///     
        ///     C:\My\Library\Path\steamapps\common\Super Cool Game     <br/>
        ///     [  Library Path  ] [ Xtra Folders ] [  Game Name  ]
        /// </remarks>
        public string? FullInstallPath
        { 
            get 
            {
                if (this.LinkedSteamLibraryPath is null || this.InstallFolderName is null)
                    return null;

                return System.IO.Path.Combine(LinkedSteamLibraryPath, "steamapps", "common", InstallFolderName);
            } 
        }

        /// <summary>
        ///     Whether the game's directory (<see cref="FullInstallPath"/>) exists.
        /// </summary>
        /// <remarks>
        ///     <see cref="LinkedSteamLibraryPath"/> and <see cref="InstallFolderName"/> need to be set.
        /// </remarks>
        public bool GameFolderExists
        {
            get
            {
                return Directory.Exists(this.FullInstallPath);
            }
        }

        /// <summary>
        ///     The ID of this Steam App, e.g. for CS:GO this would be 730.
        /// </summary>
        public int AppId { get; set; }

        /// <summary>
        ///     The flags of this game, defined in <see cref="SteamShared.Models.GameState"/>.
        ///     Note, that these are *flags* and thus can have multiple values.
        /// </summary>
        public int GameState { get; set; }

        /// <summary>
        ///     The time this game was last updated at.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        ///     The SteamID64 of the last owner user of this game.
        /// </summary>
        public long LastOwnerSteam64Id { get; set; }

        /// <summary>
        ///     The amount of bytes that are left to download.
        /// </summary>
        public long BytesToDownload { get; set; }

        /// <summary>
        ///     The amount of bytes that were already downloaded.
        /// </summary>
        public long BytesDownloaded { get; set; }

        /// <summary>
        ///     The amount of bytes that are left to be staged (Not 100% sure, what staging does).
        /// </summary>
        public long BytesToStage { get; set; }

        /// <summary>
        ///     The amount of bytes that were alread staged.
        /// </summary>
        public long BytesStaged { get; set; }

        /// <summary>
        ///     Whether Steam should keep this game updated automatically.
        /// </summary>
        public bool KeepAutomaticallyUpdated { get; set; }

        /// <summary>
        ///     Whether Steam is allowed to update other games and apps while this app is running.
        /// </summary>
        public bool AllowOtherUpdatesWhileRunning { get; set; }
    }

    /// <summary>
    ///     The GameState flags defined in the app manifests.
    /// </summary>
    [Flags]
    enum GameState
    {
        StateInvalid = 0,
        StateUninstalled = 1,
        StateUpdateRequired = 2,
        StateFullyInstalled = 4,
        StateEncrypted = 8,
        StateLocked = 16,
        StateFilesMissing = 32,
        StateAppRunning = 64,
        StateFilesCorrupt = 128,
        StateUpdateRunning = 256,
        StateUpdatePaused = 512,
        StateUpdateStarted = 1024,
        StateUninstalling = 2048,
        StateBackupRunning = 4096,
        StateReconfiguring = 65536,
        StateValidating = 131072,
        StateAddingFiles = 262144,
        StatePreallocating = 524288,
        StateDownloading = 1048576,
        StateStaging = 2097152,
        StateCommitting = 4194304,
        StateUpdateStopping = 8388608
    }
}
