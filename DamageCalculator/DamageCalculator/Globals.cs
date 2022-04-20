using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Damage_Calculator
{
    public static class Globals
    {
        public static Settings Settings { get; set; } = new Settings();

        public static void LoadSettings()
        {
            // Get path
            string pathToFile = System.IO.Path.Combine(MainWindow.FilesPath, Settings.SettingsFileName);

            if (!System.IO.File.Exists(pathToFile))
            {
                Globals.SaveSettings();
                return;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (var fs = new System.IO.FileStream(pathToFile, System.IO.FileMode.Open))
            {
                Globals.Settings = (Settings)serializer.Deserialize(fs);
            }
        }

        public static void SaveSettings()
        {
            // Get path
            string pathToFile = System.IO.Path.Combine(MainWindow.FilesPath, Settings.SettingsFileName);

            if (!System.IO.Directory.Exists(MainWindow.FilesPath))
                // Make sure the folder exists before attempting to save the file
                System.IO.Directory.CreateDirectory(MainWindow.FilesPath);

            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (var fs = new System.IO.FileStream(pathToFile, System.IO.FileMode.Create))
            {
                serializer.Serialize(fs, Globals.Settings);
            }
        }
    }
}
