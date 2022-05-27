using SteamShared;
using SteamShared.SourceConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Xml.Serialization;

namespace DamagePrinterGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly uint WM_COPYDATA = 0x004A;

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll",  CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int SendMessageW(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Used for databinding.
        /// </summary>
        public Settings Settings { get; set; } = Globals.Settings;

        private static readonly string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string folderPath = System.IO.Path.Combine(myDocumentsPath, "CSGO Damage Printer");
        private static readonly string settingsFilePath = System.IO.Path.Combine(folderPath, "settings.xml");
        private static string consoleLogFileName = "console.log";
        private static string? consoleLogFolderPath = null;

        // Task cancellation
        private static CancellationTokenSource ts = new CancellationTokenSource();
        private static CancellationToken ct = ts.Token;

        public MainWindow()
        {
            InitializeComponent();

            if (!this.ensureConsoleLogAndGamePath())
            {
                MessageBox.Show("The console log could not be created.\n\nAs a workaround, try to create an autoexec config and adding the line 'con_logfile console.log' to it.", "Unknown setup error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            Task.Run(mainLoop, ct);
        }

        private void mainLoop()
        {
            string consoleLogPath = System.IO.Path.Combine(consoleLogFolderPath ?? string.Empty, consoleLogFileName);

            var prevTaggedPlayers = new List<Tuple<string, int, int>>();

            // here we start fresh at line 0 cause it got deleted
            while (true)
            {
                if (ct.IsCancellationRequested)
                    break;

                this.Dispatcher.Invoke(() => this.lblConsoleLogFound.Text = "No");
                // Check for the window every now and then
                bool initialScan = true;
                bool consoleLogExists = false;

                long oldFileSize = 0;
                long nextLineOffset = 0;
                if (this.findCsgoWindow())
                {
                    this.Dispatcher.Invoke(() => this.lblCsgoWindowFound.Text = "Yes");
                    this.Dispatcher.Invoke(() => this.txtDamageOutput.AppendText("CS:GO Window found." + '\n'));
                    // We found the window so begin checking for the console
                    while (true)
                    {
                        if (ct.IsCancellationRequested)
                            break;

                        if (!findCsgoWindow())
                            break;

                        if (!File.Exists(consoleLogPath))
                            // Not yet
                            continue;

                        this.Dispatcher.Invoke(() => this.lblConsoleLogFound.Text = "Yes");

                        if (Globals.Settings == null)
                            break;

                        // Log exists

                        bool update = false;

                        if (!consoleLogExists && initialScan)
                        {
                            consoleLogExists = true;
                            initialScan = false;

                            oldFileSize = 0;
                            try
                            {
                                oldFileSize = new FileInfo(consoleLogPath).Length;
                            }
                            catch { }
                            nextLineOffset = oldFileSize; // bytes from the start of the file, by default not to read anything
                        }

                        long curFileSize = new FileInfo(consoleLogPath).Length;
                        if (curFileSize != oldFileSize)
                        {
                            update = true;
                            oldFileSize = curFileSize;
                        }

                        if (!update)
                            continue;
                        int damageTakenTotal = 0;

                        // Read in all the NEW lines of the console log
                        List<string> lines = new List<string>();
                        using (var fs = File.Open(consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Position = nextLineOffset;
                            bool endOfFile = false;

                            while (!endOfFile)
                            {
                                string line = "";
                                bool endOfLine = false;
                                while (!endOfLine)
                                {
                                    List<int> nextBytes = new List<int>();

                                    // 0x80 is 8th bit or 1<<7 (first character of multibyte)
                                    // as long as 0x80 (1<<7) and 0x40 (1<<6) (seventh bit) are both set, add it to current character
                                    int nextByte = fs.ReadByte();
                                    nextBytes.Add(nextByte);

                                    if (nextByte == -1 && line != "")
                                    {
                                        lines.Add(line);
                                        endOfFile = true;
                                        break;
                                    }

                                    bool charFinished = nextByte < 1 || (nextByte & 1 << 7) == 0;
                                    
                                    while (!charFinished)
                                    {
                                        nextByte = fs.ReadByte();
                                        if((nextByte & 1 << 7) != 0 && (nextByte & 1 << 6) == 0)
                                        {
                                            // Is next byte of multibyte char
                                            nextBytes.Add(nextByte);
                                            continue;
                                        }

                                        fs.Position--; // Move back one because we read prematurely
                                        charFinished = true;
                                    }

                                    
                                    char[] nextCharTry = Encoding.UTF8.GetChars(nextBytes.Select(b => (byte)b).ToArray());
                                    char nextChar = nextCharTry.Length > 0 && nextBytes.Count > 1 ? nextCharTry[0] : (char)nextByte;

                                    if (nextChar == '\n' && line != "")
                                    {
                                        lines.Add(line);
                                        endOfLine = true;
                                    }
                                    else
                                        if (nextChar != '\r')
                                            line += nextChar;
                                }
                            }

                            lines.ForEach(l => System.Diagnostics.Debug.WriteLine(l));

                            nextLineOffset = fs.Position;
                        }

                        var taggedPlayers = new List<Tuple<string, int, int>>();

                        foreach (string line in lines)
                        {
                            /*  -------------------------
                                Damage Given to "BOT Eugene" - 65 in 2 hits
                                -------------------------
                                Damage Taken from "BOT Eugene" - 117 in 4 hits*/
                            if (line.StartsWith("Damage Taken"))
                            {
                                Regex regexTaken = new Regex("Damage Taken from \"(.+?)\" - (\\d+) in \\d+ hits?");
                                Match takenMatch = regexTaken.Match(line);

                                if (takenMatch.Success)
                                {
                                    damageTakenTotal += int.Parse(takenMatch.Groups[2].Value);
                                }
                            }
                            else if (line.StartsWith("Damage Given"))
                            {
                                Regex regexTaken = new Regex("Damage Given to \"(.+?)\" - (\\d+) in (\\d+) hits?");
                                Match givenMatch = regexTaken.Match(line);

                                if (givenMatch.Success)
                                {
                                    string name = givenMatch.Groups[1].Value;
                                    int damage = int.Parse(givenMatch.Groups[2].Value);
                                    int hits = int.Parse(givenMatch.Groups[3].Value);

                                    if ((Globals.Settings.PrintDeadPlayers || damage < 100) && damage > Globals.Settings.MinimumDealtDamage
                                        && taggedPlayers.FirstOrDefault(player => player.Item1 == name) == null)
                                    {
                                        // not in list yet so add
                                        taggedPlayers.Add(new Tuple<string, int, int>(name, damage, hits));
                                    }
                                }
                            }
                        }

                        if (damageTakenTotal < Globals.Settings.MinimumReceivedDamage)
                        {
                            continue;
                        }

                        if (Globals.Settings.WithholdDuplicateConsoleOutputs && taggedPlayers.Count > 0 && areListsEqual(taggedPlayers, prevTaggedPlayers))
                        {
                            // Last is the same as previous, likely dealt damage, died and now the round ended and it was printed again
                            Console.WriteLine("Double console output.");
                            continue;
                        }

                        prevTaggedPlayers = taggedPlayers;

                        if (taggedPlayers.Count < 1)
                            continue;

                        string[] commands = new string[taggedPlayers.Count];
                        this.Dispatcher.Invoke(() => this.txtDamageOutput.AppendText("\n"));
                        // We had our minimum damage taken, so print the text
                        for (int i = 0; i < commands.Length; i++)
                        {
                            string textToAdd = string.Empty;
                            commands[i] += $"{(Globals.Settings.PrintTeamChat ? "say_team" : "say")} \"";

                            if (Globals.Settings.UseSpecificTerms)
                            {
                                if (taggedPlayers[i].Item2 >= 100)
                                {
                                    // Dead
                                    textToAdd = $"{taggedPlayers[i].Item1} is dead: {taggedPlayers[i].Item2}";
                                }
                                else if (taggedPlayers[i].Item2 > 90)
                                {
                                    // One-shot
                                    textToAdd = $"{taggedPlayers[i].Item1} is one-shot: {taggedPlayers[i].Item2}";
                                }
                                else if (taggedPlayers[i].Item2 >= 70)
                                {
                                    // Lit
                                    textToAdd = $"{taggedPlayers[i].Item1} is lit for {taggedPlayers[i].Item2}";
                                }
                                else
                                {
                                    // Tagged
                                    textToAdd = $"{taggedPlayers[i].Item1} is tagged for {taggedPlayers[i].Item2}";
                                }
                            }
                            else
                            {
                                textToAdd = $"{taggedPlayers[i].Item1} is hit for {taggedPlayers[i].Item2}";
                            }

                            if (Globals.Settings.PrintAmountOfShots)
                                textToAdd += $" in {taggedPlayers[i].Item3}";

                            commands[i] += textToAdd;

                            commands[i] += "\"";


                            this.Dispatcher.Invoke(() =>
                            {
                                if (this.txtDamageOutput.Text.Length > 10_000)
                                {
                                    // Too much text, so delete some
                                    this.txtDamageOutput.Text = this.txtDamageOutput.Text.Remove(0, this.txtDamageOutput.Text.IndexOf('\n', this.txtDamageOutput.Text.Length - 5_000) + 1);
                                }
                            });

                            // Print to local program "console"
                            if (!Globals.Settings.PrintIngameChat)
                                textToAdd = $"({textToAdd})";

                            this.Dispatcher.Invoke(() =>
                            {
                                this.txtDamageOutput.AppendText(textToAdd + '\n');
                            });
                        }

                        // Notify players in-game
                        if (Globals.Settings.PrintIngameChat)
                            ExecuteCommands(false, commands);

                        // End of each actual check
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    this.Dispatcher.Invoke(() => this.lblCsgoWindowFound.Text = "No");
                    // Ensure a smaller log size
                    File.Delete(consoleLogPath);
                }

                prevTaggedPlayers.Clear();
                // End of game-alive-check, check less often
                Thread.Sleep(2000);
            }
        }

        static bool areListsEqual(List<Tuple<string, int, int>> list1, List<Tuple<string, int, int>> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // still same length
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Item1 != list2[i].Item1)
                    return false;
                if (list1[i].Item2 != list2[i].Item2)
                    return false;
                if (list1[i].Item3 != list2[i].Item3)
                    return false;
            }

            return true;
        }

        static bool ExecuteCommands(bool triggeredByInGameCommand, params string[] cmds)
        {
            if (cmds == null)
                return false;

            IntPtr hWnd = FindWindow("Valve001", null!);

            if (hWnd == IntPtr.Zero)
                return false;

            int chatTimeoutMs = 700;
            int commandsHandled = 0;

            for (int i = 0; i < cmds.Length; i++)
            {
                if (cmds[i] == null)
                    continue;

                cmds[i] = cmds[i].Trim();

                COPYDATASTRUCT data;
                data.dwData = 0;
                data.cbData = (uint)System.Text.ASCIIEncoding.UTF8.GetByteCount(cmds[i]) + 1;// (uint)cmds[i].Length + 1;
                data.lpData = cmds[i];

                if (triggeredByInGameCommand)
                    Thread.Sleep(chatTimeoutMs);

                // Allocate for data
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, ptr, false);

                int ret = SendMessageW(hWnd, WM_COPYDATA, IntPtr.Zero, ptr);

                Console.WriteLine(cmds[i]);

                // Free data
                Marshal.FreeHGlobal(ptr);

                if (ret == 0)
                    commandsHandled++;

                if (cmds[i].StartsWith("say") || cmds[i].StartsWith("say_team"))
                    Thread.Sleep(chatTimeoutMs);
            }

            return cmds.Length > 0 && commandsHandled == cmds.Length;
        }

        private bool findCsgoWindow()
        {
            return FindWindow("Valve001", null!) != IntPtr.Zero;
        }

        private void saveSettings()
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (File.Exists(settingsFilePath))
                File.Delete(settingsFilePath);

            XmlSerializer serializer = new XmlSerializer(typeof(Settings));

            using (var fs = File.Open(settingsFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                serializer.Serialize(fs, Globals.Settings);
            }
        }

        private bool ensureConsoleLogAndGamePath()
        {
            // Get csgo path
            var csgoPath = new SteamHelper().GetGamePathFromExactName("Counter-Strike: Global Offensive");

            if (csgoPath == null)
            {
                this.lblCsgoFolderFound.Text = "No";
                return false;
            }
            else
            {
                this.lblCsgoFolderFound.Text = "Yes";
            }

            string gamePath = System.IO.Path.Combine(csgoPath, "csgo");
            string configsPath = System.IO.Path.Combine(gamePath, "cfg");
            consoleLogFolderPath = gamePath; // To use later
            string autoexecPath = System.IO.Path.Combine(configsPath, "autoexec.cfg");

            if (!File.Exists(autoexecPath))
            {
                // Create autoexec and enable console logging
                File.WriteAllText(autoexecPath, "con_logfile " + consoleLogFileName);
                ExecuteCommands(false, "exec autoexec");
                return true;
            }
            else
            {
                // First create a backup in case our code has a bug that fucks up the autoexec or something
                File.Delete(System.IO.Path.Combine(folderPath, "autoexec.backup"));
                File.Copy(autoexecPath, System.IO.Path.Combine(folderPath, "autoexec.backup"));

                // Check if autoexec has the command in it. If not, create it
                var autoexec = SourceCFG.FromFile(autoexecPath);

                if (autoexec == null)
                    return false;

                var foundCommand = autoexec.Commands?.FirstOrDefault(line => line.CommandName?.ToLower() == "con_logfile");
                if (foundCommand == null)
                {
                    // line not found so add it.
                    File.AppendAllText(autoexecPath, Environment.NewLine + "con_logfile " + consoleLogFileName);
                    ExecuteCommands(false, "exec autoexec");
                }
                else
                {
                    // user or we ourself added this command before, so just use the path specified here
                    string? newConsoleLogFileName = foundCommand.GetValuesAsOne();

                    if (newConsoleLogFileName != null)
                    {
                        // We now use the set one, which may differ, or may not
                        consoleLogFileName = newConsoleLogFileName;
                    }
                }

                return true;
            }
        }

        private void loadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));

                using (var fs = File.Open(settingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    Settings? settings = null;
                    try
                    {
                        settings = (Settings?)serializer.Deserialize(fs);
                    }
                    catch
                    {
                        MessageBox.Show("There was an error loading the settings in " + settingsFilePath + ".\n\nYour settings have been reset to default. If you get this more often please write me an email..Sorry for that :(", "Error loading settings", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (settings != null)
                        Globals.Settings.ApplySettingsFrom(settings);
                }
            }
        }

        #region events
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load settings
            this.loadSettings();
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save settings
            this.saveSettings();

            ts.Cancel();
        }
        #endregion
    }

    struct COPYDATASTRUCT
    {
        public ulong dwData;
        public uint cbData;
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string lpData;
    }
}
