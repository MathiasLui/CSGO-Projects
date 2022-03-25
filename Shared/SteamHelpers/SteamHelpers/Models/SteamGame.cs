using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class SteamGame
    {
        public string? Name { get; set; }

        public string? InstallFolderName { get; set; }

        public int AppId { get; set; }

        public int GameState { get; set; }

        public DateTime LastUpdated { get; set; }

        public long LastOwnerSteam64Id { get; set; }

        public long BytesToDownload { get; set; }

        public long BytesDownloaded { get; set; }

        public long BytesToStage { get; set; }

        public long BytesStaged { get; set; }

        public bool KeepAutomaticallyUpdated { get; set; }

        public bool AllowOtherUpdatesWhileRunning { get; set; }
    }

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
