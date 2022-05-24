using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.SourceConfig
{
    public class SourceCFG
    {
        /// <summary>
        /// Gets or sets a list of concommands or convars in this CFG-file.
        /// </summary>
        public List<SourceCFGCommand>? Commands { get; set; }

        public static SourceCFG? FromFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var config = new SourceCFG();
            var commands = new List<SourceCFGCommand>();

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Position = 0;

                int curByte;
                int nextByte = 0x20; // Placeholder value
                char curChar = ' ';
                char nextChar;

                bool wasInQuoted = false;
                bool inQuoted = false;
                bool inComment = false;
                bool cmdNameParsed = false;
                bool endOfFile = false;

                while (true)
                {
                    // Start of the file and every new command or line (after line break or unquoted semicolon)
                    var cmd = new SourceCFGCommand();
                    StringBuilder cmdString = new StringBuilder();
                    StringBuilder valueString = new StringBuilder();

                    if (endOfFile)
                        // end of stream
                        break;

                    while (true)
                    {
                        // in this loop breaking means going into next line or next command
                        // continuing means going to next character
                        // READ NEXT CHAR

                        curByte = nextByte < 0 ? -1 : fs.ReadByte();

                        try
                        {
                            curChar = Convert.ToChar(curByte);
                        }
                        catch
                        { }

                        nextByte = fs.ReadByte();

                        // Since we just sneakily peeked a byte, move it back a notch
                        if (fs.Position > 0)
                            fs.Position--;
                        else
                        {
                            // File is probably empty, because the first ReadByte didn't advance the stream position
                            endOfFile = true;
                            break;
                        }

                        try
                        {
                            nextChar = Convert.ToChar(nextByte);
                        }
                        catch
                        {
                            // Probably end of file after this one (AFAIK mostly negative numbers can't be parsed so it should be end of stream)
                            nextChar = ' ';
                        }

                        // PARSE NEXT CHAR... man I could've used regexes or some shit but nvm

                        if (inComment && !isEndOfLine(curChar, nextChar))
                            // Ends of lines come before this because those are the only ones that cannot be commented out
                            continue;

                        if (curByte < 1 || curChar == '\"' || (curChar == ';' || char.IsWhiteSpace(curChar) && !inQuoted) || isEndOfLine(curChar, nextChar))
                        {
                            // Newlines are also whitespace
                            // We've reached end of line, a gap between commands or just padding, a quote or the end of command, always break at newlines or semis

                            // We might be at the first unquoted space after a command as well, this would mean... oh yeah! we finally finished fetching that command name!
                            cmdNameParsed = true;

                            wasInQuoted = false;
                            if (curChar == '\"')
                            {
                                // Toggle quoted and continue with next character
                                wasInQuoted = inQuoted;
                                inQuoted = !inQuoted;
                            }

                            // Create and save current value, if quoted or not empty
                            if (curByte < 1 || wasInQuoted || !string.IsNullOrWhiteSpace(valueString.ToString()))
                            {
                                if (cmd.CommandValues == null)
                                    // Only create it if there are values, otherwise it will be null, for example when executing commands that have no values
                                    cmd.CommandValues = new List<SourceCFGCommandValue>();

                                var newVal = new SourceCFGCommandValue();
                                newVal.Value = valueString.ToString();
                                cmd.CommandValues.Add(newVal);
                                valueString.Clear();

                                if (curByte < 1)
                                {
                                    // end of stream or null byte
                                    endOfFile = true;
                                    break;
                                }
                            }

                            if (curChar == ';' || isEndOfLine(curChar, nextChar))
                            {
                                if (isEndOfLine(curChar, nextChar) && curChar == '\r')
                                    // Windows-Style line breaks need an extra character, so skip that one
                                    fs.Position++;

                                // is line break or unquoted semicolon so go to next line,
                                // if it was just a different whitespace, leave it be
                                break;
                            }

                            // oh it was a different white space or a quote... so just skip it ig lmao
                            continue;
                        }

                        if (!inQuoted && curChar == '/' && nextChar == '/')
                        {
                            // We've encountered the beginning of a comment (double slashes inside of quoted values are allowed I guess)
                            inComment = true;
                            continue;
                        }

                        if (cmdNameParsed)
                            // We already have the command or convar, so just assume everything else is a value
                            valueString.Append(curChar);
                        else
                            // We still at the beginning of the line bro, awesome!
                            cmdString.Append(curChar);
                    }

                    // Here is the end of line or command separated by newlines or semicolons (It's late and I've misspelt this thrice)
                    // Just save the command name, the values should've been added already in zhe process of life Bruder
                    cmd.CommandName = cmdString.ToString();
                    cmdString.Clear(); // Prepare for next line, make it tidy and clean like a baby's bottom :)

                    if (!string.IsNullOrWhiteSpace(cmd.CommandName)) 
                    {
                        commands.Add(cmd);
                    }
                    
                    inQuoted = false; // In case quotes were forgotten but end of line has been reached, just make sure. In the ideal case this should already be false
                    inComment = false; // We don't want to ignore everything in the file, only everything in that line 
                    cmdNameParsed = false; // Next line will probably have another command so prepare dinner here ;)
                }
            }

            config.Commands = commands;
            return config;
        }

        private static bool isEndOfLine(char curChar, char nextChar)
        {
            return curChar == '\n' || (curChar == '\r' && nextChar == '\n');
        }
    }
}
