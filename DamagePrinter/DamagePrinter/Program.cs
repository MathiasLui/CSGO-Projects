using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using SteamShared;
using LoreSoft.MathExpressions;
using System.Net.Http;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

static class Program
{
    static readonly uint WM_COPYDATA = 0x004A;
    static SteamHelper steamHelper = new SteamHelper();
    static MathEvaluator mathEval = new MathEvaluator();
    static HttpClient httpClient = new HttpClient();

    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int SendMessage(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);

    [STAThread]
    static void Main(string[] args)
    {
        string? gamePath = steamHelper.GetGamePathFromExactName("Counter-Strike: Global Offensive");

        if (gamePath == null)
            return;

        string consoleLogPath = Path.Combine(gamePath, "csgo", "console.log");
        bool consoleLogExists = false;

        long oldFileSize = 0;
        try
        {
            oldFileSize = new FileInfo(consoleLogPath).Length;
        } catch { }

        long nextLineOffset = oldFileSize; // bytes from the start of the file, by default not to read anything
        int minDamageToCount = 20;
        bool initialScan = true;
        var prevTaggedPlayers = new List<Tuple<string, int, int>>();
        DateTime lastFuelRequestTime = DateTime.MinValue;

        if (FindWindow("Valve001", null) == IntPtr.Zero)
            File.Delete(consoleLogPath);

        // here we start fresh at line 0 cause it got deleted
        while (true)
        {
            if (File.Exists(consoleLogPath))
            {
                bool update = false;

                if (!consoleLogExists && initialScan) {
                    consoleLogExists = true;
                    initialScan = false;
                }

                long curFileSize = new FileInfo(consoleLogPath).Length;
                if (curFileSize != oldFileSize)
                {
                    update = true;
                    oldFileSize = curFileSize;
                }

                if (update)
                {
                    int damageTakenTotal = 0;

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
                                int nextByte = fs.ReadByte();

                                if (nextByte == -1 && line != "")
                                {
                                    lines.Add(line);
                                    endOfFile = true;
                                    break;
                                }

                                char nextChar = (char)nextByte;

                                if (nextChar == '\n' && line != "")
                                {
                                    lines.Add(line);
                                    endOfLine = true;
                                }
                                else
                                    if(nextChar != '\r')
                                        line += nextChar;
                            }
                        }

                        nextLineOffset = fs.Position;
                    }

                    var taggedPlayers = new List<Tuple<string, int, int>>();
                    string calcKeyWord = "!calc";
                    string fuelPriceKeyWord = "!fuel";
                    string weatherKeyWord = "!weather";

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

                                if(damage < 100 && damage > minDamageToCount && taggedPlayers.FirstOrDefault(player => player.Item1 == name) == null)
                                    // not in list yet so add
                                    taggedPlayers.Add(new Tuple<string, int, int>(name, damage, hits));
                            }
                        }
                        else if (line.ToLower().Contains(calcKeyWord + ' '))
                        {
                            // Calculate
                            string expression = line.Substring(line.IndexOf(calcKeyWord + ' ') + calcKeyWord.Length);
                            if (String.IsNullOrWhiteSpace(expression))
                                continue;

                            try
                            {
                                double res = mathEval.Evaluate(expression);
                                Thread.Sleep(700); // so the chat message is shown if we requested it ourselves
                                ExecuteCommands(true, $"say \"Answer: {res}\"");
                            }
                            catch { }
                        }
                        else if (line.ToLower().Contains(fuelPriceKeyWord))
                        {
                            if (DateTime.Now - lastFuelRequestTime < TimeSpan.FromMinutes(1))
                                continue;

                            string apiKey = "";
                            string nordOelID = "69ad1928-e972-421b-a33c-4319da73deaa";
                            string shellID = "a507dd35-4a7f-46d1-86b6-accc4769a47b";
                            string request = $"https://creativecommons.tankerkoenig.de/json/prices.php?ids={nordOelID},{shellID}&apikey={apiKey}";

                            HttpResponseMessage response;
                            try
                            {
                                response = httpClient.GetAsync(request).Result;

                                lastFuelRequestTime = DateTime.Now;
                            }
                            catch { continue; }

                            var responseString = response.Content.ReadAsStringAsync().Result;

                            dynamic? jsonResponse = JsonConvert.DeserializeObject(responseString);

                            if (jsonResponse == null || (bool)jsonResponse!.ok == false)
                                continue;

                            var cmds = new List<string>();

                            if((string)jsonResponse!.prices[nordOelID].status == "open")
                            {
                                cmds.Add($"say \"NORDOEL: Super: {(string)jsonResponse!.prices[nordOelID].e5} Euro, Diesel: {(string)jsonResponse!.prices[nordOelID].diesel} Euro\"");
                            }
                            else
                            {
                                cmds.Add("say \"NORDOEL: Geschlossen\"");
                            }

                            if ((string)jsonResponse.prices[shellID].status == "open")
                            {
                                cmds.Add($"say \"Shell: Super: {(string)jsonResponse!.prices[shellID].e5} Euro, Diesel: {(string)jsonResponse!.prices[shellID].diesel} Euro\"");
                            }
                            else
                            {
                                cmds.Add("say \"Shell: Geschlossen\"");
                            }

                            ExecuteCommands(true, cmds.ToArray());
                        }
                        else if (line.ToLower().Contains(weatherKeyWord))
                        {
                            string request = $"https://api.open-meteo.com/v1/forecast?latitude=54.2335&longitude=10.3397&hourly=temperature_2m,cloudcover&daily=precipitation_hours&timezone=Europe%2FBerlin";

                            HttpResponseMessage response;
                            try
                            {
                                response = httpClient.GetAsync(request).Result;
                            }
                            catch { continue; }

                            var responseString = response.Content.ReadAsStringAsync().Result;

                            dynamic? jsonResponse = JsonConvert.DeserializeObject(responseString);

                            if (jsonResponse == null)
                                continue;

                            string cmd = $"say \"{((double)jsonResponse!.hourly.temperature_2m[0]).ToString(System.Globalization.CultureInfo.InvariantCulture)} C, Wolkendecke {(double)jsonResponse!.hourly.cloudcover[0]} %, Stunden Regen: {Math.Round((double)jsonResponse!.daily.precipitation_hours[0] * 100 / 24)} %\"";

                            ExecuteCommands(true, cmd);
                        }
                    }

                    bool didPlayerDie = damageTakenTotal >= 100;

                    if (!didPlayerDie)
                    {
                        continue;
                    }
                    
                    if (areListsEqual(taggedPlayers, prevTaggedPlayers))
                    {
                        // Last is the same as previous, likely dealt damage, died and now the round ended and it was printed again
                        Console.WriteLine("Double console output.");
                        continue;
                    }

                    prevTaggedPlayers = taggedPlayers;

                    if (taggedPlayers.Count < 1)
                        continue;

                    string[] commands = new string[taggedPlayers.Count];

                    // We didn't have a round end, but got killed
                    for (int i = 0; i < commands.Length; i++)
                    {
                        commands[i] += "say_team \"";
                        if (taggedPlayers[i].Item2 > 90)
                        {
                            // One-shot
                            commands[i] += $"{taggedPlayers[i].Item1} is one-shot {taggedPlayers[i].Item2}";
                        }
                        else if (taggedPlayers[i].Item2 >= 70)
                        {
                            // Lit
                            commands[i] += $"{taggedPlayers[i].Item1} is lit for {taggedPlayers[i].Item2}";
                        }
                        else
                        {
                            // Tagged
                            commands[i] += $"{taggedPlayers[i].Item1} is tagged for {taggedPlayers[i].Item2}";
                        }

                        commands[i] += "\"";
                    }

                    // Notify players
                    ExecuteCommands(false, commands);
                }
            }
            Thread.Sleep(50);
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

    static bool ExecuteCommands(bool triggeredByCommand, params string[] cmds)
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
            data.cbData = (uint)cmds[i].Length + 1;
            data.lpData = cmds[i];

            if (triggeredByCommand)
                Thread.Sleep(chatTimeoutMs);

            // Allocate for data
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            Marshal.StructureToPtr(data, ptr, false);

            int ret = SendMessage(hWnd, WM_COPYDATA, IntPtr.Zero, ptr);

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
}

struct COPYDATASTRUCT
{
    public ulong dwData;
    public uint cbData;
    public string lpData;
}
