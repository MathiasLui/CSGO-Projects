using SteamShared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SteamShared
{
    /// <summary>
    /// State when first connecting.
    /// </summary>
    public enum CsgoSocketConnectResult { Success, WrongPassword, Failure }

    /// <summary>
    /// State when checking if a connection is still up.
    /// </summary>
    public enum SocketConnectionState { Connected, Disconnected, Blocking }

    public class CsgoSocketConnection : IDisposable
    {
        private Socket? socket = null;
        private DateTime lastCommandExecute = DateTime.MinValue;
        private readonly TimeSpan commandTimeout = TimeSpan.FromSeconds(1); // Actual timeout is a bit lower

        public event EventHandler OnDisconnect;
        
        public bool IsConnecting { get; private set; }

        public async Task<CsgoSocketConnectResult> ConnectAsync(ushort port, string? password)
        {
            bool usePassword = !String.IsNullOrWhiteSpace(password);
            if (socket == null)
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                IAsyncResult connectResult = socket.BeginConnect("localhost", port, null, null); // We wanna wait for the connection

                // Wait for timeout before closing connection
                this.IsConnecting = true;
                bool success = connectResult.AsyncWaitHandle.WaitOne(10_000, exitContext: true);
                this.IsConnecting = false;

                if (this.socket.Connected)
                {
                    this.socket.EndConnect(connectResult);
                }
                else
                {
                    // We couldn't connect in the given time
                    this.socket.Close();
                    return CsgoSocketConnectResult.Failure;
                }
            }
            catch (Exception e)
            {
                return CsgoSocketConnectResult.Failure;
            }

            if (usePassword)
            {
                // A password was specified (not by us) so we gotta deal with this shit
                await this.executeCommand("PASS " + password, 0); // Actual bytes might be for example 2
            }

            return CsgoSocketConnectResult.Success;
        }

        public async Task DisconnectAsync()
        {
            if (socket == null)
                return;

            await this.socket.DisconnectAsync(false);
            this.OnDisconnect(this, null!);
            this.socket = null;
        }

        private string stripTrailingNullBytes(string text)
        {
            string result = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == 0)
                {
                    return result;
                }
                result += text[i];
            }
            return result;
        }

        public async Task<string?> GetMapName()
        {
            string? response = await this.executeCommand("host_map", 2048);

            if (response == null)
                return null;

            // Example response: "host_map" = "de_tuscan.bsp" ( def. "" )       
            // This can be changed by the user, regardless of sv_cheats, at least on their own server
            // By default it will result to the map's file name that's been loaded

            int indexStart = response.LastIndexOf("host_map");

            if (indexStart < 0)
                return null;

            response = response.Substring(indexStart); // Here our output starts

            var valuesSplit = response.Split('=');

            if (valuesSplit.Length < 2)
                return null;

            var foundArgument = Regex.Match(valuesSplit[1], Globals.ArgumentPattern, RegexOptions.IgnoreCase);

            if (!foundArgument.Success)
                return null;

            // Only take everything until the last dot, to erase the file name, if existent
            int indexOfLastDot = foundArgument.Value.LastIndexOf('.');
            string mapName = foundArgument.Value.Substring(0, indexOfLastDot < 0 ? foundArgument.Value.Length : indexOfLastDot);

            return mapName;
        }

        public async Task<Vector3?> GetPlayerPosition()
        {
            string? response = await this.executeCommand("getpos_exact", 2048, isCheat: true);

            if (response == null)
                return null;

            // Example response: setpos_exact 0.000000 0.000000 0.000000;setang_exact 0.000000 0.000000 0.000000
            // It might only send everything until the first ; is hit, but we don't want the angles here anyways.
            // If we later also want the orientation then one could maybe execute Receive() twice
            // It might also contain text before it, so we wanna find the beginning of the output first

            int indexStart = response.LastIndexOf("setpos_exact");

            if (indexStart < 0)
                return null;

            response = response.Substring(indexStart); // Here our output starts

            var valuesSplit = response.Split(';');

            if (valuesSplit.Length < 2)
                return null;

            // Get position
            var positionSplit = valuesSplit[0].Split(' ');

            if(positionSplit.Length != 4)
                return null;

            var pos = new Vector3();
            if(float.TryParse(positionSplit[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x ))
            {
                pos.X = x;
            }
            if (float.TryParse(positionSplit[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                pos.Y = y;
            }
            if (float.TryParse(positionSplit[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                pos.Z = z;
            }

            return pos;
        }

        private async Task<string?> executeCommand(string message, int expectedBytes, bool isCheat = false, bool isRepeat = false)
        {
            if (Globals.Settings.CsgoHelper.GetRunningCsgo().Item1 == null)
            {
                // CS:GO isn't running (anymore)
                await this.DisconnectAsync();
                return null;
            }

            if (socket == null)
                return null;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\r\n");

            Debug.WriteLine("command is: " + message);

            bool repeatCommand = false;
            string answer = string.Empty;

            TimeSpan timeSinceLastCommand = DateTime.Now - lastCommandExecute;
            if (timeSinceLastCommand < commandTimeout)
            {
                Debug.WriteLine("timeout. sleeping for " + (commandTimeout - timeSinceLastCommand).Milliseconds + "ms");
                // Sleep for the remaining timeout
                await Task.Delay(commandTimeout - timeSinceLastCommand);
            }

            try
            {
                Debug.WriteLine("Sending actual command...");
                // Send the actual command
                await socket.SendAsync(messageBytes, SocketFlags.None);
            }
            catch { return null; }

            // Save this for the CS:GO ConCommand timeout
            this.lastCommandExecute = DateTime.Now;

            byte[] buffer = new byte[expectedBytes];
            socket.ReceiveTimeout = 1000; // 1 second

            int parts = 0;
            answer = string.Empty;
            try
            {
                Debug.WriteLine("Receiving actual command...");

                do
                {
                    int receivedBytes = this.socket.Receive(buffer, SocketFlags.None);
                    parts++;

                    string partAnswer = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    
                    Debug.WriteLine($"Received response (Part {parts}): \n" + partAnswer);

                    answer += partAnswer;

                    // We will most likely not get all of the response in one go, so we'll read until we get a completely empty response and concat everything
                } while (buffer[0] != 0);
            }
            catch 
            {
                if (string.IsNullOrEmpty(answer))
                    return null; 
            }

            Debug.WriteLine($"Complete response in {parts} parts:\n" + answer);

            if (isCheat && !isRepeat)
            {
                repeatCommand = this.cheatCommandNeedsElevation(answer);

                if (repeatCommand)
                {
                    // The game told us we used a cheat command and need to have sv_cheats set to 1
                    Debug.WriteLine("Trying to execute sv_cheats 1...");
                    await this.executeCommand("sv_cheats 1", 0);
                    Debug.WriteLine("Executed sv_cheats 1. Now re-executing previous command...");
                    return await this.executeCommand(message, expectedBytes, isCheat, isRepeat: true);
                }
            }

            return answer;
        }

        private bool cheatCommandNeedsElevation(string command)
        {
            return command.Contains("Can't use cheat command");
        }
        
        private SocketConnectionState checkConnected()
        {
            if (socket == null)
                return SocketConnectionState.Disconnected;

            var connectionState = SocketConnectionState.Disconnected;

            bool blockingState = socket.Blocking;
            try
            {
                byte[] tmp = new byte[1];

                socket.Blocking = false;
                socket.Send(tmp, 0, 0);

                connectionState = SocketConnectionState.Connected;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                {
                    // Still Connected, but the Send would block
                    connectionState = SocketConnectionState.Blocking;
                }
                else
                {
                    connectionState = SocketConnectionState.Disconnected;
                }
            }
            finally
            {
                socket.Blocking = blockingState;
            }

            return connectionState;
        }

        public void Dispose()
        {
            this.socket?.Dispose();
        }
    }
}
