﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Damage_Calculator.ZatVdfParser
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
        public VDFFile(string filePath)
        {
            RootElements = new List<Element>();
            Parse(filePath);
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
        private void Parse(string filePath)
        {
            Element currentLevel = null;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("\0"))
                        return;

                    line = line.Trim();
                    string[] parts = line.Split('"');

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
                        currentLevel = currentLevel.Parent;
                    }
                    /*else if (line == "{")
                    {
                        //Nothing to do here
                    }*/
                }
            }
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
        public Element this[string key]
        {
            get
            {
                return RootElements.FirstOrDefault(x => x.Name == key);
            }
        }
        #endregion
    }
}
