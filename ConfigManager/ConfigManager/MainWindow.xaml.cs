using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Effects;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace ConfigManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string m_sProgramVersion = "1.0.2";
        static string m_sStartupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSGOConfigManager");
        string m_sStartupPathWithFileDefaultPath = System.IO.Path.Combine(m_sStartupPath, "DefaultPath.cfg");
        string m_sStartupPathWithFileFavourites = System.IO.Path.Combine(m_sStartupPath, "Favourites.cfg");
        string m_sStartupPathWithFileSettings = System.IO.Path.Combine(m_sStartupPath, "Settings.xml");
        string m_sAutoLibrary;
        string m_sSteamPath;
        bool m_bAutoDetectedLibrary = false;
        string[] m_sLibraries = null;
        int m_nAmountOfSteamLibraries;
        Settings m_Settings = new Settings();
        DispatcherTimer m_timer = new DispatcherTimer();
        List<string> m_listFavourites = new List<string>();
        string[] command_list;
        

        private void loadFavourites()
        {
            //Save favourites in array
            if (File.Exists(m_sStartupPathWithFileFavourites))
            {
                m_listFavourites = File.ReadAllLines(m_sStartupPathWithFileFavourites).ToList();
            }
            else
            {
                File.Create(m_sStartupPathWithFileFavourites);
            }
        }

        private void autoDetectLibrary()
        {
            //GET STEAM PATH
            m_nAmountOfSteamLibraries = 0;
            m_sSteamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null).ToString();
        
            var steamHelper = new SteamShared.SteamHelper();
            m_sLibraries = steamHelper.GetSteamLibraries().Select(lib => lib.Path).ToArray();

            m_nAmountOfSteamLibraries = m_sLibraries.Count();
            m_bAutoDetectedLibrary = false;
            foreach (string library in m_sLibraries)
            {
                string test = library + m_sRelativeConfigFolderPath;
                if (Directory.Exists(library + m_sRelativeConfigFolderPath))
                {
                    m_sAutoLibrary = library;
                    m_bAutoDetectedLibrary = true;
                }
            }
            setPathText();
            if (m_bAutoDetectedLibrary)
                LoadConfigs(m_sAutoLibrary, true); //Try loading configs
            else
                LoadConfigs(m_sConfigFilesPath, false); //Try loading configs
            if (m_bAutoDetectedLibrary)
                showInfoText($"Automatically detected the steam library. Libraries found: {m_nAmountOfSteamLibraries}");
        }

        /// <summary>
        /// Cycles through the config.vdf and gets all library paths
        /// </summary>
        /// <param name="sText">The text to parse (normally the content of the config.vdf file)</param>
        /// <returns>Returns string[] of all library paths</returns>
        private string[] getAllLibraries(string sText)
        {
            int nAmountOfLibraries = getAmountOfLibraries(sText);
            string[] sLibraries = new string[nAmountOfLibraries + 1];
            for (int i = 1; i <= nAmountOfLibraries; i++)
            {
                string sCurrentLibrary = "";
                int nIndex = sText.IndexOf($"BaseInstallFolder_{i}");
                nIndex += ($"BaseInstallFolder_{i}").Count() + 2;
                if (nIndex != -1)
                {
                    while (sText[nIndex - 1] != '"' && ((sText[nIndex - 2] != ' ') || (sText[nIndex - 2] != '\t')))
                    {
                        char stest = sText[nIndex];
                        char stest2 = sText[nIndex - 1];
                        char stest3 = sText[nIndex - 2];
                        nIndex++;
                    }
                    while (sText[nIndex] != '"' && ((sText[nIndex + 1] != ' ') || (sText[nIndex + 1] != '\t')))
                    {
                        sCurrentLibrary += sText[nIndex];
                        nIndex++;
                    }
                }
                sCurrentLibrary = sCurrentLibrary.Replace("\\\\", "\\");
                sLibraries[i] = sCurrentLibrary;
            }
            return sLibraries;
        }

        private int getAmountOfLibraries(string sText)
        {
            int nIndexLibrary = 0;
            while (sText.Contains($"BaseInstallFolder_{nIndexLibrary + 1}"))
            {
                nIndexLibrary++;
            }
            return nIndexLibrary;
        }

        string sDefaultPath;
        private void setPathText()
        {
            if (m_bAutoDetectedLibrary)
                sDefaultPath = m_sAutoLibrary;
            else if (m_sLibraries[0] != null)
                sDefaultPath = m_sLibraries[0];
            else
                sDefaultPath = "C:\\Program Files\\Steam";
            lblPath.Text = sDefaultPath; //Set label text of default path
        }

        private bool saveSettings()
        {
            try
            {
                m_Settings.CheckSubfolders = chkCheckSubfolders.IsChecked == true ? true : false;
                m_Settings.OnlyShowFavourites = chkOnlyShowFavourites.IsChecked == true ? true : false;
                m_Settings.SearchCaseSensitive = chkSearchCaseSensitive.IsChecked == true ? true : false;

                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                FileStream fs = new FileStream(m_sStartupPathWithFileSettings, FileMode.Create);
                using (fs)
                {
                    serializer.Serialize(fs, m_Settings);
                }
                showInfoText("Successfully saved program settings.");
                return true;
            }
            catch
            {
                showInfoText("Failed to save program settings.");
                return false;
            }
        }

        private bool loadSettings()
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(Settings));
                FileStream fs = new FileStream(m_sStartupPathWithFileSettings, FileMode.Open);
                using (fs)
                {
                    m_Settings = (Settings)deserializer.Deserialize(fs);
                }
                chkCheckSubfolders.IsChecked = m_Settings.CheckSubfolders;
                chkOnlyShowFavourites.IsChecked = m_Settings.OnlyShowFavourites;
                chkSearchCaseSensitive.IsChecked = m_Settings.SearchCaseSensitive;
                return true;
            }
            catch
            {
                showInfoText("Failed to load program settings.");
                return false;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            gridInfoText.Visibility = Visibility.Collapsed;
            m_timer.Stop();
        }

        //Filters out and returns the name of a config by getting its string
        //f.e. sPath would be "C:\\Steam\\SteamLibrary\\common\\Counter Strike: Global Offensive\\csgo\\cfg\\name.cfg
        //In this example this function would return "name.cfg" as long as the path ends with "\\nameOfTheConfigFile"
        public string getConfigNameFromPath(string sPath)
        {
            string sResult = "";
            int iStartIndex = sPath.Length - 1;
            char[] Path = sPath.ToCharArray();
            while (Path[iStartIndex - 1] != '\\')
                iStartIndex--;
            for (int i = iStartIndex; i < (sPath.Length - 4); i++)
            {
                sResult += Path[i];
            }
            return sResult;
        }

        string m_sConfigFilesPath;
        string m_sRelativeConfigFolderPath = "\\SteamApps\\common\\Counter-Strike Global Offensive\\csgo\\cfg\\";
        string m_sCurrentlyOpenedConfigPath;
        bool m_bCurrentConfigGotEdited = false;
        bool m_bConfigIsBeingLoaded = false;
        private void btnChangePath_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    LoadConfigs(fbd.SelectedPath, false);
                    btnRefreshConfigs.Visibility = Visibility.Visible;
                }
            }
        }

        private void btnRefreshConfigs_Click(object sender, RoutedEventArgs e)
        {
            LoadConfigs(m_sConfigFilesPath, false);
        }

        List<ctrlConfig> listConfigs;
        private void LoadConfigs(string Path, bool bHideInfoText)
        {
            bool? bConfigChanged = hasConfigChanged();
            if (bConfigChanged == null)
            {
                return;
            }
            try
            {
                stackConfigs.Children.Clear();
                listConfigs = new List<ctrlConfig>();
                m_sConfigFilesPath = Path;

                string sPath = Path + "\\SteamApps\\common\\Counter-Strike Global Offensive\\csgo\\cfg";
                lblPath.Text = sPath;

                string[] files = null;
                try
                {
                    files = Directory.GetFiles(sPath, "*.cfg", (m_Settings.CheckSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                }
                catch
                {
                    System.Windows.MessageBox.Show("Make sure the Path to your Steam Library you have entered is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Array.Sort(files);
                int nAmountOfConfigsShown = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    string sCurrentConfigName = getConfigNameFromPath(files[i]);
                    if (!String.IsNullOrWhiteSpace(txtFilterConfigs.Text) && txtFilterConfigs.Text != "filter configs")
                    {
                        if (m_Settings.SearchCaseSensitive)
                        {
                            if (!sCurrentConfigName.Contains(txtFilterConfigs.Text))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!((sCurrentConfigName.ToUpper()).Contains(txtFilterConfigs.Text.ToUpper())))
                            {
                                continue;
                            }
                        }
                    }
                    if (m_listFavourites != null)
                    {
                        if (m_Settings.OnlyShowFavourites == true)
                        {
                            foreach (string favourite in m_listFavourites)
                            {
                                if (!sCurrentConfigName.Equals(favourite))
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    ctrlConfig currentConfig = new ctrlConfig();
                    currentConfig.txtConfigName.Text = sCurrentConfigName;
                    currentConfig.IsFavourite = false;
                    if (m_listFavourites != null)
                    {
                        foreach (string name in m_listFavourites)
                        {
                            if (name.Equals(sCurrentConfigName))
                                currentConfig.IsFavourite = true;
                        }

                        if (m_Settings.OnlyShowFavourites)
                        {
                            if (!currentConfig.IsFavourite)
                                continue;
                        }
                    }

                    currentConfig.Path = files[i];
                    stackConfigs.Children.Add(currentConfig);
                    currentConfig.OnEditConfig += displayConfig;
                    currentConfig.OnDeleteConfig += onDeleteConfig;
                    currentConfig.OnToggleFavouriteConfig += onToggleFavouriteConfig;
                    listConfigs.Add(currentConfig);
                    nAmountOfConfigsShown++;
                }
                lblConfigsFoundValue.Text = nAmountOfConfigsShown.ToString();
                if (!bHideInfoText)
                    showInfoText("List of all configs retrieved.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                showInfoText("Retrieving of config list failed.");
            }
        }

        char[] forbiddenCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }; //Put all forbidden characters in here
        bool nameContainsForbiddenCharacter(string sName)
        {
            foreach (char character in forbiddenCharacters)
            {
                if (sName.Contains(character))
                {
                    return true;
                }
            }
            return false;
        }

        string convertCharArrayToStringWithSpacesInbetween(char[] charArray)
        {
            string sResult = String.Empty;
            for (int i = 0; i < charArray.Length; i++)
            {
                sResult += charArray[i];
                if (i != (charArray.Length - 1))
                    sResult += " ";
            }
            return sResult;
        }

        bool saveConfig(string sPath)
        {
            //Check if path is null
            if (sPath == null)
            {
                showInfoText("Saving failed.");
                return false;
            }
            //CHECK FILE NAME
            string sEnteredConfigName = txtConfigTitle.Text;
            if (nameContainsForbiddenCharacter(sEnteredConfigName) || String.IsNullOrWhiteSpace(sEnteredConfigName))
            {
                string sForbiddenCharacters = new string(forbiddenCharacters);
                ShowDialog("Forbidden name", $"The config name you have entered is either empty or contains one of the following forbidden characters:\n{convertCharArrayToStringWithSpacesInbetween(forbiddenCharacters)}", DialogButton.Ok);
                return false;
            }
            if (sEnteredConfigName.Contains(".cfg"))
            {
                bool? bKeepFileExtensionInConfigName = ShowDialog("Unnecessary text", "The config name you have entered contains \".cfg\". This file type will be appended automatically when saving. Do you want to keep it anyways?", DialogButton.YesNoCancel);
                if (bKeepFileExtensionInConfigName == true)
                    ;
                else if (bKeepFileExtensionInConfigName == false)
                {
                    showInfoText("Saving failed.");
                    return false;
                }
                else if (bKeepFileExtensionInConfigName == null)
                {
                    showInfoText("Saving failed.");
                    return false;
                }
            }
            //Check if already existing config file would be overwritten
            string sOldConfigPath = sPath;
            string sOldConfigName = getConfigNameFromPath(sPath);
            string sNewConfigPath = m_sConfigFilesPath + m_sRelativeConfigFolderPath + sEnteredConfigName + ".cfg";
            string sNewConfigName = getConfigNameFromPath(sNewConfigPath);
            //If you try to save the config as a new name already existing as a different config
            if (!sPath.Equals(sNewConfigPath))
            {
                if (File.Exists(sNewConfigPath))
                {
                    bool? bOverwriteDialog = ShowDialog("File already exists", $"The config with the name \"{sNewConfigName}\" already exists. Do you want to overwrite it?", DialogButton.YesNoCancel);
                    if (bOverwriteDialog == true) //Overwrite already existing file
                    {
                        try
                        {
                            deleteConfig(sNewConfigPath, false);
                        }
                        catch
                        {
                            showInfoText("File could not be deleted.");
                            return false;
                        }
                    }
                    else if (bOverwriteDialog == false)
                    {
                        showInfoText("Saving aborted.");
                        return false;
                    }
                    else if (bOverwriteDialog == null)
                    {
                        showInfoText("Saving aborted.");
                        return false;
                    }
                }
            }
            isConfigAvailable(sPath);
            //Show timer text
            showInfoText("Config saved.");
            //SAVE FILE CONTENT
            string[] sLinesConfigContent = new string[txtConfigContent.LineCount];
            for (int i = 0; i < txtConfigContent.LineCount; i++)
            {
                sLinesConfigContent[i] = txtConfigContent.GetLineText(i);
            }

            string sOutput = "";
            for (int i = 0; i < sLinesConfigContent.Length; i++)
            {
                sOutput += sLinesConfigContent[i];
                /*if (i != (sLinesConfigContent.Length - 1)) //If it's not the last line being looped
                    sOutput += Environment.NewLine; //Append a new line character*/
            }
            File.WriteAllText(sPath, sOutput);
            //SAVE FILE NAME
            m_sCurrentlyOpenedConfigPath = sNewConfigPath;
            File.Move(sPath, sNewConfigPath);
            m_bCurrentConfigGotEdited = false;
            LoadConfigs(m_sConfigFilesPath, true);
            return true;
        }

        void showInfoText(string sTextToShow)
        {
            m_timer.Stop();
            lblInfoText.Text = sTextToShow;
            gridInfoText.Visibility = Visibility.Visible;
            m_timer.Start();
        }

        string createConfig()
        {
            string newConfigName = "New_Config";
            int newConfigNameNumber = 1;
            bool bNewConfigHasNumber = false;
            if (File.Exists(m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + ".cfg"))
            {
                bNewConfigHasNumber = true;
                while (File.Exists(m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + '_' + newConfigNameNumber.ToString() + ".cfg"))
                {
                    newConfigNameNumber++;
                }
            }
            if (bNewConfigHasNumber)
            {
                File.Create(m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + '_' + newConfigNameNumber.ToString() + ".cfg").Close();
                return m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + '_' + newConfigNameNumber.ToString() + ".cfg";
            }
            else
            {
                File.Create(m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + ".cfg").Close();
                return m_sConfigFilesPath + m_sRelativeConfigFolderPath + newConfigName + ".cfg";
            }
        }

        bool? hasConfigChanged()
        {
            if (m_bCurrentConfigGotEdited)
            {//SAVE?
                if (File.Exists(m_sCurrentlyOpenedConfigPath))
                {
                    bool? bPromptSave = ShowDialog("Unsaved Changes", "You have unsaved changes. Do you want to save your changes?", DialogButton.YesNoCancel, "Yes", "Discard");
                    if (bPromptSave == null)
                    {
                        return null;
                    }
                    else if (bPromptSave == false)
                    {
                        m_bCurrentConfigGotEdited = false;
                        return false;
                    }
                    else if (bPromptSave == true)
                    {
                        saveConfig(m_sCurrentlyOpenedConfigPath);
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        bool? isConfigAvailable(string sPath)
        {
            if (sPath == null)
                return null;
            if (!File.Exists(sPath))
            {
                bool? bPromptCreate = ShowDialog("File Not Found", "The config you tried to load got moved, renamed or deleted. Do you want to create it?", DialogButton.YesNoCancel, "Create", "Delete");
                if (bPromptCreate == null)
                {
                    return null;
                }
                else if (bPromptCreate == false)
                {
                    //REMOVE CONFIG FROM LIST
                    listConfigs.RemoveAt(listConfigs.FindIndex(x => x.Path.Equals(sPath)));
                    LoadConfigs(m_sConfigFilesPath, false);
                    return false;
                }
                else if (bPromptCreate == true)
                {
                    //CREATE FILE
                    File.Create(sPath).Close();
                    if (isConfigCurrentlyOpened(sPath))
                        m_bCurrentConfigGotEdited = false;
                    return true;
                }
            }
            return true;
        }

        //Handles the event of a Config being selected
        void displayConfig(object sender, string sPath)
        {
            bool? configChanged = hasConfigChanged(); //See if changes have been made and ask the user if he wants to save
            if (configChanged == null) //Check if config has changed, if yes ask if wanna save or discard changes
                return;
            else if (configChanged == false)
                ;
            else if (configChanged == true)
                ;

            bool? configAvailable = isConfigAvailable(sPath);
            if (configAvailable == null) //Check if config is available in the folder with its old name, if not create it or remove from list?
                return;
            else if (configAvailable == false)
                return;
            else if (configAvailable == true)
                ;

            //Reads all the config content and stores them into a variable
            string[] sLines = File.ReadAllLines(sPath, Encoding.Default);


            //Put all lines of the selected config
            //into a string with all new line characters
            string sOutput = "";
            //Loop all lines of the selected config content
            //and add them to the sOutput string
            for (int i = 0; i < sLines.Length; i++)
            {
                sOutput += sLines[i];
                if (i != (sLines.Length - 1)) //If it's not the last line being looped
                    sOutput += Environment.NewLine; //Append a new line character
            }

            //Set text boxes to title and the config content
            //for the user to edit
            m_bConfigIsBeingLoaded = true;
            m_bCurrentConfigGotEdited = false;
            txtConfigTitle.Text = getConfigNameFromPath(sPath);
            txtConfigContent.Text = sOutput;
            m_bConfigIsBeingLoaded = false;

            m_sCurrentlyOpenedConfigPath = sPath;
            //Reset the UNDO cache
            txtConfigContent.UndoLimit = 0;
            txtConfigContent.UndoLimit = 100;
            showInfoText("Config loaded.");
            rectTrashbinMouseOver.ToolTip = $"Delete \"{getConfigNameFromPath(m_sCurrentlyOpenedConfigPath)}\"";
            rectSaveMouseOver.ToolTip = $"Save \"{getConfigNameFromPath(m_sCurrentlyOpenedConfigPath)}\"";
        }

        private void onDeleteConfig(object sender, string sPath)
        {
            deleteConfig(sPath);
        }

        private void onToggleFavouriteConfig(object sender, bool bFavourite)
        {
            writeFavourites();
        }

        private void writeFavourites()
        {
            loadFavourites();
            try
            {
                string sCfgName;
                bool bIsAlreadyTracer = false;
                //m_listFavourites.Clear();
                foreach (ctrlConfig config in listConfigs)
                {
                    if (config.IsFavourite)
                    {
                        sCfgName = getConfigNameFromPath(config.Path);
                        bIsAlreadyTracer = false;
                        foreach (string favourite in m_listFavourites)
                        {
                            if (sCfgName == favourite)
                                bIsAlreadyTracer = true;
                        }
                        if (!bIsAlreadyTracer)
                            m_listFavourites.Add(sCfgName);
                    }
                    else //every config which is not a favourite at the time of change
                    {
                        sCfgName = getConfigNameFromPath(config.Path);
                        bIsAlreadyTracer = false;
                        foreach (string favourite in m_listFavourites)
                        {
                            if (sCfgName == favourite)
                            {
                                bIsAlreadyTracer = true;
                                m_listFavourites.Remove(favourite);
                                break;
                            }
                        }
                    }
                }
                File.WriteAllLines(m_sStartupPathWithFileFavourites, m_listFavourites);
                LoadConfigs(m_sConfigFilesPath, true);
            }
            catch
            {

            }
        }

        private bool deleteConfig(string sPath, bool bPromptDelete = true)
        {
            if (isConfigCurrentlyOpened(sPath))
            {
                if (File.Exists(sPath))
                {
                    if (m_bCurrentConfigGotEdited && bPromptDelete)
                    {
                        bool? bPromptSave = ShowDialog("Unsaved Changes", "You have unsaved changes. Do you want to save your changes?", DialogButton.YesNoCancel, "Yes", "Discard");
                        if (bPromptSave == null)
                        {
                            return false;
                        }
                        else if (bPromptSave == true)
                        {
                            saveConfig(sPath);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (!File.Exists(sPath))
            {
                listConfigs.RemoveAt(listConfigs.FindIndex(x => x.Path.Equals(sPath)));
                ShowDialog("File has been removed", $"The config \"{getConfigNameFromPath(sPath)}\" does not exist anymore and has been removed from the list", DialogButton.Ok);
                LoadConfigs(m_sConfigFilesPath, false);
                return true;
            }
            bool? bSureDelete = true;
            if (bPromptDelete)
            {
                bSureDelete = ShowDialog("Delete config", $"Are you sure you want to delete \"{getConfigNameFromPath(sPath)}\"?", DialogButton.YesNoCancel, "Yes", "No, keep config");
            }
            if (bSureDelete == true)
            {
                listConfigs.RemoveAt(listConfigs.FindIndex(x => x.Path.Equals(sPath)));
                File.Delete(sPath);
                LoadConfigs(m_sConfigFilesPath, true);
                showInfoText("Config deleted.");
                if (sPath.Equals(m_sCurrentlyOpenedConfigPath))
                    clearInputs();
                return true;
            }
            if (bSureDelete == false)
            {
                return false;
            }
            if (bSureDelete == null)
            {
                return false;
            }
            return false;
        }

        private void clearInputs()
        {
            txtConfigTitle.Text = String.Empty;
            txtConfigContent.Text = String.Empty;
            m_sCurrentlyOpenedConfigPath = String.Empty;
        }

        private bool isConfigCurrentlyOpened(string sPath)
        {
            if (String.IsNullOrWhiteSpace(m_sCurrentlyOpenedConfigPath))
            {
                return false;
            }
            else if (m_sCurrentlyOpenedConfigPath.Equals(sPath))
            {
                return true;
            }
            return false;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool? configChanged = hasConfigChanged();
            if (configChanged == true)
                ;
            else if (configChanged == false)
                ;
            else if (configChanged == null)
            {
                e.Cancel = true;
                return;
            }

            //Save the last selected config folder path in a text file 
            //if program is closed to load it up the next time the 
            //program is started
            string sStartupPathWithFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSGOConfigManager\\DefaultPath.cfg");
            File.WriteAllText(sStartupPathWithFile, m_sConfigFilesPath);
            saveSettings();
            writeFavourites();
        }

        /// <summary>
        /// Toggles the blur of the Main window on or off
        /// </summary>
        /// <param name="bEnableBlur">Boolean on whether to turn the blur on or off</param>
        private void toggleBlur(bool bEnableBlur)
        {
            BlurEffect blur = new BlurEffect();
            if (bEnableBlur)
                blur.Radius = 10;
            else
                blur.Radius = 0;
            gridNonMenu.Effect = blur;
        }

        enum DialogButton
        {
            YesNoCancel, Ok
        }
        private bool? ShowDialog(string sTitle, string sMessage, DialogButton ButtonType, string sBtn1 = "Yes", string sBtn2 = "No")
        {
            toggleBlur(true);
            SaveDialogWindow wndDialog = new SaveDialogWindow();
            wndDialog.Owner = this;
            wndDialog.lblDialogTitle.Text = sTitle;
            wndDialog.lblDialogText.Text = sMessage;
            switch (ButtonType)
            {
                case DialogButton.YesNoCancel:
                    wndDialog.btnYes.Content = sBtn1;
                    wndDialog.btnNo.Content = sBtn2;
                    break;
                case DialogButton.Ok:
                    wndDialog.btnYes.Content = "Ok";
                    wndDialog.btnNo.Visibility = Visibility.Collapsed;
                    wndDialog.btnCancel.Visibility = Visibility.Collapsed;
                    break;
            }
            wndDialog.ShowDialog();
            if (wndDialog.DialogResult == true)
            {
                toggleBlur(false);
                return true;
            }
            else if (wndDialog.DialogResult == false)
            {
                if (wndDialog.ReturnValue == false)
                {
                    toggleBlur(false);
                    return false;
                }
                else if (wndDialog.ReturnValue == null)
                {
                    toggleBlur(false);
                    return null;
                }
            }
            return null;
        }

        private void txtConfigTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!m_bConfigIsBeingLoaded)
                m_bCurrentConfigGotEdited = true;
        }

        private void txtConfigContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!m_bConfigIsBeingLoaded)
                m_bCurrentConfigGotEdited = true;
        }

        private void txtFilterConfigs_GotFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtFilterConfigs.Text) || txtFilterConfigs.Text == "filter configs")
            {
                txtFilterConfigs.Text = "";
            }
        }

        private void txtFilterConfigs_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtFilterConfigs.Text))
            {
                txtFilterConfigs.Text = "filter configs";
            }
        }

        public string CurrentConfigPath
        {
            get { return m_sConfigFilesPath; }
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            gridPath.Width = Double.NaN;
        }

        private void gridPath_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            gridPath.Width = 160;
        }

        private void gridPath_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Clipboard.SetText(lblPath.Text);
            showInfoText("Configs path copied to clipboard.");
        }

        private void btnCreateConfig_Click(object sender, RoutedEventArgs e)
        {
            displayConfig(sender, createConfig());
            LoadConfigs(m_sConfigFilesPath, true);
            showInfoText("Config created.");
        }

        private void window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (saveConfig(m_sCurrentlyOpenedConfigPath))
                {

                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void rectTrashbinMouseOver_Loaded(object sender, RoutedEventArgs e)
        {
            rectTrashbinMouseOver.ToolTip = "Delete currently opened config";
        }

        private void rectTrashbinMouseOver_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            rectTrashbinMouseOver.Opacity = 1;
        }

        private void rectTrashbinMouseOver_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            rectTrashbinMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            rectTrashbinMouseOver.Opacity = 0;
        }

        private void rectTrashbinMouseOver_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            rectTrashbinMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 30, 30, 30));
        }

        private void rectTrashbinMouseOver_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            rectTrashbinMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            try
            {
                deleteConfig(m_sCurrentlyOpenedConfigPath);
            }
            catch
            {
                showInfoText("No config is opened");
            }
        }

        private void rectSaveMouseOver_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            rectSaveMouseOver.Opacity = 1;
        }

        private void rectSaveMouseOver_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            rectSaveMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            rectSaveMouseOver.Opacity = 0;
        }

        private void rectSaveMouseOver_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            rectTrashbinMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            saveConfig(m_sCurrentlyOpenedConfigPath);
        }

        private void rectSaveMouseOver_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            rectSaveMouseOver.Fill = new SolidColorBrush(Color.FromArgb(77, 30, 30, 30));
        }

        private void rectSaveMouseOver_Loaded(object sender, RoutedEventArgs e)
        {
            rectSaveMouseOver.ToolTip = "Save currently opened config";
        }

        private void txtFilterConfigs_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (hasConfigChanged() == null)
                {
                    e.Handled = true;
                    return;
                }
                LoadConfigs(m_sConfigFilesPath, true);
            }
        }

        private void imgSettingsMenu_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (gridSettingsMenu.Visibility == Visibility.Visible)
            {
                gridSettingsMenu.Visibility = Visibility.Collapsed;
                rectBlockInput.Visibility = Visibility.Collapsed;
                toggleBlur(false);
            }
            else
            {
                gridSettingsMenu.Visibility = Visibility.Visible;
                rectBlockInput.Visibility = Visibility.Visible;
                toggleBlur(true);
            }
        }

        private void btnBackFromSettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (gridSettingsMenu.Visibility == Visibility.Visible)
            {
                gridSettingsMenu.Visibility = Visibility.Collapsed;
                rectBlockInput.Visibility = Visibility.Collapsed;
                toggleBlur(false);
            }
            else
            {
                gridSettingsMenu.Visibility = Visibility.Visible;
                rectBlockInput.Visibility = Visibility.Visible;
                toggleBlur(true);
            }
        }

        private void chkSettings_Changed(object sender, RoutedEventArgs e)
        {
            updateSettings();
            LoadConfigs(m_sConfigFilesPath, true);
        }

        private void updateSettings()
        {
            m_Settings.CheckSubfolders = chkCheckSubfolders.IsChecked == true ? true : false;
            m_Settings.OnlyShowFavourites = chkOnlyShowFavourites.IsChecked == true ? true : false;
            m_Settings.SearchCaseSensitive = chkSearchCaseSensitive.IsChecked == true ? true : false;
            showInfoText("Settings got changed.");
        }

        private void btnAutoDetectLibrary_Click(object sender, RoutedEventArgs e)
        {
            autoDetectLibrary();
        }

        public MainWindow()
        {
            m_timer.Interval = new TimeSpan(0, 0, 2);
            m_timer.Tick += timer_Tick;
            m_timer.Start();

            InitializeComponent();

            Directory.CreateDirectory(m_sStartupPath); //create base program directory
            if (File.Exists(m_sStartupPathWithFileDefaultPath))
            {
                string[] lines = File.ReadAllLines(m_sStartupPathWithFileDefaultPath);
                m_sConfigFilesPath = lines.FirstOrDefault();
            }
            else
            {
                File.WriteAllText(m_sStartupPathWithFileDefaultPath, sDefaultPath);
            }
            loadFavourites();

            autoDetectLibrary();

            setPathText();
            this.Title = "CSGO-Config Manager (" + m_sProgramVersion + ")";
            lblProgramVersion.Text = "Version: " + m_sProgramVersion;
            loadSettings();
            if (m_bAutoDetectedLibrary)
                LoadConfigs(m_sAutoLibrary, true); //Try loading configs
            else
                LoadConfigs(m_sConfigFilesPath, false); //Try loading configs
            if (m_bAutoDetectedLibrary)
                showInfoText($"Automatically detected the steam library. Libraries found: {m_nAmountOfSteamLibraries}");
            command_list = new string[] { "+alt1",
                "+alt2",
                "+attack",
                "+attack2",
                "+back",
                "+break",
                "+camdistance",
                "+camin",
                "+cammousemove",
                "+camout",
                "+campitchdown",
                "+campitchup",
                "+camyawleft",
                "+camyawright",
                "+commandermousemove",
                "+csm_rot_x_neg",
                "+csm_rot_x_plus",
                "+csm_rot_y_neg",
                "+csm_rot_y_plus",
                "+duck",
                "+forward",
                "+graph",
                "+grenade1",
                "+grenade2",
                "+jlook",
                "+jump",
                "+klook",
                "+left",
                "+lookdown",
                "+lookspin",
                "+lookup",
                "+mat_texture_list",
                "+movedown",
                "+moveleft",
                "+moveright",
                "+moveup",
                "+posedebug",
                "+reload",
                "+right",
                "+score",
                "+showbudget",
                "+showbudget_texture",
                "+showbudget_texture_global",
                "+showscores",
                "+showvprof",
                "+speed",
                "+strafe",
                "+use",
                "+vgui_drawtree",
                "+voicerecord",
                "+walk",
                "+zoom",
                "+zoom_in",
                "+zoom_out",
                "-alt1",
                "-alt2",
                "-attack",
                "-attack2",
                "-back",
                "-break",
                "-camdistance",
                "-camin",
                "-cammousemove",
                "-camout",
                "-campitchdown",
                "-campitchup",
                "-camyawleft",
                "-camyawright",
                "-commandermousemove",
                "-csm_rot_x_neg",
                "-csm_rot_x_plus",
                "-csm_rot_y_neg",
                "-csm_rot_y_plus",
                "-duck",
                "-forward",
                "-graph",
                "-grenade1",
                "-grenade2",
                "-jlook",
                "-jump",
                "-klook",
                "-left",
                "-lookdown",
                "-lookspin",
                "-lookup",
                "-mat_texture_list",
                "-movedown",
                "-moveleft",
                "-moveright",
                "-moveup",
                "-posedebug",
                "-reload",
                "-right",
                "-score",
                "-showbudget",
                "-showbudget_texture",
                "-showbudget_texture_global",
                "-showscores",
                "-showvprof",
                "-speed",
                "-strafe",
                "-use",
                "-vgui_drawtree",
                "-voicerecord",
                "-walk",
                "-zoom",
                "-zoom_in",
                "-zoom_out",
                "_autosave",
                "_autosavedangerous",
                "_bugreporter_restart",
                "_record",
                "_resetgamestats",
                "_restart",
                "achievement_debug",
                "achievement_disable",
                "addip",
                "adsp_debug",
                "adsp_reset_nodes",
                "ai_clear_bad_links",
                "ai_debug_los",
                "ai_debug_node_connect",
                "ai_debug_shoot_positions",
                "ai_disable",
                "ai_drawbattlelines",
                "ai_drop_hint",
                "ai_dump_hints",
                "ai_hull",
                "ai_next_hull",
                "ai_nodes",
                "ai_report_task_timings_on_limit",
                "ai_resume",
                "ai_set_move_height_epsilon",
                "ai_setenabled",
                "ai_show_connect",
                "ai_show_connect_crawl",
                "ai_show_connect_fly",
                "ai_show_connect_jump",
                "ai_show_graph_connect",
                "ai_show_grid",
                "ai_show_hints",
                "ai_show_hull",
                "ai_show_node",
                "ai_show_visibility",
                "ai_step",
                "ai_test_los",
                "ai_think_limit_label",
                "ai_vehicle_avoidance",
                "ainet_generate_report",
                "ainet_generate_report_only",
                "air_density",
                "alias",
                "ammo_338mag_max",
                "ammo_357sig_max",
                "ammo_357sig_min_max",
                "ammo_357sig_p250_max",
                "ammo_357sig_small_max",
                "ammo_45acp_max",
                "ammo_50AE_max",
                "ammo_556mm_box_max",
                "ammo_556mm_max",
                "ammo_556mm_small_max",
                "ammo_57mm_max",
                "ammo_762mm_max",
                "ammo_9mm_max",
                "ammo_buckshot_max",
                "ammo_grenade_limit_default",
                "ammo_grenade_limit_flashbang",
                "ammo_grenade_limit_total",
                "askconnect_accept",
                "asw_engine_finished_building_map",
                "async_resume",
                "async_suspend",
                "audit_save_in_memory",
                "autobuy",
                "autosave",
                "autosavedangerous",
                "autosavedangerousissafe",
                "banid",
                "banip",
                "bench_end",
                "bench_showstatsdialog",
                "bench_start",
                "bench_upload",
                "benchframe",
                "bind",
                "bind_osx",
                "BindToggle",
                "blackbox_dump",
                "blackbox_record",
                "bot_add",
                "bot_add_ct",
                "bot_add_t",
                "bot_all_weapons",
                "bot_autodifficulty_threshold_high",
                "bot_autodifficulty_threshold_low",
                "bot_chatter",
                "bot_crouch",
                "bot_debug",
                "bot_debug_target",
                "bot_defer_to_human_goals",
                "bot_defer_to_human_items",
                "bot_difficulty",
                "bot_dont_shoot",
                "bot_freeze",
                "bot_goto_mark",
                "bot_goto_selected",
                "bot_join_after_player",
                "bot_join_team",
                "bot_kick",
                "bot_kill",
                "bot_knives_only",
                "bot_loadout",
                "bot_max_vision_distance_override",
                "bot_mimic",
                "bot_mimic_yaw_offset",
                "bot_pistols_only",
                "bot_place",
                "bot_quota",
                "bot_quota_mode",
                "bot_randombuy",
                "bot_show_battlefront",
                "bot_show_nav",
                "bot_show_occupy_time",
                "bot_snipers_only",
                "bot_stop",
                "bot_traceview",
                "bot_zombie",
                "box",
                "buddha",
                "budget_averages_window",
                "budget_background_alpha",
                "budget_bargraph_background_alpha",
                "budget_bargraph_range_ms",
                "budget_history_numsamplesvisible",
                "budget_history_range_ms",
                "budget_panel_bottom_of_history_fraction",
                "budget_panel_height",
                "budget_panel_width",
                "budget_panel_x",
                "budget_panel_y",
                "budget_peaks_window",
                "budget_show_averages",
                "budget_show_history",
                "budget_show_peaks",
                "budget_toggle_group",
                "bug",
                "bug_swap",
                "bugreporter_uploadasync",
                "bugreporter_username",
                "buildcubemaps",
                "building_cubemaps",
                "buildmodelforworld",
                "buy_stamps",
                "buymenu",
                "buyrandom",
                "c_maxdistance",
                "c_maxpitch",
                "c_maxyaw",
                "c_mindistance",
                "c_minpitch",
                "c_minyaw",
                "c_orthoheight",
                "c_orthowidth",
                "c_thirdpersonshoulder",
                "c_thirdpersonshoulderaimdist",
                "c_thirdpersonshoulderdist",
                "c_thirdpersonshoulderheight",
                "c_thirdpersonshoulderoffset",
                "cache_print",
                "cache_print_lru",
                "cache_print_summary",
                "callvote",
                "cam_collision",
                "cam_command",
                "cam_idealdelta",
                "cam_idealdist",
                "cam_idealdistright",
                "cam_idealdistup",
                "cam_ideallag",
                "cam_idealpitch",
                "cam_idealyaw",
                "cam_showangles",
                "cam_snapto",
                "cancelselect",
                "cash_player_bomb_defused",
                "cash_player_bomb_planted",
                "cash_player_damage_hostage",
                "cash_player_get_killed",
                "cash_player_interact_with_hostage",
                "cash_player_killed_enemy_default",
                "cash_player_killed_enemy_factor",
                "cash_player_killed_hostage",
                "cash_player_killed_teammate",
                "cash_player_rescued_hostage",
                "cash_player_respawn_amount",
                "cash_team_elimination_bomb_map",
                "cash_team_elimination_hostage_map_ct",
                "cash_team_elimination_hostage_map_t",
                "cash_team_hostage_alive",
                "cash_team_hostage_interaction",
                "cash_team_loser_bonus",
                "cash_team_loser_bonus_consecutive_rounds",
                "cash_team_planted_bomb_but_defused",
                "cash_team_rescued_hostage",
                "cash_team_terrorist_win_bomb",
                "cash_team_win_by_defusing_bomb",
                "cash_team_win_by_hostage_rescue",
                "cash_team_win_by_time_running_out_bomb",
                "cash_team_win_by_time_running_out_hostage",
                "cast_hull",
                "cast_ray",
                "cc_emit",
                "cc_findsound",
                "cc_flush",
                "cc_lang",
                "cc_linger_time",
                "cc_predisplay_time",
                "cc_random",
                "cc_showblocks",
                "cc_subtitles",
                "centerview",
                "ch_createairboat",
                "ch_createjeep",
                "changelevel",
                "changelevel2",
                "chet_debug_idle",
                "cl_accountprivacysetting1",
                "cl_allowdownload",
                "cl_allowupload",
                "cl_animationinfo",
                "cl_autobuy",
                "cl_autohelp",
                "cl_autowepswitch",
                "cl_avatar_convert_rgb",
                "cl_backspeed",
                "cl_bob_lower_amt",
                "cl_bob_version",
                "cl_bobamt_lat",
                "cl_bobamt_vert",
                "cl_bobcycle",
                "cl_bobup",
                "cl_brushfastpath",
                "cl_buy_favorite",
                "cl_buy_favorite_nowarn",
                "cl_buy_favorite_quiet",
                "cl_buy_favorite_reset",
                "cl_buy_favorite_set",
                "cl_camera_follow_bone_index",
                "cl_chatfilter_version",
                "cl_chatfilters",
                "cl_clanid",
                "cl_clearhinthistory",
                "cl_clock_correction",
                "cl_clock_correction_adjustment_max_amount",
                "cl_clock_correction_adjustment_max_offset",
                "cl_clock_correction_adjustment_min_offset",
                "cl_clock_correction_force_server_tick",
                "cl_clock_showdebuginfo",
                "cl_clockdrift_max_ms",
                "cl_clockdrift_max_ms_threadmode",
                "cl_cmdrate",
                "cl_color",
                "cl_crosshair_drawoutline",
                "cl_crosshair_dynamic_maxdist_splitratio",
                "cl_crosshair_dynamic_splitalpha_innermod",
                "cl_crosshair_dynamic_splitalpha_outermod",
                "cl_crosshair_dynamic_splitdist",
                "cl_crosshair_outlinethickness",
                "cl_crosshairalpha",
                "cl_crosshaircolor",
                "cl_crosshaircolor_b",
                "cl_crosshaircolor_g",
                "cl_crosshaircolor_r",
                "cl_crosshairdot",
                "cl_crosshairgap",
                "cl_crosshairgap_useweaponvalue",
                "cl_crosshairscale",
                "cl_crosshairsize",
                "cl_crosshairstyle",
                "cl_crosshairthickness",
                "cl_crosshairusealpha",
                "cl_cs_dump_econ_item_stringtable",
                "cl_csm_server_status",
                "cl_csm_status",
                "cl_custommaterial_debug_graph",
                "cl_debug_ugc_downloads",
                "cl_debugrumble",
                "cl_decryptdata_key",
                "cl_decryptdata_key_pub",
                "cl_detail_avoid_force",
                "cl_detail_avoid_radius",
                "cl_detail_avoid_recover_speed",
                "cl_detail_max_sway",
                "cl_detail_multiplier",
                "cl_disable_ragdolls",
                "cl_disablefreezecam",
                "cl_disablehtmlmotd",
                "cl_dm_buyrandomweapons",
                "cl_download_demoplayer",
                "cl_downloadfilter",
                "cl_draw_only_deathnotices",
                "cl_drawhud",
                "cl_drawleaf",
                "cl_drawmaterial",
                "cl_drawshadowtexture",
                "cl_dump_particle_stats",
                "cl_dumpplayer",
                "cl_dumpsplithacks",
                "cl_ent_absbox",
                "cl_ent_bbox",
                "cl_ent_rbox",
                "cl_entityreport",
                "cl_extrapolate",
                "cl_extrapolate_amount",
                "cl_fastdetailsprites",
                "cl_find_ent",
                "cl_find_ent_index",
                "cl_fixedcrosshairgap",
                "cl_flushentitypacket",
                "cl_forcepreload",
                "cl_forwardspeed",
                "cl_freezecameffects_showholiday",
                "cl_freezecampanel_position_dynamic",
                "cl_fullupdate",
                "cl_game_mode_convars",
                "cl_hideserverip",
                "cl_hud_background_alpha",
                "cl_hud_bomb_under_radar",
                "cl_hud_color",
                "cl_hud_healthammo_style",
                "cl_hud_playercount_pos",
                "cl_hud_playercount_showcount",
                "cl_hud_radar_scale",
                "cl_idealpitchscale",
                "cl_ignorepackets",
                "cl_interp",
                "cl_interp_ratio",
                "cl_interpolate",
                "cl_inv_showdividerline",
                "cl_inventory_saved_filter",
                "cl_inventory_saved_sort",
                "cl_jiggle_bone_debug",
                "cl_jiggle_bone_debug_pitch_constraints",
                "cl_jiggle_bone_debug_yaw_constraints",
                "cl_jiggle_bone_invert",
                "cl_join_advertise",
                "cl_lagcompensation",
                "cl_leafsystemvis",
                "cl_leveloverview",
                "cl_leveloverviewmarker",
                "cl_loadout_colorweaponnames",
                "cl_logofile",
                "cl_mainmenu_blog_file",
                "cl_mainmenu_hide_blog",
                "cl_mainmenu_show_blog",
                "cl_mainmenu_show_datagraph",
                "cl_maxrenderable_dist",
                "cl_minimal_rtt_shadows",
                "cl_modemanager_reload",
                "cl_mouseenable",
                "cl_mouselook",
                "cl_obs_interp_enable",
                "cl_observercrosshair",
                "cl_operation_premium_reminder_op05",
                "cl_overdraw_test",
                "cl_panelanimation",
                "cl_particle_retire_cost",
                "cl_particles_dump_effects",
                "cl_particles_dumplist",
                "cl_particles_show_bbox",
                "cl_particles_show_controlpoints",
                "cl_pclass",
                "cl_pdump",
                "cl_phys_show_active",
                "cl_phys_timescale",
                "cl_pitchdown",
                "cl_pitchup",
                "cl_portal_use_new_dissolve",
                "cl_precacheinfo",
                "cl_pred_track",
                "cl_predict",
                "cl_predictioncopy_describe",
                "cl_predictionlist",
                "cl_predictweapons",
                "cl_radar_always_centered",
                "cl_radar_icon_scale_min",
                "cl_radar_rotate",
                "cl_radar_scale",
                "cl_radar_square_with_scoreboard",
                "cl_ragdoll_gravity",
                "cl_rebuy",
                "cl_reload_hud",
                "cl_reloadpostprocessparams",
                "cl_remove_all_workshop_maps",
                "cl_remove_old_ugc_downloads",
                "cl_removedecals",
                "cl_report_soundpatch",
                "cl_resend",
                "cl_resend_timeout",
                "cl_righthand",
                "cl_rumblescale",
                "cl_saveweaponcustomtextures",
                "cl_scalecrosshair",
                "cl_scoreboard_mouse_enable_binding",
                "cl_shadowtextureoverlaysize",
                "cl_show_clan_in_death_notice",
                "cl_showanimstate_activities",
                "cl_showents",
                "cl_showerror",
                "cl_showevents",
                "cl_showfps",
                "cl_showhelp",
                "cl_showloadout",
                "cl_showpluginmessages",
                "cl_showpos",
                "cl_sidespeed",
                "cl_skipfastpath",
                "cl_skipslowpath",
                "cl_sos_test_get_opvar",
                "cl_sos_test_set_opvar",
                "cl_soundemitter_flush",
                "cl_soundemitter_reload",
                "cl_soundfile",
                "cl_soundscape_flush",
                "cl_soundscape_printdebuginfo",
                "cl_spec_follow_grenade_key",
                "cl_spec_mode",
                "cl_spec_show_bindings",
                "cl_spec_stats",
                "cl_sporeclipdistance",
                "cl_ss_origin",
                "cl_steamscreenshots",
                "cl_sun_decay_rate",
                "cl_sunlight_ortho_size",
                "cl_teamid_overhead",
                "cl_teamid_overhead_maxdist",
                "cl_teamid_overhead_maxdist_spec",
                "cl_teamid_overhead_name_alpha",
                "cl_teamid_overhead_name_fadetime",
                "cl_teammate_colors_show",
                "cl_timeout",
                "cl_tree_sway_dir",
                "cl_updaterate",
                "cl_updatevisibility",
                "cl_upspeed",
                "cl_use_new_headbob",
                "cl_use_opens_buy_menu",
                "cl_view",
                "cl_viewmodel_shift_left_amt",
                "cl_viewmodel_shift_right_amt",
                "cl_weapon_debug_print_accuracy",
                "cl_winddir",
                "cl_windspeed",
                "cl_wpn_sway_scale",
                "clear",
                "clear_anim_cache",
                "clear_debug_overlays",
                "clientport",
                "closecaption",
                "closeonbuy",
                "cmd",
                "cmd1",
                "cmd2",
                "cmd3",
                "cmd4",
                "collision_test",
                "colorcorrectionui",
                "commentary_cvarsnotchanging",
                "commentary_finishnode",
                "commentary_firstrun",
                "commentary_showmodelviewer",
                "commentary_testfirstrun",
                "computer_name",
                "con_enable",
                "con_filter_enable",
                "con_filter_text",
                "con_filter_text_out",
                "con_logfile",
                "con_min_severity",
                "condump",
                "confirm_abandon_match",
                "confirm_activate_itemid_now",
                "confirm_join_friend_session_exit_current",
                "confirm_join_new_session_exit_current",
                "confirm_purchase_item_def_now",
                "confirm_watch_friend_session_exit_current",
                "connect",
                "connect_splitscreen",
                "coverme",
                "crash",
                "create_flashlight",
                "CreatePredictionError",
                "creditsdone",
                "crosshair",
                "cs_enable_player_physics_box",
                "cs_hostage_near_rescue_music_distance",
                "cs_make_vip",
                "cs_ShowStateTransitions",
                "CS_WarnFriendlyDamageInterval",
                "csgo_download_match",
                "csgo_econ_action_preview",
                "cursortimeout",
                "custom_bot_difficulty",
                "cvarlist",
                "dbghist_addline",
                "dbghist_dump",
                "debug_map_crc",
                "debug_visibility_monitor",
                "debugsystemui",
                "default_fov",
                "demo_gototick",
                "demo_listhighlights",
                "demo_listimportantticks",
                "demo_pause",
                "demo_recordcommands",
                "demo_resume",
                "demo_timescale",
                "demo_togglepause",
                "demolist",
                "demos",
                "demoui",
                "developer",
                "devshots_nextmap",
                "devshots_screenshot",
                "differences",
                "disable_static_prop_loading",
                "disconnect",
                "disp_list_all_collideable",
                "display_elapsedtime",
                "display_game_events",
                "dlight_debug",
                "dm_reset_spawns",
                "dm_togglerandomweapons",
                "drawcross",
                "drawline",
                "drawoverviewmap",
                "drawradar",
                "ds_get_newest_subscribed_files",
                "dsp_db_min",
                "dsp_db_mixdrop",
                "dsp_dist_max",
                "dsp_dist_min",
                "dsp_enhance_stereo",
                "dsp_mix_max",
                "dsp_mix_min",
                "dsp_off",
                "dsp_player",
                "dsp_reload",
                "dsp_slow_cpu",
                "dsp_volume",
                "dt_dump_flattened_tables",
                "dti_flush",
                "dump_entity_sizes",
                "dump_globals",
                "dump_particlemanifest",
                "dumpentityfactories",
                "dumpeventqueue",
                "dumpgamestringtable",
                "dumpstringtables",
                "echo",
                "econ_build_pinboard_images_from_collection_name",
                "econ_clear_inventory_images",
                "econ_highest_baseitem_seen",
                "econ_show_items_with_tag",
                "editdemo",
                "editor_toggle",
                "enable_debug_overlays",
                "enable_fast_math",
                "enable_skeleton_draw",
                "endmatch_votenextmap",
                "endmovie",
                "endround",
                "enemydown",
                "enemyspot",
                "engine_no_focus_sleep",
                "ent_absbox",
                "ent_attachments",
                "ent_autoaim",
                "ent_bbox",
                "ent_cancelpendingentfires",
                "ent_create",
                "ent_dump",
                "ent_fire",
                "ent_info",
                "ent_keyvalue",
                "ent_messages",
                "ent_messages_draw",
                "ent_name",
                "ent_orient",
                "ent_pause",
                "ent_pivot",
                "ent_rbox",
                "ent_remove",
                "ent_remove_all",
                "ent_rotate",
                "ent_script_dump",
                "ent_setang",
                "ent_setname",
                "ent_setpos",
                "ent_show_response_criteria",
                "ent_step",
                "ent_teleport",
                "ent_text",
                "ent_viewoffset",
                "envmap",
                "error_message_explain_vac",
                "escape",
                "exec",
                "execifexists",
                "execwithwhitelist",
                "exit",
                "explode",
                "explodevector",
                "fadein",
                "fadeout",
                "fallback",
                "ff_damage_bullet_penetration",
                "ff_damage_reduction_bullets",
                "ff_damage_reduction_grenade",
                "ff_damage_reduction_grenade_self",
                "ff_damage_reduction_other",
                "find",
                "find_ent",
                "find_ent_index",
                "findflags",
                "firetarget",
                "firstperson",
                "fish_debug",
                "fish_dormant",
                "flush",
                "flush_locked",
                "fog_color",
                "fog_colorskybox",
                "fog_enable",
                "fog_enable_water_fog",
                "fog_enableskybox",
                "fog_end",
                "fog_endskybox",
                "fog_hdrcolorscale",
                "fog_hdrcolorscaleskybox",
                "fog_maxdensity",
                "fog_maxdensityskybox",
                "fog_override",
                "fog_start",
                "fog_startskybox",
                "fogui",
                "followme",
                "force_audio_english",
                "force_centerview",
                "forcebind",
                "forktest",
                "foundry_engine_get_mouse_control",
                "foundry_engine_release_mouse_control",
                "foundry_select_entity",
                "foundry_sync_hammer_view",
                "foundry_update_entity",
                "fov_cs_debug",
                "fps_max",
                "fps_max_menu",
                "fps_screenshot_frequency",
                "fps_screenshot_threshold",
                "fs_clear_open_duplicate_times",
                "fs_dump_open_duplicate_times",
                "fs_fios_cancel_prefetches",
                "fs_fios_flush_cache",
                "fs_fios_prefetch_file",
                "fs_fios_prefetch_file_in_pack",
                "fs_fios_print_prefetches",
                "fs_printopenfiles",
                "fs_report_sync_opens",
                "fs_syncdvddevcache",
                "fs_warning_level",
                "func_break_max_pieces",
                "fx_new_sparks",
                "g15_dumpplayer",
                "g15_reload",
                "g15_update_msec",
                "g_debug_angularsensor",
                "g_debug_constraint_sounds",
                "g_debug_ragdoll_removal",
                "g_debug_ragdoll_visualize",
                "g_debug_trackpather",
                "g_debug_vehiclebase",
                "g_debug_vehicledriver",
                "g_debug_vehicleexit",
                "g_debug_vehiclesound",
                "g_jeepexitspeed",
                "game_mode",
                "game_type",
                "gameinstructor_dump_open_lessons",
                "gameinstructor_enable",
                "gameinstructor_find_errors",
                "gameinstructor_reload_lessons",
                "gameinstructor_reset_counts",
                "gameinstructor_save_restore_lessons",
                "gameinstructor_verbose",
                "gameinstructor_verbose_lesson",
                "gamemenucommand",
                "gamepadslot1",
                "gamepadslot2",
                "gamepadslot3",
                "gamepadslot4",
                "gamepadslot5",
                "gamepadslot6",
                "gameui_activate",
                "gameui_allowescape",
                "gameui_allowescapetoshow",
                "gameui_hide",
                "gameui_preventescape",
                "gameui_preventescapetoshow",
                "gcmd",
                "getinpos",
                "getout",
                "getpos",
                "getpos_exact",
                "give",
                "givecurrentammo",
                "gl_clear_randomcolor",
                "gl_dump_stats",
                "gl_persistent_buffer_max_offset",
                "gl_shader_compile_time_dump",
                "global_event_log_enabled",
                "global_set",
                "glow_outline_effect_enable",
                "glow_outline_width",
                "god",
                "gods",
                "gotv_theater_container",
                "groundlist",
                "hammer_update_entity",
                "hammer_update_safe_entities",
                "heartbeat",
                "help",
                "hideconsole",
                "hidehud",
                "hideoverviewmap",
                "hidepanel",
                "hideradar",
                "hidescores",
                "holdpos",
                "host_filtered_time_report",
                "host_flush_threshold",
                "host_framerate",
                "host_info_show",
                "host_map",
                "host_name_store",
                "host_players_show",
                "host_reset_config",
                "host_rules_show",
                "host_runofftime",
                "host_sleep",
                "host_timer_report",
                "host_timescale",
                "host_workshop_collection",
                "host_workshop_map",
                "host_writeconfig",
                "host_writeconfig_ss",
                "hostage_debug",
                "hostfile",
                "hostip",
                "hostname",
                "hostport",
                "hud_reloadscheme",
                "hud_scaling",
                "hud_showtargetid",
                "hud_subtitles",
                "hud_takesshots",
                "hunk_print_allocations",
                "hunk_track_allocation_types",
                "hurtme",
                "impulse",
                "in_forceuser",
                "incrementvar",
                "inferno_child_spawn_interval_multiplier",
                "inferno_child_spawn_max_depth",
                "inferno_damage",
                "inferno_debug",
                "inferno_dlight_spacing",
                "inferno_flame_lifetime",
                "inferno_flame_spacing",
                "inferno_forward_reduction_factor",
                "inferno_friendly_fire_duration",
                "inferno_initial_spawn_interval",
                "inferno_max_child_spawn_interval",
                "inferno_max_flames",
                "inferno_max_range",
                "inferno_per_flame_spawn_duration",
                "inferno_scorch_decals",
                "inferno_spawn_angle",
                "inferno_surface_offset",
                "inferno_velocity_decay_factor",
                "inferno_velocity_factor",
                "inferno_velocity_normal_factor",
                "inposition",
                "invnext",
                "invnextgrenade",
                "invnextitem",
                "invnextnongrenade",
                "invprev",
                "ip",
                "ip_steam",
                "ip_tv",
                "joy_accelmax",
                "joy_accelscale",
                "joy_accelscalepoly",
                "joy_advanced",
                "joy_advaxisr",
                "joy_advaxisu",
                "joy_advaxisv",
                "joy_advaxisx",
                "joy_advaxisy",
                "joy_advaxisz",
                "joy_autoaimdampen",
                "joy_autoAimDampenMethod",
                "joy_autoaimdampenrange",
                "joy_axis_deadzone",
                "joy_axisbutton_threshold",
                "joy_cfg_preset",
                "joy_circle_correct",
                "joy_curvepoint_1",
                "joy_curvepoint_2",
                "joy_curvepoint_3",
                "joy_curvepoint_4",
                "joy_curvepoint_end",
                "joy_diagonalpov",
                "joy_display_input",
                "joy_forwardsensitivity",
                "joy_forwardthreshold",
                "joy_gamma",
                "joy_inverty",
                "joy_lowend",
                "joy_lowend_linear",
                "joy_lowmap",
                "joy_movement_stick",
                "joy_name",
                "joy_no_accel_jump",
                "joy_pitchsensitivity",
                "joy_pitchthreshold",
                "joy_response_look",
                "joy_response_look_pitch",
                "joy_response_move",
                "joy_sensitive_step0",
                "joy_sensitive_step1",
                "joy_sensitive_step2",
                "joy_sidesensitivity",
                "joy_sidethreshold",
                "joy_wingmanwarrior_turnhack",
                "joy_yawsensitivity",
                "joy_yawthreshold",
                "joyadvancedupdate",
                "joystick",
                "joystick_force_disabled",
                "joystick_force_disabled_set_from_options",
                "jpeg",
                "kdtree_test",
                "key_bind_version",
                "key_findbinding",
                "key_listboundkeys",
                "key_updatelayout",
                "kick",
                "kickid",
                "kickid_ex",
                "kill",
                "killserver",
                "killvector",
                "lastinv",
                "light_crosshair",
                "lightcache_maxmiss",
                "lightprobe",
                "linefile",
                "list_active_casters",
                "listdemo",
                "listid",
                "listip",
                "listissues",
                "listmodels",
                "listRecentNPCSpeech",
                "load",
                "loadcommentary",
                "loader_dump_table",
                "lobby_default_access",
                "lobby_voice_chat_enabled",
                "locator_split_len",
                "locator_split_maxwide_percent",
                "lockMoveControllerRet",
                "log",
                "log_color",
                "log_dumpchannels",
                "log_flags",
                "log_level",
                "logaddress_add",
                "logaddress_del",
                "logaddress_delall",
                "logaddress_list",
                "lookspring",
                "lookstrafe",
                "loopsingleplayermaps",
                "m_customaccel",
                "m_customaccel_exponent",
                "m_customaccel_max",
                "m_customaccel_scale",
                "m_forward",
                "m_mouseaccel1",
                "m_mouseaccel2",
                "m_mousespeed",
                "m_pitch",
                "m_rawinput",
                "m_side",
                "m_yaw",
                "map",
                "map_background",
                "map_commentary",
                "map_edit",
                "map_setbombradius",
                "map_showbombradius",
                "map_showspawnpoints",
                "mapcycledisabled",
                "mapgroup",
                "mapoverview_allow_client_draw",
                "mapoverview_allow_grid_usage",
                "maps",
                "mat_accelerate_adjust_exposure_down",
                "mat_ambient_light_b",
                "mat_ambient_light_g",
                "mat_ambient_light_r",
                "mat_aniso_disable",
                "mat_autoexposure_max",
                "mat_autoexposure_max_multiplier",
                "mat_autoexposure_min",
                "mat_bloomamount_rate",
                "mat_bumpbasis",
                "mat_camerarendertargetoverlaysize",
                "mat_colcorrection_forceentitiesclientside",
                "mat_colorcorrection",
                "mat_configcurrent",
                "mat_crosshair",
                "mat_crosshair_edit",
                "mat_crosshair_explorer",
                "mat_crosshair_printmaterial",
                "mat_crosshair_reloadmaterial",
                "mat_custommaterialusage",
                "mat_debug_bloom",
                "mat_debug_postprocessing_effects",
                "mat_debugalttab",
                "mat_disable_bloom",
                "mat_displacementmap",
                "mat_drawflat",
                "mat_drawgray",
                "mat_drawwater",
                "mat_dynamic_tonemapping",
                "mat_dynamiclightmaps",
                "mat_dynamicPaintmaps",
                "mat_edit",
                "mat_exposure_center_region_x",
                "mat_exposure_center_region_y",
                "mat_fastclip",
                "mat_fastnobump",
                "mat_fillrate",
                "mat_force_bloom",
                "mat_force_tonemap_min_avglum",
                "mat_force_tonemap_percent_bright_pixels",
                "mat_force_tonemap_percent_target",
                "mat_force_tonemap_scale",
                "mat_forcedynamic",
                "mat_frame_sync_enable",
                "mat_frame_sync_force_texture",
                "mat_fullbright",
                "mat_hdr_enabled",
                "mat_hdr_uncapexposure",
                "mat_hsv",
                "mat_info",
                "mat_leafvis",
                "mat_loadtextures",
                "mat_local_contrast_edge_scale_override",
                "mat_local_contrast_midtone_mask_override",
                "mat_local_contrast_scale_override",
                "mat_local_contrast_vignette_end_override",
                "mat_local_contrast_vignette_start_override",
                "mat_lpreview_mode",
                "mat_luxels",
                "mat_measurefillrate",
                "mat_monitorgamma",
                "mat_monitorgamma_tv_enabled",
                "mat_morphstats",
                "mat_norendering",
                "mat_normalmaps",
                "mat_normals",
                "mat_postprocess_enable",
                "mat_powersavingsmode",
                "mat_proxy",
                "mat_queue_mode",
                "mat_queue_priority",
                "mat_queue_report",
                "mat_reloadallcustommaterials",
                "mat_reloadallmaterials",
                "mat_reloadmaterial",
                "mat_reloadtextures",
                "mat_remoteshadercompile",
                "mat_rendered_faces_count",
                "mat_rendered_faces_spew",
                "mat_reporthwmorphmemory",
                "mat_reversedepth",
                "mat_savechanges",
                "mat_setvideomode",
                "mat_shadercount",
                "mat_show_histogram",
                "mat_show_texture_memory_usage",
                "mat_showcamerarendertarget",
                "mat_showframebuffertexture",
                "mat_showlowresimage",
                "mat_showmaterials",
                "mat_showmaterialsverbose",
                "mat_showmiplevels",
                "mat_showtextures",
                "mat_showwatertextures",
                "mat_softwareskin",
                "mat_spewalloc",
                "mat_spewvertexandpixelshaders",
                "mat_stub",
                "mat_surfaceid",
                "mat_surfacemat",
                "mat_tessellation_accgeometrytangents",
                "mat_tessellation_cornertangents",
                "mat_tessellation_update_buffers",
                "mat_tessellationlevel",
                "mat_texture_list",
                "mat_texture_list_txlod",
                "mat_tonemap_algorithm",
                "mat_updateconvars",
                "mat_viewportscale",
                "mat_viewportupscale",
                "mat_wireframe",
                "mat_yuv",
                "maxplayers",
                "mc_accel_band_size",
                "mc_dead_zone_radius",
                "mc_max_pitchrate",
                "mc_max_yawrate",
                "mdlcache_dump_dictionary_state",
                "mem_compact",
                "mem_dump",
                "mem_dumpvballocs",
                "mem_eat",
                "mem_incremental_compact",
                "mem_incremental_compact_rate",
                "mem_test",
                "mem_vcollide",
                "mem_verify",
                "memory",
                "menuselect",
                "minisave",
                "mm_csgo_community_search_players_min",
                "mm_datacenter_debugprint",
                "mm_debugprint",
                "mm_dedicated_force_servers",
                "mm_dedicated_search_maxping",
                "mm_dlc_debugprint",
                "mm_queue_draft_show",
                "mm_queue_show_stats",
                "mm_server_search_lan_ports",
                "mm_session_search_ping_buckets",
                "mm_session_search_qos_timeout",
                "mod_combiner_info",
                "mod_DumpWeaponWiewModelCache",
                "mod_DumpWeaponWorldModelCache",
                "mod_dynamicloadpause",
                "mod_dynamicloadthrottle",
                "mod_dynamicmodeldebug",
                "molotov_throw_detonate_time",
                "motdfile",
                "movie_fixwave",
                "mp_afterroundmoney",
                "mp_autokick",
                "mp_autoteambalance",
                "mp_backup_restore_list_files",
                "mp_backup_restore_load_autopause",
                "mp_backup_restore_load_file",
                "mp_backup_round_auto",
                "mp_backup_round_file",
                "mp_backup_round_file_last",
                "mp_backup_round_file_pattern",
                "mp_buy_allow_grenades",
                "mp_buy_anywhere",
                "mp_buy_during_immunity",
                "mp_buytime",
                "mp_c4timer",
                "mp_competitive_endofmatch_extra_time",
                "mp_ct_default_grenades",
                "mp_ct_default_melee",
                "mp_ct_default_primary",
                "mp_ct_default_secondary",
                "mp_death_drop_c4",
                "mp_death_drop_defuser",
                "mp_death_drop_grenade",
                "mp_death_drop_gun",
                "mp_deathcam_skippable",
                "mp_default_team_winner_no_objective",
                "mp_defuser_allocation",
                "mp_disable_autokick",
                "mp_display_kill_assists",
                "mp_dm_bonus_length_max",
                "mp_dm_bonus_length_min",
                "mp_dm_bonus_percent",
                "mp_dm_time_between_bonus_max",
                "mp_dm_time_between_bonus_min",
                "mp_do_warmup_offine",
                "mp_do_warmup_period",
                "mp_dump_timers",
                "mp_endmatch_votenextleveltime",
                "mp_endmatch_votenextmap",
                "mp_endmatch_votenextmap_keepcurrent",
                "mp_force_assign_teams",
                "mp_force_pick_time",
                "mp_forcecamera",
                "mp_forcerespawnplayers",
                "mp_forcewin",
                "mp_free_armor",
                "mp_freezetime",
                "mp_friendlyfire",
                "mp_ggprogressive_random_weapon_kills_needed",
                "mp_ggprogressive_round_restart_delay",
                "mp_ggprogressive_use_random_weapons",
                "mp_ggtr_bomb_defuse_bonus",
                "mp_ggtr_bomb_detonation_bonus",
                "mp_ggtr_bomb_pts_for_flash",
                "mp_ggtr_bomb_pts_for_he",
                "mp_ggtr_bomb_pts_for_molotov",
                "mp_ggtr_bomb_pts_for_upgrade",
                "mp_ggtr_bomb_respawn_delay",
                "mp_ggtr_end_round_kill_bonus",
                "mp_ggtr_halftime_delay",
                "mp_ggtr_last_weapon_kill_ends_half",
                "mp_ggtr_num_rounds_autoprogress",
                "mp_give_player_c4",
                "mp_halftime",
                "mp_halftime_duration",
                "mp_halftime_pausetimer",
                "mp_hostages_max",
                "mp_hostages_rescuetime",
                "mp_hostages_run_speed_modifier",
                "mp_hostages_spawn_farthest",
                "mp_hostages_spawn_force_positions",
                "mp_hostages_spawn_same_every_round",
                "mp_hostages_takedamage",
                "mp_humanteam",
                "mp_ignore_round_win_conditions",
                "mp_join_grace_time",
                "mp_limitteams",
                "mp_logdetail",
                "mp_match_can_clinch",
                "mp_match_end_changelevel",
                "mp_match_end_restart",
                "mp_match_restart_delay",
                "mp_maxmoney",
                "mp_maxrounds",
                "mp_molotovusedelay",
                "mp_overtime_enable",
                "mp_overtime_halftime_pausetimer",
                "mp_overtime_maxrounds",
                "mp_overtime_startmoney",
                "mp_pause_match",
                "mp_playercashawards",
                "mp_playerid",
                "mp_playerid_delay",
                "mp_playerid_hold",
                "mp_radar_showall",
                "mp_randomspawn",
                "mp_randomspawn_los",
                "mp_respawn_immunitytime",
                "mp_respawn_on_death_ct",
                "mp_respawn_on_death_t",
                "mp_respawnwavetime_ct",
                "mp_respawnwavetime_t",
                "mp_restartgame",
                "mp_round_restart_delay",
                "mp_roundtime",
                "mp_roundtime_defuse",
                "mp_roundtime_hostage",
                "mp_scrambleteams",
                "mp_solid_teammates",
                "mp_spawnprotectiontime",
                "mp_spec_swapplayersides",
                "mp_spectators_max",
                "mp_startmoney",
                "mp_swapteams",
                "mp_switchteams",
                "mp_t_default_grenades",
                "mp_t_default_melee",
                "mp_t_default_primary",
                "mp_t_default_secondary",
                "mp_td_dmgtokick",
                "mp_td_dmgtowarn",
                "mp_td_spawndmgthreshold",
                "mp_teamcashawards",
                "mp_teamflag_1",
                "mp_teamflag_2",
                "mp_teamlogo_1",
                "mp_teamlogo_2",
                "mp_teammatchstat_1",
                "mp_teammatchstat_2",
                "mp_teammatchstat_cycletime",
                "mp_teammatchstat_holdtime",
                "mp_teammatchstat_txt",
                "mp_teammates_are_enemies",
                "mp_teamname_1",
                "mp_teamname_2",
                "mp_teamprediction_pct",
                "mp_teamprediction_txt",
                "mp_timelimit",
                "mp_tkpunish",
                "mp_tournament_restart",
                "mp_unpause_match",
                "mp_use_respawn_waves",
                "mp_verbose_changelevel_spew",
                "mp_warmup_end",
                "mp_warmup_pausetimer",
                "mp_warmup_start",
                "mp_warmuptime",
                "mp_warmuptime_all_players_connected",
                "mp_weapons_allow_map_placed",
                "mp_weapons_allow_randomize",
                "mp_weapons_allow_zeus",
                "mp_weapons_glow_on_ground",
                "mp_win_panel_display_time",
                "ms_player_dump_properties",
                "multvar",
                "muzzleflash_light",
                "name",
                "nav_add_to_selected_set",
                "nav_add_to_selected_set_by_id",
                "nav_analyze",
                "nav_analyze_scripted",
                "nav_area_bgcolor",
                "nav_area_max_size",
                "nav_avoid",
                "nav_begin_area",
                "nav_begin_deselecting",
                "nav_begin_drag_deselecting",
                "nav_begin_drag_selecting",
                "nav_begin_selecting",
                "nav_begin_shift_xy",
                "nav_build_ladder",
                "nav_check_connectivity",
                "nav_check_file_consistency",
                "nav_check_floor",
                "nav_check_stairs",
                "nav_chop_selected",
                "nav_clear_attribute",
                "nav_clear_selected_set",
                "nav_clear_walkable_marks",
                "nav_compress_id",
                "nav_connect",
                "nav_coplanar_slope_limit",
                "nav_coplanar_slope_limit_displacement",
                "nav_corner_adjust_adjacent",
                "nav_corner_lower",
                "nav_corner_place_on_ground",
                "nav_corner_raise",
                "nav_corner_select",
                "nav_create_area_at_feet",
                "nav_create_place_on_ground",
                "nav_crouch",
                "nav_debug_blocked",
                "nav_delete",
                "nav_delete_marked",
                "nav_disconnect",
                "nav_displacement_test",
                "nav_dont_hide",
                "nav_draw_limit",
                "nav_edit",
                "nav_end_area",
                "nav_end_deselecting",
                "nav_end_drag_deselecting",
                "nav_end_drag_selecting",
                "nav_end_selecting",
                "nav_end_shift_xy",
                "nav_flood_select",
                "nav_gen_cliffs_approx",
                "nav_generate",
                "nav_generate_fencetops",
                "nav_generate_fixup_jump_areas",
                "nav_generate_incremental",
                "nav_generate_incremental_range",
                "nav_generate_incremental_tolerance",
                "nav_jump",
                "nav_ladder_flip",
                "nav_load",
                "nav_lower_drag_volume_max",
                "nav_lower_drag_volume_min",
                "nav_make_sniper_spots",
                "nav_mark",
                "nav_mark_attribute",
                "nav_mark_unnamed",
                "nav_mark_walkable",
                "nav_max_view_distance",
                "nav_max_vis_delta_list_length",
                "nav_merge",
                "nav_merge_mesh",
                "nav_no_hostages",
                "nav_no_jump",
                "nav_place_floodfill",
                "nav_place_list",
                "nav_place_pick",
                "nav_place_replace",
                "nav_place_set",
                "nav_potentially_visible_dot_tolerance",
                "nav_precise",
                "nav_quicksave",
                "nav_raise_drag_volume_max",
                "nav_raise_drag_volume_min",
                "nav_recall_selected_set",
                "nav_remove_from_selected_set",
                "nav_remove_jump_areas",
                "nav_run",
                "nav_save",
                "nav_save_selected",
                "nav_select_blocked_areas",
                "nav_select_damaging_areas",
                "nav_select_half_space",
                "nav_select_invalid_areas",
                "nav_select_obstructed_areas",
                "nav_select_overlapping",
                "nav_select_radius",
                "nav_select_stairs",
                "nav_selected_set_border_color",
                "nav_selected_set_color",
                "nav_set_place_mode",
                "nav_shift",
                "nav_show_approach_points",
                "nav_show_area_info",
                "nav_show_compass",
                "nav_show_continguous",
                "nav_show_danger",
                "nav_show_light_intensity",
                "nav_show_node_grid",
                "nav_show_node_id",
                "nav_show_nodes",
                "nav_show_player_counts",
                "nav_show_potentially_visible",
                "nav_simplify_selected",
                "nav_slope_limit",
                "nav_slope_tolerance",
                "nav_snap_to_grid",
                "nav_solid_props",
                "nav_splice",
                "nav_split",
                "nav_split_place_on_ground",
                "nav_stand",
                "nav_stop",
                "nav_store_selected_set",
                "nav_strip",
                "nav_subdivide",
                "nav_test_node",
                "nav_test_node_crouch",
                "nav_test_node_crouch_dir",
                "nav_test_stairs",
                "nav_toggle_deselecting",
                "nav_toggle_in_selected_set",
                "nav_toggle_place_mode",
                "nav_toggle_place_painting",
                "nav_toggle_selected_set",
                "nav_toggle_selecting",
                "nav_transient",
                "nav_unmark",
                "nav_update_blocked",
                "nav_update_lighting",
                "nav_update_visibility_on_edit",
                "nav_use_place",
                "nav_walk",
                "nav_warp_to_mark",
                "nav_world_center",
                "needbackup",
                "net_allow_multicast",
                "net_blockmsg",
                "net_channels",
                "net_droponsendoverflow",
                "net_droppackets",
                "net_dumpeventstats",
                "net_earliertempents",
                "net_fakejitter",
                "net_fakelag",
                "net_fakeloss",
                "net_graph",
                "net_graphheight",
                "net_graphholdsvframerate",
                "net_graphmsecs",
                "net_graphpos",
                "net_graphproportionalfont",
                "net_graphshowinterp",
                "net_graphshowlatency",
                "net_graphshowsvframerate",
                "net_graphsolid",
                "net_graphtext",
                "net_maxroutable",
                "net_public_adr",
                "net_scale",
                "net_showreliablesounds",
                "net_showsplits",
                "net_showudp",
                "net_showudp_oob",
                "net_showudp_remoteonly",
                "net_splitrate",
                "net_start",
                "net_status",
                "net_steamcnx_allowrelay",
                "net_steamcnx_enabled",
                "net_steamcnx_status",
                "net_threaded_socket_burst_cap",
                "net_threaded_socket_recovery_rate",
                "net_threaded_socket_recovery_time",
                "next",
                "nextdemo",
                "nextlevel",
                "nextmap_print_enabled",
                "noclip",
                "noclip_fixup",
                "notarget",
                "npc_ally_deathmessage",
                "npc_ammo_deplete",
                "npc_bipass",
                "npc_combat",
                "npc_conditions",
                "npc_create",
                "npc_create_aimed",
                "npc_destroy",
                "npc_destroy_unselected",
                "npc_enemies",
                "npc_focus",
                "npc_freeze",
                "npc_freeze_unselected",
                "npc_go",
                "npc_go_random",
                "npc_heal",
                "npc_height_adjust",
                "npc_kill",
                "npc_nearest",
                "npc_relationships",
                "npc_reset",
                "npc_route",
                "npc_select",
                "npc_set_freeze",
                "npc_set_freeze_unselected",
                "npc_squads",
                "npc_steering",
                "npc_steering_all",
                "npc_task_text",
                "npc_tasks",
                "npc_teleport",
                "npc_thinknow",
                "npc_viewcone",
                "observer_use",
                "option_duck_method",
                "option_speed_method",
                "paintsplat_bias",
                "paintsplat_max_alpha_noise",
                "paintsplat_noise_enabled",
                "panel_test_title_safe",
                "particle_simulateoverflow",
                "particle_test_attach_attachment",
                "particle_test_attach_mode",
                "particle_test_file",
                "particle_test_start",
                "particle_test_stop",
                "password",
                "path",
                "pause",
                "perfui",
                "perfvisualbenchmark",
                "perfvisualbenchmark_abort",
                "phys_debug_check_contacts",
                "phys_show_active",
                "physics_budget",
                "physics_constraints",
                "physics_debug_entity",
                "physics_highlight_active",
                "physics_report_active",
                "physics_select",
                "picker",
                "ping",
                "pingserver",
                "pixelvis_debug",
                "play",
                "play_distance",
                "play_with_friends_enabled",
                "playdemo",
                "player_botdifflast_s",
                "player_competitive_maplist_8_4_0_EF00F6A0",
                "player_debug_print_damage",
                "player_gamemodelast_m",
                "player_gamemodelast_s",
                "player_gametypelast_m",
                "player_gametypelast_s",
                "player_last_leaderboards_filter",
                "player_last_leaderboards_mode",
                "player_last_leaderboards_panel",
                "player_last_medalstats_category",
                "player_last_medalstats_panel",
                "player_maplast_m",
                "player_maplast_s",
                "player_medalstats_most_recent_time",
                "player_medalstats_recent_range",
                "player_nevershow_communityservermessage",
                "player_teamplayedlast",
                "playflush",
                "playgamesound",
                "playoverwatchevidence",
                "playsoundscape",
                "playvideo",
                "playvideo_end_level_transition",
                "playvideo_exitcommand",
                "playvideo_exitcommand_nointerrupt",
                "playvideo_nointerrupt",
                "playvol",
                "plugin_load",
                "plugin_pause",
                "plugin_pause_all",
                "plugin_print",
                "plugin_unload",
                "plugin_unpause",
                "plugin_unpause_all",
                "post_jump_crouch",
                "press_x360_button",
                "print_colorcorrection",
                "print_mapgroup",
                "print_mapgroup_sv",
                "progress_enable",
                "prop_crosshair",
                "prop_debug",
                "prop_dynamic_create",
                "prop_physics_create",
                "pwatchent",
                "pwatchvar",
                "quit",
                "quit_prompt",
                "r_AirboatViewDampenDamp",
                "r_AirboatViewDampenFreq",
                "r_AirboatViewZHeight",
                "r_alphafade_usefov",
                "r_ambientfraction",
                "r_ambientlightingonly",
                "r_avglight",
                "r_avglightmap",
                "r_brush_queue_mode",
                "r_cheapwaterend",
                "r_cheapwaterstart",
                "r_cleardecals",
                "r_ClipAreaFrustums",
                "r_ClipAreaPortals",
                "r_colorstaticprops",
                "r_debugcheapwater",
                "r_debugrandomstaticlighting",
                "r_depthoverlay",
                "r_disable_distance_fade_on_big_props",
                "r_disable_distance_fade_on_big_props_thresh",
                "r_disable_update_shadow",
                "r_DispBuildable",
                "r_DispWalkable",
                "r_dlightsenable",
                "r_drawallrenderables",
                "r_DrawBeams",
                "r_drawbrushmodels",
                "r_drawclipbrushes",
                "r_drawdecals",
                "r_DrawDisp",
                "r_drawentities",
                "r_drawfuncdetail",
                "r_drawleaf",
                "r_drawlightcache",
                "r_drawlightinfo",
                "r_drawlights",
                "r_DrawModelLightOrigin",
                "r_drawmodelstatsoverlay",
                "r_drawmodelstatsoverlaydistance",
                "r_drawmodelstatsoverlayfilter",
                "r_drawmodelstatsoverlaymax",
                "r_drawmodelstatsoverlaymin",
                "r_drawopaquerenderables",
                "r_drawopaqueworld",
                "r_drawothermodels",
                "r_drawparticles",
                "r_DrawPortals",
                "r_DrawRain",
                "r_drawrenderboxes",
                "r_drawropes",
                "r_drawscreenoverlay",
                "r_drawskybox",
                "r_drawsprites",
                "r_drawstaticprops",
                "r_drawtracers",
                "r_drawtracers_firstperson",
                "r_drawtracers_movetonotintersect",
                "r_drawtranslucentrenderables",
                "r_drawtranslucentworld",
                "r_drawunderwateroverlay",
                "r_drawvgui",
                "r_drawviewmodel",
                "r_drawworld",
                "r_dscale_basefov",
                "r_dscale_fardist",
                "r_dscale_farscale",
                "r_dscale_neardist",
                "r_dscale_nearscale",
                "r_dynamic",
                "r_dynamiclighting",
                "r_eyegloss",
                "r_eyemove",
                "r_eyeshift_x",
                "r_eyeshift_y",
                "r_eyeshift_z",
                "r_eyesize",
                "r_eyewaterepsilon",
                "r_farz",
                "r_flashlightambient",
                "r_flashlightbacktraceoffset",
                "r_flashlightbrightness",
                "r_flashlightclip",
                "r_flashlightconstant",
                "r_flashlightdrawclip",
                "r_flashlightfar",
                "r_flashlightfov",
                "r_flashlightladderdist",
                "r_flashlightlinear",
                "r_flashlightlockposition",
                "r_flashlightmuzzleflashfov",
                "r_flashlightnear",
                "r_flashlightnearoffsetscale",
                "r_flashlightoffsetforward",
                "r_flashlightoffsetright",
                "r_flashlightoffsetup",
                "r_flashlightquadratic",
                "r_flashlightshadowatten",
                "r_flashlightvisualizetrace",
                "r_flushlod",
                "r_hwmorph",
                "r_itemblinkmax",
                "r_itemblinkrate",
                "r_JeepFOV",
                "r_JeepViewBlendTo",
                "r_JeepViewBlendToScale",
                "r_JeepViewBlendToTime",
                "r_JeepViewDampenDamp",
                "r_JeepViewDampenFreq",
                "r_JeepViewZHeight",
                "r_lightcache_invalidate",
                "r_lightcache_numambientsamples",
                "r_lightcache_radiusfactor",
                "r_lightcachecenter",
                "r_lightcachemodel",
                "r_lightinterp",
                "r_lightmap",
                "r_lightstyle",
                "r_lightwarpidentity",
                "r_lockpvs",
                "r_mapextents",
                "r_modelAmbientMin",
                "r_modelwireframedecal",
                "r_nohw",
                "r_nosw",
                "r_novis",
                "r_occlusionspew",
                "r_oldlightselection",
                "r_particle_demo",
                "r_partition_level",
                "r_portalsopenall",
                "r_PortalTestEnts",
                "r_printdecalinfo",
                "r_proplightingpooling",
                "r_radiosity",
                "r_rainalpha",
                "r_rainalphapow",
                "r_RainCheck",
                "r_RainDebugDuration",
                "r_raindensity",
                "r_RainHack",
                "r_rainlength",
                "r_RainProfile",
                "r_RainRadius",
                "r_RainSideVel",
                "r_RainSimulate",
                "r_rainspeed",
                "r_RainSplashPercentage",
                "r_rainwidth",
                "r_randomflex",
                "r_rimlight",
                "r_ropes_holiday_light_color",
                "r_screenoverlay",
                "r_shadow_debug_spew",
                "r_shadow_deferred",
                "r_shadowangles",
                "r_shadowblobbycutoff",
                "r_shadowcolor",
                "r_shadowdir",
                "r_shadowdist",
                "r_shadowfromanyworldlight",
                "r_shadowfromworldlights_debug",
                "r_shadowids",
                "r_shadows_gamecontrol",
                "r_shadowwireframe",
                "r_showenvcubemap",
                "r_showz_power",
                "r_skin",
                "r_skybox",
                "r_slowpathwireframe",
                "r_SnowDebugBox",
                "r_SnowEnable",
                "r_SnowEndAlpha",
                "r_SnowEndSize",
                "r_SnowFallSpeed",
                "r_SnowInsideRadius",
                "r_SnowOutsideRadius",
                "r_SnowParticles",
                "r_SnowPosScale",
                "r_SnowRayEnable",
                "r_SnowRayLength",
                "r_SnowRayRadius",
                "r_SnowSpeedScale",
                "r_SnowStartAlpha",
                "r_SnowStartSize",
                "r_SnowWindScale",
                "r_SnowZoomOffset",
                "r_SnowZoomRadius",
                "r_swingflashlight",
                "r_updaterefracttexture",
                "r_vehicleBrakeRate",
                "r_VehicleViewClamp",
                "r_VehicleViewDampen",
                "r_visocclusion",
                "r_visualizelighttraces",
                "r_visualizelighttracesshowfulltrace",
                "r_visualizetraces",
                "radarvisdistance",
                "radarvismaxdot",
                "radarvismethod",
                "radarvispow",
                "radio1",
                "radio2",
                "radio3",
                "rangefinder",
                "rate",
                "rcon",
                "rcon_address",
                "rcon_password",
                "ready_to_join_game_proceed_to_motd_and_team_select",
                "rebuy",
                "recompute_speed",
                "record",
                "regroup",
                "reload",
                "reload_vjobs",
                "remote_bug",
                "removeallids",
                "removeid",
                "removeip",
                "render_blanks",
                "report",
                "report_cliententitysim",
                "report_clientthinklist",
                "report_entities",
                "report_simthinklist",
                "report_soundpatch",
                "report_touchlinks",
                "reportingin",
                "reset_expo",
                "reset_gameconvars",
                "respawn_entities",
                "restart",
                "retry",
                "roger",
                "rope_min_pixel_diameter",
                "rpt",
                "rpt_client_enable",
                "rpt_connect",
                "rpt_download_log",
                "rpt_end",
                "rpt_password",
                "rpt_screenshot",
                "rpt_server_enable",
                "rpt_start",
                "rr_followup_maxdist",
                "rr_forceconcept",
                "rr_reloadresponsesystems",
                "rr_remarkable_max_distance",
                "rr_remarkable_world_entities_replay_limit",
                "rr_remarkables_enabled",
                "rr_thenany_score_slop",
                "safezonex",
                "safezoney",
                "save",
                "save_finish_async",
                "say",
                "say_team",
                "scandemo",
                "scene_flush",
                "scene_playvcd",
                "scene_showfaceto",
                "scene_showlook",
                "scene_showmoveto",
                "scene_showunlock",
                "screenshot",
                "script",
                "script_client",
                "script_debug",
                "script_debug_client",
                "script_dump_all",
                "script_dump_all_client",
                "script_execute",
                "script_execute_client",
                "script_help",
                "script_help_client",
                "script_reload_code",
                "script_reload_entity_code",
                "script_reload_think",
                "sectorclear",
                "send_round_backup_file_list",
                "sensitivity",
                "server_game_time",
                "servercfgfile",
                "setang",
                "setang_exact",
                "setinfo",
                "setmaster",
                "setmodel",
                "setpause",
                "setpos",
                "setpos_exact",
                "setpos_player",
                "sf4_meshcache_stats",
                "sf_ui_tint",
                "shake",
                "shake_stop",
                "shake_testpunch",
                "show_loadout_toggle",
                "showbudget_texture",
                "showbudget_texture_global_dumpstats",
                "showconsole",
                "showinfo"
            };
        }

        private void btnDeleteEmptyConfigs_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
