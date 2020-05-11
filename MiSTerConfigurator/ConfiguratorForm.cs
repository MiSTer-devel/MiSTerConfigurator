using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace MiSTerConfigurator
{
    public partial class ConfiguratorForm : Form
    {
        const String strGitHubURL = "https://github.com";
        const String strInternetTestURL = "https://google.com";
        const String strMiSTerURL = "https://github.com/MiSTer-devel/Main_MiSTer";
        const String strMiSTerINIURL = "https://github.com/MiSTer-devel/Main_MiSTer/blob/master/MiSTer.ini";
        const String strMiSTerWikiURL = "https://github.com/MiSTer-devel/Main_MiSTer/wiki/";
        // The arcade files no longer apprear in the right menu load from there own page
        const String strMiSTerArcadeWikiURL = "https://github.com/MiSTer-devel/Main_MiSTer/wiki/Arcade-Cores-List";
        const String strMacOSInstaller = "macOS MiSTer SD Card Formatter Script (by michaelshmitty)|https://github.com/michaelshmitty/SD-Installer-macos_MiSTer|MiSTer-sd-installer-macos.sh";
        const String strLinuxInstaller = "Linux MiSTer SD Card Formatter Script (by alanswx)|https://github.com/alanswx/SD-installer_MiSTer|create_sd.sh";
        const string strLocalFilesDir = "files";
        const int intErrorPause = 1000;
        const String strUIStopLabel = "Stop";
        String strUIWizardButtonLabel;
        String strUICoresButtonLabel;
        String strUIExtrasButtonLabel;

        String strWorkDir = Path.Combine("Scripts", ".mister_updater");
        bool blnInitialized = false;
        System.Net.WebClient objWebClient = new System.Net.WebClient();
        bool blnDownloadingCores = false;
        bool blnDownloadingExtras = false;
        bool blnRunningWizard = false;
        String strMiSTerINI_GitHub = "";
        String strMiSTerINI_Current = "";

        String strMiSTerDir_constructor = "";
        bool blnMiSTerDir_locked = false;

        enum enmOS : byte { Windows, MacOS, Linux };
        enmOS bytOS = enmOS.Windows;
 
        public ConfiguratorForm()
        {
            InitializeComponent();
        }

        public ConfiguratorForm(String MiSTerDir, bool locked)
        {
            strMiSTerDir_constructor = MiSTerDir;
            blnMiSTerDir_locked = locked;
            InitializeComponent();
        }



        #region "Wizard business logic"

        const String strVideoMode_INIKey = "video_mode";
        const String strVideoModePAL_INIKey = "video_mode_pal";
        const String strVideoModeNTSC_INIKey = "video_mode_ntsc";
        const String strVScaleMode_INIKey = "vscale_mode";
        const String strVSyncAdjust_INIKey = "vsync_adjust";

        const String strUser_Samba = "SAMBA_USER";
        const String strPassword_Samba = "SAMBA_PASS";

        const String strCountry_WiFi = "country";
        const String strSSID_WiFi = "ssid";
        const String strPassword_WiFi = "psk";


        private void readMiSTerINI(String strMiSTerINI)
        {
            //String strMiSTerINI=readFileSTR(iniFile);
            int intValue;

            if (String.IsNullOrEmpty(strMiSTerINI)) return;

            intValue = getINIValueINT(strMiSTerINI, strVideoMode_INIKey);
            if (intValue >= 0) cmbVideoMode.SelectedIndex = intValue;
            if (getINIValueINT(strMiSTerINI, strVideoModePAL_INIKey) >= 0 && getINIValueINT(strMiSTerINI, strVideoModeNTSC_INIKey) >= 0)
            {
                chkEnablePAL_NTSC.Checked = true;
            }
            else
            {
                chkEnablePAL_NTSC.Checked = false;
            };
            intValue = getINIValueINT(strMiSTerINI, strVScaleMode_INIKey);
            if (intValue >= 0) cmbScalingMode.SelectedIndex = intValue;
            intValue = getINIValueINT(strMiSTerINI, "vsync_adjust");
            if (intValue >= 0) cmbVSyncMode.SelectedIndex = intValue;
        }

        private void saveMiSTerINI(ref String strMiSTerINI, String iniFile)
        {
            //String strMiSTerINI = readFileSTR(iniFile);

            if (String.IsNullOrEmpty(strMiSTerINI)) return;

            strMiSTerINI = setINIValue(strMiSTerINI, strVideoMode_INIKey, cmbVideoMode.SelectedIndex.ToString(), false);
            switch (cmbVideoMode.SelectedIndex)
            {
                case 8:
                case 9:
                    strMiSTerINI = setINIValue(strMiSTerINI, strVideoModeNTSC_INIKey, 8.ToString(), !chkEnablePAL_NTSC.Checked);
                    strMiSTerINI = setINIValue(strMiSTerINI, strVideoModePAL_INIKey, 9.ToString(), !chkEnablePAL_NTSC.Checked);
                    break;
                default:
                    strMiSTerINI = setINIValue(strMiSTerINI, strVideoModeNTSC_INIKey, 0.ToString(), !chkEnablePAL_NTSC.Checked);
                    strMiSTerINI = setINIValue(strMiSTerINI, strVideoModePAL_INIKey, 7.ToString(), !chkEnablePAL_NTSC.Checked);
                    break;
            }

            strMiSTerINI = setINIValue(strMiSTerINI, strVScaleMode_INIKey, cmbScalingMode.SelectedIndex.ToString(), false);
            strMiSTerINI = setINIValue(strMiSTerINI, "vsync_adjust", cmbVSyncMode.SelectedIndex.ToString(), false);

            writeFileSTR(iniFile, strMiSTerINI);
        }

        private void readSamba()
        {
            String strFileContent;
            if (File.Exists(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "samba.sh")))
            {
                chkEnableSamba.Checked = true;
                chkEnableSamba.Enabled = true;
                strFileContent = readFileSTR(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "samba.sh"));
            }
            else if (File.Exists(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_samba.sh")))
            {
                chkEnableSamba.Checked = false;
                chkEnableSamba.Enabled = true;
                strFileContent = readFileSTR(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_samba.sh"));
            }
            else
            {
                chkEnableSamba.Checked = false;
                chkEnableSamba.Enabled = false;
                strFileContent = "";
            };
            if (!String.IsNullOrEmpty(strFileContent))
            {
                txtSambaUserName.Text = getINIValueSTR(strFileContent, strUser_Samba);
                txtSambaPassword.Text = getINIValueSTR(strFileContent, strPassword_Samba);
            };
        }

        private void saveSamba()
        {
            String strFileSambaEnabled = Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "samba.sh");
            String strFileSambaDisabled = Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_samba.sh");
            String strSamba;

            if (chkEnableSamba.Checked)
            {
                if (!File.Exists(strFileSambaEnabled))
                {
                    if (File.Exists(strFileSambaDisabled))
                    {
                        File.Move(strFileSambaDisabled, strFileSambaEnabled);
                    };
                }
                if (File.Exists(strFileSambaEnabled))
                {
                    strSamba = readFileSTR(strFileSambaEnabled);
                    strSamba = setINIValue(strSamba, strUser_Samba, txtSambaUserName.Text, false);
                    strSamba = setINIValue(strSamba, strPassword_Samba, txtSambaPassword.Text, false);
                    writeFileSTR(strFileSambaEnabled, strSamba);
                };
            }
            else
            {
                if (File.Exists(strFileSambaEnabled))
                {
                    if (File.Exists(strFileSambaDisabled))
                    {
                        File.Delete(strFileSambaEnabled);
                    }
                    else
                    {
                        File.Move(strFileSambaEnabled, strFileSambaDisabled);
                    };
                };
            };
        }

        private void readWiFi()
        {
            String strFileContent;
            if (File.Exists(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "wpa_supplicant.conf")))
            {
                chkEnableWiFi.Checked = true;
                chkEnableWiFi.Enabled = true;
                strFileContent = readFileSTR(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "wpa_supplicant.conf"));
            }
            else if (File.Exists(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_wpa_supplicant.conf")))
            {
                chkEnableWiFi.Checked = false;
                chkEnableWiFi.Enabled = true;
                strFileContent = readFileSTR(Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_wpa_supplicant.conf"));
            }
            else
            {
                chkEnableWiFi.Checked = false;
                chkEnableWiFi.Enabled = false;
                strFileContent = "";
            };
            if (!String.IsNullOrEmpty(strFileContent))
            {
                txtWiFiCountry.Text = getINIValueSTR(strFileContent, strCountry_WiFi);
                txtWiFiSSID.Text = getINIQuotedValueSTR(strFileContent, strSSID_WiFi);
                txtWiFiPassword.Text = getINIQuotedValueSTR(strFileContent, strPassword_WiFi);
            };
        }

        private void saveWiFi()
        {
            String strFileWiFiEnabled = Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "wpa_supplicant.conf");
            String strFileWiFiDisabled = Path.Combine(Path.Combine(getControlText_TS(cmbMiSTerDir), "linux"), "_wpa_supplicant.conf");
            String strWiFi;

            if (chkEnableWiFi.Checked)
            {
                if (!File.Exists(strFileWiFiEnabled))
                {
                    if (File.Exists(strFileWiFiDisabled))
                    {
                        File.Move(strFileWiFiDisabled, strFileWiFiEnabled);
                    };
                }
                if (File.Exists(strFileWiFiEnabled))
                {
                    strWiFi = readFileSTR(strFileWiFiEnabled);
                    strWiFi = setINIValue(strWiFi, strCountry_WiFi, txtWiFiCountry.Text, false);
                    strWiFi = setINIQuotedValue(strWiFi, strSSID_WiFi, txtWiFiSSID.Text, false);
                    strWiFi = setINIQuotedValue(strWiFi, strPassword_WiFi, txtWiFiPassword.Text, false);
                    writeFileSTR(strFileWiFiEnabled, strWiFi);
                };
            }
            else
            {
                if (File.Exists(strFileWiFiEnabled))
                {
                    if (File.Exists(strFileWiFiDisabled))
                    {
                        File.Delete(strFileWiFiEnabled);
                    }
                    else
                    {
                        File.Move(strFileWiFiEnabled, strFileWiFiDisabled);
                    };
                };
            };
        }

        #endregion



        #region "Cores business logic"

        private void downloadCores(System.Windows.Forms.TreeNodeCollection Nodes, String currentDirectory, String workDirectory, bool removeArcadePrefix)
        {
            foreach (System.Windows.Forms.TreeNode objNode in Nodes)
            {
                if (!blnDownloadingCores) return;
                if (objNode.Tag == null || String.IsNullOrEmpty(objNode.Tag.ToString()))
                {
                    switch (objNode.Name)
                    {
                        case "computers---classic":
                            currentDirectory = Path.Combine(getControlText_TS(cmbMiSTerDir), getControlText_TS(txtComputerDir));
                            break;
                        case "consoles---classic":
                            currentDirectory = Path.Combine(getControlText_TS(cmbMiSTerDir), getControlText_TS(txtConsoleDir));
                            break;
                        case "other-systems":
                            currentDirectory = Path.Combine(getControlText_TS(cmbMiSTerDir), getControlText_TS(txtOtherDir));
                            break;                   
                        case "arcade-cores":
                            currentDirectory = Path.Combine(getControlText_TS(cmbMiSTerDir), getControlText_TS(txtArcadeDir));
                            break;
                        case "service-cores":
                            currentDirectory = Path.Combine(getControlText_TS(cmbMiSTerDir), getControlText_TS(txtUtilityDir));
                            break;
                    };
                    downloadCores(objNode.Nodes, currentDirectory, workDirectory, removeArcadePrefix);
                }
                else
                {
                    if (objNode.Checked) downloadCore(objNode.Text, objNode.Tag.ToString(), currentDirectory, workDirectory, removeArcadePrefix);
                };
            };
        }

        private delegate void delegateDownloadCores(System.Windows.Forms.TreeNodeCollection Nodes, String currentDirectory, String workDirectory, bool removeArcadePrefix);
        private void asyncDownloadCoresCallBack(IAsyncResult AsyncResult)
        {
            ((delegateDownloadCores)AsyncResult.AsyncState).EndInvoke(AsyncResult);
            enableCoresUI_TS(true);
            if (blnRunningWizard)
            {
                writeStatusLabel("Downloading Extras");
                asyncDownloadExtras(treeViewExtras.Nodes, getControlText_TS(cmbMiSTerDir));
            };
        }
        private void asyncDownloadCores(System.Windows.Forms.TreeNodeCollection Nodes, String currentDirectory, String workDirectory, bool removeArcadePrefix)
        {
            delegateDownloadCores objDelegateDownloadCores = new delegateDownloadCores(downloadCores);
            objDelegateDownloadCores.BeginInvoke(Nodes, currentDirectory, workDirectory, removeArcadePrefix, asyncDownloadCoresCallBack, objDelegateDownloadCores);
        }

        readonly Regex objRegExCoreReleasesURL = new Regex("/MiSTer-devel/[a-zA-Z0-9./_-]*/tree/[a-zA-Z0-9./_-]*/releases", RegexOptions.Compiled);
        readonly Regex objRegExCoreReleases = new Regex("/MiSTer-devel/.*/(?<FileName>(?<BaseName>[a-zA-Z0-9._-]*)_(?<TimeStamp>[0-9]{8}[a-zA-Z]?)(?<FileExtension>\\.rbf|\\.rar)?)", RegexOptions.Compiled);
        private void downloadCore(String coreName, String coreURL, String coreDirectory, String workDirectory, bool removeArcadePrefix)
        {
            String strReleases;
            Match objMaxReleaseMatch = null;
            Regex objRegExLocalFiles = null;
            Match objCurrentLocalFileMatch = null;
            Match objMaxLocalFileMatch = null;
            String strDestinationFile = null;
            String strDestinationDirectory = null;
            String strBaseName = null;

            writeStatusLabel_TS("Checking " + coreName);
            Application.DoEvents();
            try
            {
                strReleases = objWebClient.DownloadString(coreURL);
                strReleases = objRegExCoreReleasesURL.Match(strReleases).Value;
                strReleases = objWebClient.DownloadString(strGitHubURL + strReleases.Replace("/tree/", "/file-list/"));
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel_TS("Error checking " + coreName);
                System.Threading.Thread.Sleep(intErrorPause);
                return;
            };
            foreach (Match objMatch in objRegExCoreReleases.Matches(strReleases))
            {
                if ((coreName.CompareTo("Atari 800XL") != 0 || objMatch.Groups["BaseName"].Value.CompareTo("Atari800") == 0) && (coreName.CompareTo("Atari 5200") != 0 || objMatch.Groups["BaseName"].Value.CompareTo("Atari5200") == 0))
                {
                    if (objMaxReleaseMatch == null || objMatch.Groups["TimeStamp"].Value.CompareTo(objMaxReleaseMatch.Groups["TimeStamp"].Value) > 0)
                    {
                        objMaxReleaseMatch = objMatch;
                    };
                };
            };
            if (objMaxReleaseMatch != null)
            {
                strBaseName = objMaxReleaseMatch.Groups["BaseName"].Value;
                if (removeArcadePrefix && strBaseName.StartsWith("Arcade-")) strBaseName = strBaseName.Remove(0, 7);
                switch (strBaseName)
                {
                    case "MiSTer":
                    case "menu":
                        strDestinationDirectory = workDirectory;
                        break;
                    default:
                        strDestinationDirectory = coreDirectory;
                        break;
                };
                if (Directory.Exists(strDestinationDirectory))
                {
                    objRegExLocalFiles = new Regex(strBaseName + "_(?<TimeStamp>[0-9]{8}[a-zA-Z]?)" + objMaxReleaseMatch.Groups["FileExtension"].Value.Replace(".", "\\.") + "$");
                    foreach (String strFile in Directory.GetFiles(strDestinationDirectory, strBaseName + "*" + objMaxReleaseMatch.Groups["FileExtension"].Value))
                    {
                        objCurrentLocalFileMatch = objRegExLocalFiles.Match(strFile);
                        if (objCurrentLocalFileMatch != null && !String.IsNullOrEmpty(objCurrentLocalFileMatch.Value))
                        {
                            if (String.IsNullOrEmpty(objCurrentLocalFileMatch.Groups["TimeStamp"].Value))
                            {
                                File.Delete(strFile);
                            }
                            else
                            {
                                if (objMaxReleaseMatch.Groups["TimeStamp"].Value.CompareTo(objCurrentLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                                {
                                    File.Delete(strFile);
                                };
                                if (objMaxLocalFileMatch == null || objCurrentLocalFileMatch.Groups["TimeStamp"].Value.CompareTo(objMaxLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                                {
                                    objMaxLocalFileMatch = objCurrentLocalFileMatch;
                                };
                            };
                        };
                    };
                };
                if (objMaxLocalFileMatch == null || objMaxReleaseMatch.Groups["TimeStamp"].Value.CompareTo(objMaxLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                {
                    if (!Directory.Exists(strDestinationDirectory) && !CreateDirectorySafe(strDestinationDirectory)) return;

                    strDestinationFile = objMaxReleaseMatch.Groups["FileName"].Value;
                    if (removeArcadePrefix && strDestinationFile.StartsWith("Arcade-")) strDestinationFile = strDestinationFile.Remove(0, 7);
                    strDestinationFile = Path.Combine(strDestinationDirectory, strDestinationFile);
                    writeStatusLabel_TS("Downloading " + coreName);
                    Application.DoEvents();
                    try
                    {
                        objWebClient.DownloadFile(strGitHubURL + objMaxReleaseMatch.Value + "?raw=true", strDestinationFile);
                    }
                    catch (System.Exception ex)
                    {
                        writeLog(ex.ToString());
                        writeStatusLabel_TS("Error downloading " + coreName);
                        System.Threading.Thread.Sleep(intErrorPause);
                        return;
                    };
                    switch (strBaseName)
                    {
                        case "MiSTer":
                            if (File.Exists(Path.Combine(coreDirectory, strBaseName))) File.Delete(Path.Combine(coreDirectory, strBaseName));
                            File.Move(strDestinationFile, Path.Combine(coreDirectory, strBaseName));
                            createEmptyFile(strDestinationFile);
                            break;
                        case "menu":
                            if (File.Exists(Path.Combine(coreDirectory, strBaseName + objMaxReleaseMatch.Groups["FileExtension"].Value))) File.Delete(Path.Combine(coreDirectory, strBaseName + objMaxReleaseMatch.Groups["FileExtension"].Value));
                            File.Move(strDestinationFile, Path.Combine(coreDirectory, strBaseName + objMaxReleaseMatch.Groups["FileExtension"].Value));
                            createEmptyFile(strDestinationFile);
                            break;
                    };
                };
            };
        }

        #endregion



        #region "Extras business logic"

        private void downloadExtras(System.Windows.Forms.TreeNodeCollection Nodes, String currentDir)
        {
            String[] strExtraOptions = null;
            foreach (System.Windows.Forms.TreeNode objNode in Nodes)
            {
                if (!blnDownloadingExtras) return;
                if (objNode.Checked)
                {
                    strExtraOptions = objNode.Tag.ToString().Split('|');
                    switch (objNode.Name)
                    {
                        case "Cheats":
                            downloadCheats(strExtraOptions[0], strExtraOptions[1], Path.Combine(getControlText_TS(cmbMiSTerDir), strExtraOptions[2]), Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir));
                            break;
                        default:
                            if (objNode.Tag != null && !String.IsNullOrEmpty(objNode.Tag.ToString()))
                            {
                                downoadExtra(objNode.Text, strExtraOptions[0], strExtraOptions[1], Path.Combine(getControlText_TS(cmbMiSTerDir), strExtraOptions[2]));
                            };
                            break;
                    };
                };
            };
        }

        private delegate void delegateDownloadExtras(System.Windows.Forms.TreeNodeCollection Nodes, String currentDir);
        private void asyncDownloadExtrasCallBack(IAsyncResult AsyncResult)
        {
            ((delegateDownloadExtras)AsyncResult.AsyncState).EndInvoke(AsyncResult);
            enableExtrasUI_TS(true);
            if (blnRunningWizard) enableWizardUI_TS(true);
        }
        private void asyncDownloadExtras(System.Windows.Forms.TreeNodeCollection Nodes, String currentDir)
        {
            delegateDownloadExtras objDelegateDownloadExtras = new delegateDownloadExtras(downloadExtras);
            objDelegateDownloadExtras.BeginInvoke(Nodes, currentDir, asyncDownloadExtrasCallBack, objDelegateDownloadExtras);
        }

        private void downoadExtra(String extraName, String extraURL, String extraFilters, String extraDirectory)
        {
            String strReleases;
            Regex objRegExReleases = new Regex("href=\"(?<ExtraURL>[^\"]*/(?<ExtraFile>[^\"]*?(?:" + extraFilters.Replace(" ", "|") + ")))\".*?<td class=\"age\">.*?<time-ago datetime=\"(?<Year>\\d{4})-(?<Month>\\d{2})-(?<Day>\\d{2})T(?<Hour>\\d{2}):(?<Minute>\\d{2}):(?<Second>\\d{2})Z\"", RegexOptions.Singleline);
            String strLocalFileName;
            DateTime dtmReleaseDateTimeUTC;

            writeStatusLabel_TS("Checking " + extraName);
            extraURL = extraURL.Replace("/tree/master/", "/file-list/master/");
            if (!extraURL.Contains("/file-list/master")) extraURL = extraURL + "/file-list/master";
            try
            {
                strReleases = objWebClient.DownloadString(extraURL);
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel_TS("Error checking " + extraName);
                System.Threading.Thread.Sleep(intErrorPause);
                return;
            };
            foreach (Match objMatch in objRegExReleases.Matches(strReleases))
            {
                if (!blnDownloadingExtras) return;
                strLocalFileName = Path.Combine(extraDirectory, objMatch.Groups["ExtraFile"].Value);
                dtmReleaseDateTimeUTC = new DateTime(int.Parse(objMatch.Groups["Year"].Value), int.Parse(objMatch.Groups["Month"].Value), int.Parse(objMatch.Groups["Day"].Value), int.Parse(objMatch.Groups["Hour"].Value), int.Parse(objMatch.Groups["Minute"].Value), int.Parse(objMatch.Groups["Second"].Value));
                if (!File.Exists(strLocalFileName) || dtmReleaseDateTimeUTC.CompareTo(File.GetLastWriteTimeUtc(strLocalFileName)) > 0)
                {
                    writeStatusLabel_TS("Downloading " + objMatch.Groups["ExtraFile"].Value);
                    if (!Directory.Exists(extraDirectory) && !CreateDirectorySafe(extraDirectory)) return;
                    try
                    {
                        objWebClient.DownloadFile(strGitHubURL + objMatch.Groups["ExtraURL"].Value + "?raw=true", strLocalFileName);
                    }
                    catch (System.Exception ex)
                    {
                        writeLog(ex.ToString());
                        writeStatusLabel_TS("Error downloading " + objMatch.Groups["ExtraFile"].Value);
                        System.Threading.Thread.Sleep(intErrorPause);
                        return;
                    };
                };
            };
        }

        #endregion



        #region "Cheats business logic"

        readonly Regex objRegExCheatsReleases = new Regex("href=\"(?<CheatFile>mister_(?<CheatSystem>[^\"]*)_(?<TimeStamp>\\d{8})\\.zip)\"", RegexOptions.Singleline | RegexOptions.Compiled);
        private void downloadCheats(String cheatsURL, String cheatsMap, String cheatsDirectory, String workDirectory)
        {
            String strReleases;
            Regex objRegExLocalFiles = null;
            Match objCurrentLocalFileMatch = null;
            Match objMaxLocalFileMatch = null;
            String strDestinationFile = null;
            String strDestinationDirectory = null;
            System.Collections.Specialized.StringDictionary objCheatsMap = new System.Collections.Specialized.StringDictionary();

            foreach (String strCheatMap in cheatsMap.Split(' '))
            {
                objCheatsMap.Add(strCheatMap.Substring(0, strCheatMap.IndexOf(':')), strCheatMap.Substring(strCheatMap.IndexOf(':') + 1));
            };

            writeStatusLabel_TS("Checking Cheats");
            try
            {
                strReleases = objWebClient.DownloadString(cheatsURL);
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel_TS("Error checking Cheats");
                System.Threading.Thread.Sleep(intErrorPause);
                return;
            };
            foreach (Match objMatch in objRegExCheatsReleases.Matches(strReleases))
            {
                if (objCheatsMap.ContainsKey(objMatch.Groups["CheatSystem"].Value))
                {
                    objRegExLocalFiles = new Regex("(?<CheatFile>mister_" + objMatch.Groups["CheatSystem"].Value + "_(?<TimeStamp>\\d{8})\\.zip)", RegexOptions.Singleline);
                    objCurrentLocalFileMatch = null;
                    objMaxLocalFileMatch = null;
                    if (Directory.Exists(workDirectory))
                    {
                        foreach (String strFile in Directory.GetFiles(workDirectory, "mister_" + objMatch.Groups["CheatSystem"].Value + "*.zip"))
                        {
                            objCurrentLocalFileMatch = objRegExLocalFiles.Match(strFile);
                            if (objCurrentLocalFileMatch != null && !String.IsNullOrEmpty(objCurrentLocalFileMatch.Value))
                            {
                                if (String.IsNullOrEmpty(objCurrentLocalFileMatch.Groups["TimeStamp"].Value))
                                {
                                    File.Delete(strFile);
                                }
                                else
                                {
                                    if (objMatch.Groups["TimeStamp"].Value.CompareTo(objCurrentLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                                    {
                                        File.Delete(strFile);
                                    };
                                    if (objMaxLocalFileMatch == null || objCurrentLocalFileMatch.Groups["TimeStamp"].Value.CompareTo(objMaxLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                                    {
                                        objMaxLocalFileMatch = objCurrentLocalFileMatch;
                                    };
                                };
                            };
                        };
                    };
                    if (objMaxLocalFileMatch == null || objMatch.Groups["TimeStamp"].Value.CompareTo(objMaxLocalFileMatch.Groups["TimeStamp"].Value) > 0)
                    {
                        strDestinationFile = Path.Combine(workDirectory, objMatch.Groups["CheatFile"].Value);
                        writeStatusLabel_TS("Downloading " + objMatch.Groups["CheatFile"].Value);
                        if (!Directory.Exists(workDirectory) && !CreateDirectorySafe(workDirectory)) return;
                        try
                        {
                            objWebClient.DownloadFile(cheatsURL + objMatch.Groups["CheatFile"].Value, strDestinationFile);
                        }
                        catch (System.Exception ex)
                        {
                            writeLog(ex.ToString());
                            writeStatusLabel_TS("Error downloading " + objMatch.Groups["CheatFile"].Value);
                            System.Threading.Thread.Sleep(intErrorPause);
                            return;
                        };
                        writeStatusLabel_TS("Extracting " + objMatch.Groups["CheatFile"].Value);
                        strDestinationDirectory = Path.Combine(cheatsDirectory, objCheatsMap[objMatch.Groups["CheatSystem"].Value]);
                        if (!Directory.Exists(strDestinationDirectory) && !CreateDirectorySafe(strDestinationDirectory)) return;

                        // objFastZip.ExtractZip(strDestinationFile, strDestinationDirectory, null);
                        ExtractZipFile(strDestinationFile, "", strDestinationDirectory);
                        File.Delete(strDestinationFile);
                        createEmptyFile(strDestinationFile);
                    };
                };
            };
        }

        #endregion



        #region "Misc utils"

        private void createSDInstallerSemaphore(String baseDirectory, String workDirectory)
        {
            String semaphoreFile = null;
            if (File.Exists(Path.Combine(Path.Combine(baseDirectory, "linux"), "linux.img")))
            {
                semaphoreFile = Path.Combine(workDirectory, "release_" + File.GetLastWriteTimeUtc(Path.Combine(Path.Combine(baseDirectory, "linux"), "linux.img")).ToString("yyyyMMdd") + ".rar");
            }
            else
            {
                semaphoreFile = Path.Combine(workDirectory, "release_" + System.DateTime.Today.ToString("yyyyMMdd") + ".rar");
            };
            if (!File.Exists(semaphoreFile))
            {
                if (!Directory.Exists(workDirectory) && !CreateDirectorySafe(workDirectory)) return;
                createEmptyFile(semaphoreFile);
            };
        }

        private bool CreateDirectorySafe(String directoryName)
        {
            try
            {
                Directory.CreateDirectory(directoryName);
                return true;
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel_TS("Error creating " + directoryName);
                System.Threading.Thread.Sleep(intErrorPause);
                return false;
            };
        }

        private void createEmptyFile(String fileName)
        {
            FileStream objEmptyFile = new FileStream(fileName, FileMode.Create);
            objEmptyFile.Close();
        }

        readonly Regex objRegExZipNotValidChars = new Regex("[\\/:*?\"<>|]", RegexOptions.Compiled);
        private void ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
        {
            ICSharpCode.SharpZipLib.Zip.ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(archiveFilenameIn);
                zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs);
                if (!String.IsNullOrEmpty(password))
                {
                    zf.Password = password;		// AES encrypted entries are handled automatically
                }
                foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;			// Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];		// 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    String originalEntryFileName = entryFileName;
                    entryFileName = objRegExZipNotValidChars.Replace(entryFileName, "_");
                    if (entryFileName.CompareTo(originalEntryFileName) != 0) System.Diagnostics.Debug.Print("Invalid Zip Entry File Name: ZIP={0}, ENTRY={1}", archiveFilenameIn, originalEntryFileName);
                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }

        private void writeLog(String log)
        {
            StreamWriter objSW = new StreamWriter(Application.ExecutablePath + ".log", true);
            objSW.WriteLine("{0} - {1}", DateTime.Now.ToString(), log);
            objSW.Flush();
            objSW.Close();
        }

        private String readFileSTR(String fileName)
        {
            String strFile = "";
            StreamReader objSR = null;
            if (!File.Exists(fileName)) return "";
            objSR = new StreamReader(fileName);
            strFile = objSR.ReadToEnd();
            objSR.Close();
            return strFile;
        }

        private void writeFileSTR(String fileName, String fileContent)
        {
            StreamWriter objSW = null;
            //if (!File.Exists(fileName)) return;
            if (!Directory.Exists(Path.GetDirectoryName(fileName)) && !CreateDirectorySafe(Path.GetDirectoryName(fileName))) return;

            objSW = new StreamWriter(fileName);
            objSW.Write(fileContent);
            objSW.Close();
        }

        private String getINIValueSTR(String INI, String key)
        {
            Regex objRegEx = new Regex("^\\s*" + key + "\\s*=\\s*(?<Value>[a-zA-Z0-9%().,/_-]+)", RegexOptions.Multiline);
            Match objMatch = objRegEx.Match(INI);
            if (objMatch.Success)
            {
                return objMatch.Groups["Value"].Value;
            }
            else
            {
                return "";
            };
        }
        private String getINIQuotedValueSTR(String INI, String key)
        {
            Regex objRegEx = new Regex("^\\s*" + key + "\\s*=\\s*\"(?<Value>[^\"]+)\"", RegexOptions.Multiline);
            Match objMatch = objRegEx.Match(INI);
            if (objMatch.Success)
            {
                return objMatch.Groups["Value"].Value;
            }
            else
            {
                return "";
            };
        }
        private int getINIValueINT(String INI, String key)
        {
            String strValue = getINIValueSTR(INI, key);
            int intValue = -1;
            if (!String.IsNullOrEmpty(strValue))
            {
                int.TryParse(strValue, out intValue);
            };
            return intValue;
        }

        private String setINIValue(String INI, String key, String value, bool comment)
        {
            String strOutINI = INI;
            Regex objRegExUncommented = new Regex("^\\s*" + key + "\\s*=\\s*(?<Value>[a-zA-Z0-9%().,/_-]+)", RegexOptions.Multiline);
            Regex objRegExCommented = new Regex("^\\s*;\\s*" + key + "\\s*=", RegexOptions.Multiline);
            Match objMatch = objRegExUncommented.Match(strOutINI);
            if (!objMatch.Success)
            {
                strOutINI = objRegExCommented.Replace(strOutINI, key + "=", 1);
            };
            strOutINI = objRegExUncommented.Replace(strOutINI, ((comment) ? "; " : "") + key + "=" + value, 1);

            return strOutINI;
        }
        private String setINIQuotedValue(String INI, String key, String value, bool comment)
        {
            String strOutINI = INI;
            Regex objRegExUncommented = new Regex("^\\s*" + key + "\\s*=\\s*\"(?<Value>[^\"]+)\"", RegexOptions.Multiline);
            Regex objRegExCommented = new Regex("^\\s*;\\s*" + key + "\\s*=", RegexOptions.Multiline);
            Match objMatch = objRegExUncommented.Match(strOutINI);
            if (!objMatch.Success)
            {
                strOutINI = objRegExCommented.Replace(strOutINI, key + "=", 1);
            };
            strOutINI = objRegExUncommented.Replace(strOutINI, ((comment) ? "; " : "") + key + "=\"" + value + "\"", 1);

            return strOutINI;
        }

        private bool isMacOSX()
        {
            String strUname = "";

            System.Diagnostics.Process objProcess = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = "uname",
                    Arguments = "-s"
                }
            };
            try
            {
                objProcess.Start();
                strUname = objProcess.StandardOutput.ReadToEnd().Trim();
            }
            catch { };
            objProcess.Dispose();

            if (strUname.CompareTo("Darwin")==0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool testLinuxCertificates()
        {
            String strCertBundle = "";
            System.Diagnostics.Process objProcess;

            try
            {
                objWebClient.DownloadString(strInternetTestURL);
                return true;
            }
            catch
            {
                if (File.Exists(Path.Combine(Application.StartupPath, "cert-sync")))
                {
                    if (File.Exists("/etc/ssl/certs/ca-certificates.crt"))
                    {
                        strCertBundle = "/etc/ssl/certs/ca-certificates.crt";
                    }
                    else if (File.Exists("/etc/pki/tls/certs/ca-bundle.crt"))
                    {
                        strCertBundle = "/etc/pki/tls/certs/ca-bundle.crt";
                    };
                    if (!String.IsNullOrEmpty(strCertBundle))
                    {
                        objProcess = new System.Diagnostics.Process
                        {
                            StartInfo =
                            {
                                UseShellExecute = false,
                                RedirectStandardOutput = false,
                                FileName = Path.Combine(Application.StartupPath, "cert-sync"),
                                Arguments = "--user " + strCertBundle
                            }
                        };
                        try
                        {
                            objProcess.Start();
                            objProcess.WaitForExit();
                        }
                        catch { };
                        try
                        {
                            objWebClient.DownloadString(strInternetTestURL);
                            return true;
                        }
                        catch { };
                    };
                }
            };
            return false;
        }

        #endregion



        #region "UI methods"

        private void initializeConfigurator()
        {
            if (!blnInitialized)
            {
                cmbVideoMode.SelectedIndex = 0;
                cmbScalingMode.SelectedIndex = 0;
                cmbVSyncMode.SelectedIndex = 0;

                readSamba();
                readWiFi();

                try
                {
                    writeStatusLabel_TS("Downloading MiSTer.ini");
                    strMiSTerINI_GitHub = objWebClient.DownloadString(strMiSTerINIURL + "?raw=true");
                }
                catch (System.Exception ex)
                {
                    writeLog(ex.ToString());
                    writeStatusLabel_TS("Error downloading MiSTer.ini");
                    System.Threading.Thread.Sleep(intErrorPause);
                    return;
                };
                this.cmbMiSTerDir_TextChanged(null, null);
                this.cmbMiSTerDir.TextChanged += new System.EventHandler(this.cmbMiSTerDir_TextChanged);


                treeViewExtras.ExpandAll();
                foreach (System.Windows.Forms.TreeNode objNode in treeViewExtras.Nodes)
                {
                    objNode.Checked = true;
                };
                treeViewCores.ExpandAll();
                loadCoresTree();

                foreach (System.Windows.Forms.TreeNode objNode in treeViewCores.Nodes)
                {
                    objNode.Checked = true;
                };

                btn1ClickSetup.Enabled = true;
                btnAdvanced.Enabled = true;
                writeStatusLabel_TS("Ready");

                blnInitialized = true;
            };
        }

        private void hideTabHeaders()
        {
            tabControlSections.Appearance = TabAppearance.FlatButtons;
            tabControlSections.ItemSize = new Size(0, 1);
            tabControlSections.SizeMode = TabSizeMode.Fixed;
        }

        System.Windows.Forms.TabAppearance tabControlSectionsOriginalAppearance;
        Size tabControlSectionsOriginalSize;
        System.Windows.Forms.TabSizeMode tabControlSectionsSizeMode;
        bool blnAdvancedMode = true;
        private void enableAdvancedMode(bool enable)
        {
            if (enable)
            {
                if (!blnAdvancedMode)
                {

                    tabControlSections.Appearance = tabControlSectionsOriginalAppearance;
                    tabControlSections.ItemSize = tabControlSectionsOriginalSize;
                    tabControlSections.SizeMode = tabControlSectionsSizeMode;
                    blnAdvancedMode = true;
                };
            }
            else
            {
                if (blnAdvancedMode)
                {
                    tabControlSectionsOriginalAppearance = tabControlSections.Appearance;
                    tabControlSectionsOriginalSize = tabControlSections.ItemSize;
                    tabControlSectionsSizeMode = tabControlSections.SizeMode;
                    tabControlSections.Appearance = TabAppearance.FlatButtons;
                    tabControlSections.ItemSize = new Size(0, 1);
                    tabControlSections.SizeMode = TabSizeMode.Fixed;
                    blnAdvancedMode = false;
                }
            };
        }

        readonly Regex objRegExCoresTree = new Regex("(?:(?<CoreURL>" + strGitHubURL + "/[a-zA-Z0-9./_-]*_MiSTer)\">(?<CoreName>.*?)<)|(?:user-content-(?<CoreCategory>[a-z-]*))", RegexOptions.Compiled);
        private void loadCoresTree()
        {
            String strMiSTerWiki = null;
            System.Windows.Forms.TreeNode objCategoryNode = null;

            writeStatusLabel("Loading cores list");
            Application.DoEvents();

            try
            {
                strMiSTerWiki = objWebClient.DownloadString(strMiSTerWikiURL);
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel("Error downloading MiSTer wiki");
                System.Threading.Thread.Sleep(intErrorPause);
                return;
            };
            strMiSTerWiki = strMiSTerWiki.Substring(strMiSTerWiki.IndexOf("user-content-computers---classic"));
            strMiSTerWiki = strMiSTerWiki.Substring(0, strMiSTerWiki.IndexOf("user-content-development"));
            foreach (Match objMatch in objRegExCoresTree.Matches(strMiSTerWiki))
            {
                if (!String.IsNullOrEmpty(objMatch.Groups["CoreCategory"].Value))
                {
                    objCategoryNode = treeViewCores.Nodes.Find(objMatch.Groups["CoreCategory"].Value, true)[0];
                }
                else
                {
                    if (!objMatch.Groups["CoreURL"].Value.EndsWith("/Menu_MiSTer"))
                    {
                        objCategoryNode.Nodes.Add(objMatch.Groups["CoreName"].Value).Tag = objMatch.Groups["CoreURL"].Value;
                    };
                };
            };

            //HACK TO LOAD ARCADE CORES

            try
            {
                strMiSTerWiki = objWebClient.DownloadString(strMiSTerArcadeWikiURL);
            }
            catch (System.Exception ex)
            {
                writeLog(ex.ToString());
                writeStatusLabel("Error downloading MiSTer Arcade wiki");
                System.Threading.Thread.Sleep(intErrorPause);
                return;
            };
            strMiSTerWiki = strMiSTerWiki.Substring(strMiSTerWiki.IndexOf("wiki-content"));
            strMiSTerWiki = strMiSTerWiki.Substring(0, strMiSTerWiki.IndexOf("wiki-footer"));

            objCategoryNode = treeViewCores.Nodes.Find("arcade-cores", true)[0];
            foreach (Match objMatch in objRegExCoresTree.Matches(strMiSTerWiki))
            {
                if (objMatch.Groups["CoreURL"].Value.EndsWith("_MiSTer"))
                {
                    objCategoryNode.Nodes.Add(objMatch.Groups["CoreName"].Value).Tag = objMatch.Groups["CoreURL"].Value;
                };
                //};
            };

            writeStatusLabel("Ready");
        }

        #region "UI thread-safe methods"

        private void enableWizardUI(bool enable)
        {
            btn1ClickSetup.Text = (enable) ? strUIWizardButtonLabel : strUIStopLabel;
            cmbVideoMode.Enabled = enable;
            chkEnablePAL_NTSC.Enabled = enable;
            cmbScalingMode.Enabled = enable;
            cmbVSyncMode.Enabled = enable;
            btnCompatibilityPreset.Enabled = enable;
            btnOptimalPreset.Enabled = enable;
            chkEnableSamba.Enabled = enable;
            pnlSamba.Enabled = enable;
            chkEnableWiFi.Enabled = enable;
            pnlWiFi.Enabled = enable;
            btnAdvanced.Enabled = enable;
            if (enable) writeStatusLabel("Ready");
            Application.DoEvents();
        }
        private delegate void delegateEnableWizardUI(bool enable);
        private void enableWizardUI_TS(bool enable)
        {
            this.Invoke(new delegateEnableWizardUI(enableWizardUI), enable);
        }

        private void enableCoresUI(bool enable)
        {
            btnDownloadCores.Text = (enable) ? strUICoresButtonLabel : strUIStopLabel;
            treeViewCores.Enabled = enable;
            txtComputerDir.Enabled = enable;
            txtConsoleDir.Enabled = enable;
            txtArcadeDir.Enabled = enable;
            txtUtilityDir.Enabled = enable;
            cmbMiSTerDir.Enabled = enable;
            if (enable) writeStatusLabel("Ready");
            Application.DoEvents();
        }
        private delegate void delegateEnableCoresUI(bool enable);
        private void enableCoresUI_TS(bool enable)
        {
            this.Invoke(new delegateEnableCoresUI(enableCoresUI), enable);
        }

        private void enableExtrasUI(bool enable)
        {
            btnDownloadExtras.Text = (enable) ? strUIExtrasButtonLabel : strUIStopLabel;
            treeViewExtras.Enabled = enable;
            cmbMiSTerDir.Enabled = enable;
            if (enable) writeStatusLabel("Ready");
            Application.DoEvents();
        }
        private delegate void delegateEnableExtrasUI(bool enable);
        private void enableExtrasUI_TS(bool enable)
        {
            this.Invoke(new delegateEnableExtrasUI(enableExtrasUI), enable);
        }

        private delegate String delegateGetControlText(System.Windows.Forms.Control control);
        private String getControlText(System.Windows.Forms.Control control)
        {
            return control.Text;
        }
        private String getControlText_TS(System.Windows.Forms.Control control)
        {
            return this.Invoke(new delegateGetControlText(getControlText), control).ToString();
        }

        private delegate bool delegateGetControlChecked(System.Windows.Forms.Control control);
        private bool getControlChecked(System.Windows.Forms.Control control)
        {
            return ((System.Windows.Forms.CheckBox)control).Checked;
        }
        private bool getControlChecked_TS(System.Windows.Forms.Control control)
        {
            return (bool)this.Invoke(new delegateGetControlChecked(getControlChecked), control);
        }

        private void writeStatusLabel(String labelText)
        {
            toolStripStatusLabel1.Text = labelText;
            Application.DoEvents();
        }
        private delegate void delegateWriteStatusLabel(String labelText);
        private void writeStatusLabel_TS(String labelText)
        {
            this.Invoke(new delegateWriteStatusLabel(writeStatusLabel), labelText);
        }

        #endregion

        #endregion



        #region "UI events handling"

        private void ConfiguratorForm_Load(object sender, EventArgs e)
        {
            const System.Security.Authentication.SslProtocols _Tls12 = (System.Security.Authentication.SslProtocols)0x00000C00; const System.Net.SecurityProtocolType Tls12 = (System.Net.SecurityProtocolType)_Tls12; System.Net.ServicePointManager.SecurityProtocol = Tls12;
            // hideTabHeaders();
            enableAdvancedMode(false);

            strUIWizardButtonLabel = btn1ClickSetup.Text;
            strUICoresButtonLabel = btnDownloadCores.Text;
            strUIExtrasButtonLabel = btnDownloadExtras.Text;

            if (isMacOSX()) bytOS = enmOS.MacOS;
            else if (Environment.OSVersion.Platform == PlatformID.Unix) bytOS = enmOS.Linux;
            else bytOS = enmOS.Windows;
            switch (bytOS)
            {
                case enmOS.MacOS:
                    linkLabelUnixInstaller.Text = strMacOSInstaller.Split('|')[0];
                    linkLabelUnixInstaller.Tag = strMacOSInstaller.Split('|')[1] + "|" + strMacOSInstaller.Split('|')[2];
                    linkLabelUnixInstaller.Visible = true;
                    break;
                case enmOS.Linux:
                    linkLabelUnixInstaller.Text = strLinuxInstaller.Split('|')[0];
                    linkLabelUnixInstaller.Tag = strLinuxInstaller.Split('|')[1] + "|" + strLinuxInstaller.Split('|')[2];
                    linkLabelUnixInstaller.Visible = true;
                    testLinuxCertificates();
                    break;
                default:
                    linkLabelUnixInstaller.Visible = false;
                    break;
            };


            if (!String.IsNullOrEmpty(strMiSTerDir_constructor)) btnMiSTerDirRefresh.Enabled = false;
            btnMiSTerDirRefresh_Click(null, null);

            timer1.Enabled = true;
        }

        private void ConfiguratorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btn1ClickSetup.Text.CompareTo(strUIStopLabel)==0) btn1ClickSetup_Click(null, null);
            if (btnDownloadCores.Text.CompareTo(strUIStopLabel)==0) btnDownloadCores_Click(null, null);
            if (btnDownloadExtras.Text.CompareTo(strUIStopLabel)==0) btnDownloadExtras_Click(null, null);
        }

        private void btnMiSTerDirRefresh_Click(object sender, EventArgs e)
        {
            cmbMiSTerDir.Items.Clear();

            if (!String.IsNullOrEmpty(strMiSTerDir_constructor))
            {
                cmbMiSTerDir.Items.Add(strMiSTerDir_constructor);
            };
            if (blnMiSTerDir_locked)
            {
                cmbMiSTerDir.Enabled = false;
            }
            else
            {
                cmbMiSTerDir.Items.Add(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), strLocalFilesDir));
                cmbMiSTerDir.SelectedIndex = cmbMiSTerDir.Items.Count - 1;
                foreach (DriveInfo objDrive in DriveInfo.GetDrives())
                {
                    if (objDrive.DriveType == DriveType.Removable && objDrive.IsReady)
                    {
                        cmbMiSTerDir.Items.Add(objDrive.RootDirectory.Name);
                        if (File.Exists(Path.Combine(objDrive.RootDirectory.Name, "MiSTer")) && File.Exists(Path.Combine(objDrive.RootDirectory.Name, "menu.rbf")) && File.Exists(Path.Combine(Path.Combine(objDrive.RootDirectory.Name, "linux"), "linux.img")))
                        {
                            cmbMiSTerDir.SelectedIndex = cmbMiSTerDir.Items.Count - 1;
                        };
                    };
                };
            };
            if (!String.IsNullOrEmpty(strMiSTerDir_constructor))
            {
                cmbMiSTerDir.SelectedIndex = 0;
            };
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            initializeConfigurator();
        }

        private void linkLabelMiSTerWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(strMiSTerURL + "/wiki");
        }

        private void cmbMiSTerDir_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(getControlText_TS(cmbMiSTerDir), "MiSTer.ini")))
            {
                strMiSTerINI_Current = readFileSTR(Path.Combine(getControlText_TS(cmbMiSTerDir), "MiSTer.ini"));
            }
            else
            {
                strMiSTerINI_Current = strMiSTerINI_GitHub;
            };
            readMiSTerINI(strMiSTerINI_Current);
        }



        #region "Wizard tab"

        private void cmbVideoMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbVideoMode.SelectedIndex)
            {
                case 0:
                case 7:
                case 8:
                case 9:
                    chkEnablePAL_NTSC.Enabled = true;
                    break;
                default:
                    chkEnablePAL_NTSC.Checked = false;
                    chkEnablePAL_NTSC.Enabled = false;
                    break;
            }
        }

        private void chkEnableSamba_CheckedChanged(object sender, EventArgs e)
        {
            pnlSamba.Enabled = chkEnableSamba.Checked;
        }

        private void btnCompatibilityPreset_Click(object sender, EventArgs e)
        {
            cmbVideoMode.SelectedIndex = 0;
            chkEnablePAL_NTSC.Checked = false;
            // cmbScalingMode.SelectedIndex = 0;
            cmbVSyncMode.SelectedIndex = 0;
        }

        private void btnOptimalPreset_Click(object sender, EventArgs e)
        {
            cmbVideoMode.SelectedIndex = 8;
            chkEnablePAL_NTSC.Checked = true;
            // cmbScalingMode.SelectedIndex = 0;
            cmbVSyncMode.SelectedIndex = 2;
        }

        private void chkEnableWiFi_CheckedChanged(object sender, EventArgs e)
        {
            pnlWiFi.Enabled = chkEnableWiFi.Checked;
        }

        private void btn1ClickSetup_Click(object sender, EventArgs e)
        {
            if (btn1ClickSetup.Text.CompareTo(strUIStopLabel)!=0)
            {
                writeStatusLabel_TS("Saving Configuration");
                saveMiSTerINI(ref strMiSTerINI_Current, Path.Combine(getControlText_TS(cmbMiSTerDir), "MiSTer.ini"));
                saveSamba();
                saveWiFi();
                enableWizardUI(false);
                enableCoresUI(false);
                enableExtrasUI(false);
                createSDInstallerSemaphore(getControlText_TS(cmbMiSTerDir), Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir));
                writeStatusLabel("Downloading cores");
                blnDownloadingCores = true;
                blnDownloadingExtras = true;
                blnRunningWizard = true;
                asyncDownloadCores(treeViewCores.Nodes, cmbMiSTerDir.Text, Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir), getControlChecked_TS(chkRemoveArcadePrefix));
            }
            else
            {
                blnDownloadingCores = false;
                blnDownloadingExtras = false;
            };
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            enableAdvancedMode(!blnAdvancedMode);
        }

        private void linkLabelUnixInstaller_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bool blnWasAlreadyDownloadingExtras = blnDownloadingExtras;

            String strRepositoryURL = linkLabelUnixInstaller.Tag.ToString().Split('|')[0];
            String strFile = linkLabelUnixInstaller.Tag.ToString().Split('|')[1];
            System.Diagnostics.Process.Start(strRepositoryURL);
            if (!blnWasAlreadyDownloadingExtras) blnDownloadingExtras = true;
            downoadExtra(getControlText_TS(linkLabelUnixInstaller), strRepositoryURL, strFile, Application.StartupPath);
            if (!blnWasAlreadyDownloadingExtras) blnDownloadingExtras = false;
            writeStatusLabel("Ready");
        }

        #endregion



        #region "Core tab"

        private void treeViewCores_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null && !String.IsNullOrEmpty(e.Node.Tag.ToString()))
            {
                System.Diagnostics.Process.Start(e.Node.Tag.ToString());
            };
        }

        private void treeViewCores_AfterCheck(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Name)
            {
                case "root":
                case "computers---classic":
                case "consoles---classic":
                case "other-systems":
                case "arcade-cores":
                case "service-cores":
                    foreach (System.Windows.Forms.TreeNode objNode in e.Node.Nodes)
                        objNode.Checked = e.Node.Checked;
                    break;
            };
        }

        private void btnDownloadCores_Click(object sender, EventArgs e)
        {
            if (btnDownloadCores.Text.CompareTo("Download")==0)
            {
                enableCoresUI(false);
                createSDInstallerSemaphore(getControlText_TS(cmbMiSTerDir), Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir));
                writeStatusLabel("Downloading cores");
                blnDownloadingCores = true;
                asyncDownloadCores(treeViewCores.Nodes, cmbMiSTerDir.Text, Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir), getControlChecked_TS(chkRemoveArcadePrefix));
            }
            else
            {
                blnDownloadingCores = false;
            };
        }

        #endregion



        #region "Extras tab"

        private void treeViewExtras_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null && !String.IsNullOrEmpty(e.Node.Tag.ToString()))
            {
                System.Diagnostics.Process.Start(e.Node.Tag.ToString().Split('|')[0]);
            };
        }

        private void btnDownloadExtras_Click(object sender, EventArgs e)
        {
            if (btnDownloadExtras.Text.CompareTo("Download")==0)
            {
                enableExtrasUI(false);
                createSDInstallerSemaphore(getControlText_TS(cmbMiSTerDir), Path.Combine(getControlText_TS(cmbMiSTerDir), strWorkDir));
                writeStatusLabel("Downloading Extras");
                blnDownloadingExtras = true;
                asyncDownloadExtras(treeViewExtras.Nodes, cmbMiSTerDir.Text);
            }
            else
            {
                blnDownloadingExtras = false;
            };
        }

        #endregion

        #endregion


    }
}
