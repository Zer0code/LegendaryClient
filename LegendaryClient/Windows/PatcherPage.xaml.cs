using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using LegendaryClient.Logic;
using LegendaryClient.Logic.JSON;
using LegendaryClient.Logic.Patcher;
using LegendaryClient.Logic.SQLite;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;
using RAFlibPlus;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.Win32;
using System.Windows.Media;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for PatcherPage.xaml
    /// </summary>
    public partial class PatcherPage : Page
    {
        //#FF2E2E2E
        internal static bool LoLDataIsUpToDate = false;
        internal static string LatestLolDataVersion = "";
        internal static string LolDataVersion = "";
        public PatcherPage()
        {
            InitializeComponent();
            bool x = Properties.Settings.Default.DarkTheme;
            if (!x)
            {
                var bc = new BrushConverter();
                PatchTextBox.Background = (Brush)bc.ConvertFrom("#FFECECEC");
                DevKey.Background = (Brush)bc.ConvertFrom("#FFECECEC");
                PatchTextBox.Foreground = (Brush)bc.ConvertFrom("#FF1B1919");
            }
            DevKey.TextChanged += DevKey_TextChanged;
            StartPatcher();
            Client.Log("LegendaryClient Started Up Successfully");
        }

        void DevKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DevKey.Text == "!~devmode=true")
                Client.Dev = true;
            else if (DevKey.Text == "!~devmode=false")
                Client.Dev = false;
            if (DevKey.Text.Contains("!~devmode="))
                return;
            if (DevKey.Text.StartsWith("!~betakey-"))
                if (DevKey.Text.EndsWith("~!"))
                    GetDev(DevKey.Text.Replace("!~betakey-", "").Replace("~!", ""));
        }

        private void GetDev(string DevKey)
        {
            if (DevKey.Length != 20)
                return;
            bool Auth = false;
            var client = new WebClient();
            string KeyPlayer = client.DownloadString("http://eddy5641.github.io/LegendaryClient/BetaUsers");
            string[] Players = KeyPlayer.Split(new string[] { Environment.NewLine }, 0, StringSplitOptions.RemoveEmptyEntries);
            foreach (string Beta in Players)
            {
                string[] BetaKey = Beta.Split(',');
                if (DevKey == BetaKey[0])
                {
                    Auth = true;
                    Welcome.Text = "Welcome " + BetaKey[1];
                    Welcome.Visibility = Visibility.Visible;
                }
            }
            if (Auth == false)
            {
                this.DevKey.Text = "";
            }

        }

        private void DevSkip_Click(object sender, RoutedEventArgs e)
        {
            Client.SwitchPage(new LoginPage());
            Client.Log("Swiched to LoginPage with DevSkip");
        }

        private void SkipPatchButton_Click(object sender, RoutedEventArgs e)
        {
            Client.SwitchPage(new LoginPage());
        }

        private void StartPatcher()
        {
            try
            {

                Thread bgThead = new Thread(() =>
                {
                    LogTextBox("Starting Patcher");

                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadDDragon);
                    client.DownloadProgressChanged += (o, e) =>
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            double bytesIn = double.Parse(e.BytesReceived.ToString());
                            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                            double percentage = bytesIn / totalBytes * 100;
                            CurrentProgressLabel.Content = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
                            CurrentProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
                        }));
                    };

                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        TotalProgressLabel.Content = "20%";
                        TotalProgessBar.Value = 20;
                    }));
                    #region idk

                    client = new WebClient();
                    if (!File.Exists(Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp3"))) client.DownloadFile(new Uri("http://eddy5641.github.io/LegendaryClient/Login/Login.mp3"), Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp3"));
                    if (!File.Exists(Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp4"))) client.DownloadFile(new Uri("http://eddy5641.github.io/LegendaryClient/Login/Login.mp4"), Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp4"));
                    #endregion idk

                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    RiotPatcher patcher = new RiotPatcher();
                    

                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        TotalProgressLabel.Content = "40%";
                        TotalProgessBar.Value = 40;
                    }));

                    // Try get LoL path from registry

                    //A string that looks like C:\Riot Games\League of Legends\
                    string lolRootPath = GetLolRootPath(false);

                    #region lol_air_client

                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets"))) Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets"));

                    if (!File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                    {
                        var VersionAIR = File.Create(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                        VersionAIR.Write(encoding.GetBytes("0.0.0.0"), 0, encoding.GetBytes("0.0.0.0").Length);
                        VersionAIR.Close();
                    }

                    string LatestAIR = patcher.GetLatestAir();
                    LogTextBox("Air Assets Version: " + LatestAIR);
                    string AirVersion = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                    LogTextBox("Current Air Assets Version: " + AirVersion);
                    WebClient UpdateClient = new WebClient();
                    string Release = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA");
                    string[] LatestVersion = Release.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    var vers = LatestVersion[0];
                    if (AirVersion != LatestVersion[0])
                    {
                        //Download Air Assists from riot
                        try
                        {
                            string Package = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/packages/files/packagemanifest");
                            UpdateClient.DownloadFile(new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/files/assets/data/gameStats/gameStats_en_US.sqlite"), Path.Combine(Client.ExecutingDirectory, "Client", "gameStats_en_US.sqlite"));
                            GetAllPngs(Package);
                            string[] x = Package.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            if (File.Exists(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                                File.Delete(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR"));
                            using (var file = File.Create(System.IO.Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")))
                            {
                                file.Write(encoding.GetBytes(LatestVersion[0]), 0, encoding.GetBytes(LatestVersion[0]).Length);
                            }
                        }
                        catch (Exception e)
                        {
                            Client.Log(e.Message);
                        }
                    }

                    if (AirVersion != LatestAIR)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            SkipPatchButton.IsEnabled = true;
                            CurrentProgressLabel.Content = "Retrieving Air Assets";
                        }));
                    }

                    #endregion lol_air_client


                    //string GameVersion = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "RADS", "VERSION_LOL"));
                    #region lol_game_client
                    LogTextBox("Trying to detect League of Legends GameClient");
                    LogTextBox("League of Legends is located at: " + lolRootPath);
                    //RADS\solutions\lol_game_client_sln\releases
                    var GameLocation = Path.Combine(lolRootPath, "RADS", "solutions", "lol_game_client_sln", "releases");

                    string LolVersion2 = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_game_client/releases/releaselisting_NA");
                    string LolVersion = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA");
                    string GameClientSln = LolVersion.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    string GameClient = LolVersion2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    LogTextBox("Latest League of Legends GameClient: " + GameClientSln);
                    LogTextBox("Checking if League of Legends is Up-To-Date");

                    string LolLauncherVersion = new WebClient().DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA");
                    string LauncherVersion = LolLauncherVersion.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    bool toExit = false;

                    if (Directory.Exists(Path.Combine(GameLocation, GameClientSln)))
                    {
                        LogTextBox("League of Legends is Up-To-Date");
                        Client.LOLCLIENTVERSION = LolVersion2;
                        Client.Location = Path.Combine(lolRootPath, "RADS", "solutions", "lol_game_client_sln", "releases", GameClientSln, "deploy");
                        Client.LoLLauncherLocation = Path.Combine(lolRootPath, "RADS", "projects", "lol_air_client", "releases", LauncherVersion, "deploy");
                        Client.RootLocation = lolRootPath;
                    }
                    else
                    {
                        LogTextBox("League of Legends is not Up-To-Date. Please Update League Of Legends");
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            SkipPatchButton.IsEnabled = true;
                            FindClientButton.Visibility = Visibility.Visible;
                        }));
                        toExit = true;
                    }
                    #endregion lol_game_client

                    if (!toExit)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                        {
                            string Package = UpdateClient.DownloadString("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/packages/files/packagemanifest");
                            try
                            {
                                UpdateClient.DownloadFile(new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[0] + "/files/assets/data/gameStats/gameStats_en_US.sqlite"), Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));
                            }
                            catch
                            {
                                try
                                {
                                    UpdateClient.DownloadFile(new Uri("http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/" + LatestVersion[1] + "/files/assets/data/gameStats/gameStats_en_US.sqlite"), Path.Combine(Client.ExecutingDirectory, "gameStats_en_US.sqlite"));
                                }
                                catch
                                {
                                    Client.Log("Unable to update gamestats file. Perhaps a different LegendaryClient is running?", "Small Error");
                                }
                            }
                            TotalProgressLabel.Content = "100%";
                            TotalProgessBar.Value = 100;
                            SkipPatchButton.Content = "Play";
                            CurrentProgressLabel.Content = "Finished Patching";
                            CurrentStatusLabel.Content = "Ready To Play";
                            SkipPatchButton.IsEnabled = true;
                            SkipPatchButton_Click(null, null);
                        }));

                        LogTextBox("LegendaryClient Has Finished Patching");
                    }
                });

                bgThead.IsBackground = true;
                bgThead.Start();
            }
            catch (Exception e)
            {
                Client.Log(e.Message + " - in PatcherPage updating progress.");
            }
        }

        private string GetLolRootPath(bool restart)
        {
            if (!restart)
            {
                var possiblePaths = new List<Tuple<string, string>>  
                {
                    new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Classes\VirtualStore\MACHINE\SOFTWARE\RIOT GAMES", "Path"),
                    new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Classes\VirtualStore\MACHINE\SOFTWARE\Wow6432Node\RIOT GAMES", "Path"),
                    new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\RIOT GAMES", "Path"),
                    new Tuple<string, string>(@"HKEY_CURRENT_USER\Software\Wow6432Node\Riot Games", "Path"),
                    new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Riot Games\League Of Legends", "Path"),
                    new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games", "Path"),
                    new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games\League Of Legends", "Path"),
                    // Yes, a f*ckin whitespace after "Riot Games"..
                    new Tuple<string, string>(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Riot Games \League Of Legends", "Path"),
                };
                foreach (var tuple in possiblePaths)
                {
                    var path = tuple.Item1;
                    var valueName = tuple.Item2;
                    try
                    {
                        var value = Registry.GetValue(path, valueName, string.Empty);
                        if (value != null && value.ToString() != string.Empty)
                        {
                            return value.ToString();
                        }
                    }
                    catch { }
                }
            }

            OpenFileDialog FindLeagueDialog = new OpenFileDialog();

            if (!Directory.Exists(Path.Combine("C:\\", "Riot Games", "League of Legends")))
            {
                FindLeagueDialog.InitialDirectory = Path.Combine("C:\\", "Program Files (x86)", "GarenaLoL", "GameData", "Apps", "LoL");
            }
            else
            {
                FindLeagueDialog.InitialDirectory = Path.Combine("C:\\", "Riot Games", "League of Legends");
            }
            FindLeagueDialog.DefaultExt = ".exe";
            FindLeagueDialog.Filter = "League of Legends Launcher|lol.launcher*.exe|Garena Launcher|lol.exe";

            Nullable<bool> result = FindLeagueDialog.ShowDialog();
            if (result == true)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\RIOT GAMES");
                key.SetValue("Path", FindLeagueDialog.FileName.Replace("lol.launcher.exe", "").Replace("lol.launcher.admin.exe", ""));
                if(restart) LogTextBox("Saved value, please restart the client to login.");
                return FindLeagueDialog.FileName.Replace("lol.launcher.exe", "").Replace("lol.launcher.admin.exe", "");
            }
            else
                return string.Empty;
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                CurrentProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
                CurrentProgressLabel.Content = "Now downloading LegendaryClient";
            }));
        }

        void client_DownloadDDragon(object sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                CurrentProgressLabel.Content = "Download Completed";
                LogTextBox("Finished Download");
                CurrentProgressBar.Value = 0;
            }));
        }

        private void LogTextBox(string s)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                PatchTextBox.Text += "[" + DateTime.Now.ToShortTimeString() + "] " + s + Environment.NewLine;
                PatchTextBox.ScrollToEnd();
            }));
            Client.Log(s);
        }

        private void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        private void DeleteDirectoryRecursive(string path)
        {
            foreach (var directory in Directory.GetDirectories(path))
            {
                DeleteDirectoryRecursive(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        public void uncompressFile(string inFile, string outFile)
        {
            try
            {
                int data = 0;
                int stopByte = -1;
                System.IO.FileStream outFileStream = new System.IO.FileStream(outFile, System.IO.FileMode.Create);
                ZInputStream inZStream = new ZInputStream(System.IO.File.Open(inFile, System.IO.FileMode.Open, System.IO.FileAccess.Read));
                while (stopByte != (data = inZStream.Read()))
                {
                    byte _dataByte = (byte)data;
                    outFileStream.WriteByte(_dataByte);
                }

                inZStream.Close();
                outFileStream.Close();
            }
            catch
            {
                Client.Log("Unable to find a file to uncompress");
            }

        }

        private void GetAllPngs(string PackageManifest)
        {
            string[] FileMetaData = PackageManifest.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Skip(1).ToArray();
            Version currentVersion = new Version(File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "VERSION_AIR")));
            foreach (string s in FileMetaData)
            {
                if (String.IsNullOrEmpty(s))
                {
                    continue;
                }
                //Remove size and type metadata
                string Location = s.Split(',')[0];
                //Get save position
                Version version = new Version(Location.Split(new string[] { "/releases/", "/files/" }, StringSplitOptions.None)[1]);
                if (version > currentVersion)
                {
                    string SavePlace = Location.Split(new string[] { "/files/" }, StringSplitOptions.None)[1];
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "champions")))
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "champions"));
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "passive")))
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "passive"));
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "spell")))
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "spell"));
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "mastery")))
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "mastery"));
                    if (!Directory.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "runes")))
                        Directory.CreateDirectory(Path.Combine(Client.ExecutingDirectory, "Assets", "runes"));
                    if (SavePlace.EndsWith(".jpg") || SavePlace.EndsWith(".png"))
                    {
                        if (SavePlace.Contains("assets/images/champions/"))
                        {
                            using (WebClient newClient = new WebClient())
                            {
                                string SaveName = Location.Split(new string[] { "/champions/" }, StringSplitOptions.None)[1];
                                LogTextBox("Downloading " + SaveName + " from http://l3cdn.riotgames.com");
                                newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "champions", SaveName));
                            }
                        }
                        else if (SavePlace.Contains("assets/images/abilities/"))
                        {
                            using (WebClient newClient = new WebClient())
                            {
                                string SaveName = Location.Split(new string[] { "/abilities/" }, StringSplitOptions.None)[1];
                                LogTextBox("Downloading " + SaveName + " from http://l3cdn.riotgames.com");
                                if (SaveName.ToLower().Contains("passive"))
                                    newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "passive", SaveName));
                                else
                                    newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "spell", SaveName));
                            }
                        }
                        else if (SavePlace.Contains("assets/images/runes/"))
                        {
                            using (WebClient newClient = new WebClient())
                            {
                                string SaveName = Location.Split(new string[] { "/runes/" }, StringSplitOptions.None)[1];
                                LogTextBox("Downloading " + SaveName + " from http://l3cdn.riotgames.com");
                                newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "runes", SaveName));
                            }
                        }
                        else if (SavePlace.Contains("assets/images/mastery/"))
                        {
                            using (WebClient newClient = new WebClient())
                            {
                                string SaveName = Location.Split(new string[] { "/runes/" }, StringSplitOptions.None)[1];
                                LogTextBox("Downloading " + SaveName + " from http://l3cdn.riotgames.com");
                                newClient.DownloadFile("http://l3cdn.riotgames.com/releases/live" + Location, Path.Combine(Client.ExecutingDirectory, "Assets", "masteries", SaveName));
                            }
                        }
                    }
                }
            }
        }

        private void FindClient_Click(object sender, RoutedEventArgs e)
        {
            GetLolRootPath(true);
        }
    }
}