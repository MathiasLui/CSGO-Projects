using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamShared.ZatVdfParser
{
    public class VDFFile
    {
        #region VARIABLES
        private Regex regNested = new Regex(@"(\"")?([a-zA-Z0-9]*?)(\"")?");
        private Regex regValuePair = new Regex(@"\""(.*?)\""\s*\""(.*?)\""");
        #endregion

        #region PROPERTIES
        public List<Element> RootElements { get; set; }
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="filePathOrText">The path to the file, or the text to be parsed.</param>
        /// <param name="parseTextDirectly">Whether the given parameter is a file path or the actual string to be parsed.</param>
        public VDFFile(string filePathOrText, bool parseTextDirectly = false)
        {
            RootElements = new List<Element>();
            Parse(filePathOrText, parseTextDirectly);
        }
        #endregion

        #region METHODS
        public string ToVDF()
        {
            StringBuilder builder = new StringBuilder();
            foreach (Element child in RootElements)
                builder.Append(child.ToVDF());
            return builder.ToString();
        }
        private void Parse(string filePathOrText, bool parseTextDirectly)
        {
            if (!parseTextDirectly && !File.Exists(filePathOrText))
                return;

            Element? currentLevel = null;

            // Generate stream from string in case we want to read it directly, instead of using a file stream (boolean parameter)
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(filePathOrText);
            writer.Flush();
            stream.Position = 0;

            using (StreamReader reader = parseTextDirectly ? new StreamReader(stream) : new StreamReader(filePathOrText))
            {
                string? line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("\0", StringComparison.Ordinal))
                        return;

                    line = line.Trim();
                    // We don't want to split if " is escaped with \
                    // If " is preceeded by an even number of \, it will get split
                    string[] parts = splitEscaped(line, '"', '\\');

                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    if (regValuePair.Match(line).Success)
                    {
                        Element subElement = new Element();
                        subElement.Name = parts[1];
                        subElement.Value = parts[3];
                        subElement.Parent = currentLevel;
                        if (currentLevel == null)
                            RootElements.Add(subElement);
                        else
                            currentLevel.Children.Add(subElement);
                    }
                    else if (regNested.Match(line).Success && !String.IsNullOrEmpty(line) && line != "{" && line != "}")
                    {
                        Element nestedElement = new Element();
                        if(parts.Length == 3)
                            nestedElement.Name = parts[1];
                        else
                            nestedElement.Name = parts[0];
                        nestedElement.Parent = currentLevel;
                        if (currentLevel == null)
                            RootElements.Add(nestedElement);
                        else
                            currentLevel.Children.Add(nestedElement);
                        currentLevel = nestedElement;
                    }
                    else if (line == "}")
                    {
                        currentLevel = currentLevel!.Parent;
                    }
                    /*else if (line == "{")
                    {
                        //Nothing to do here
                    }*/
                }
            }
        }
        #endregion

        #region Private methods

        private string[] splitEscaped(string text, char delimiter, char escapeCharacter)
        {
            // Example text with delimiter " and escape character \ would be:
            // "MyPassword" "My password is \"1234\""
            // ^          ^ ^                       ^   << Splits are marked

            List<string> splitStrings = new List<string>();

            bool escaped = false;
            string currentSection = string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == delimiter)
                {
                    if (!escaped)
                    {
                        splitStrings.Add(currentSection);
                        currentSection = string.Empty;

                        if (i + 1 == text.Length)
                            // The last char in the string is a delimiter, so add that section now, because we won't iterate over it later
                            splitStrings.Add(currentSection);

                        continue;
                    }

                    escaped = false;
                }

                if (text[i] == escapeCharacter)
                    escaped = !escaped;

                currentSection += text[i];

                if (i + 1 == text.Length)
                {
                    // This was the last character, but wasn't a delimiter
                    splitStrings.Add(currentSection);
                }
            }

            return splitStrings.ToArray();
        }

        #endregion

        #region OPERATORS
        public Element this[int key]
        {
            get
            {
                return RootElements[key];
            }
        }
        public Element? this[string key]
        {
            get
            {
                return RootElements.FirstOrDefault(x => x.Name == key);
            }
        }
        #endregion
    }
}
