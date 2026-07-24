using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Launcher
{
    public enum LauncherStatus
    {
        idle,
        updateRequired,
        downloadRequired,
        readyToLaunch,
        failed,
        downloadingGame,
        downloadingUpdate
    }
    public enum BuildType
    {
        Client,
        Server,
        LauncherUpdater,
        Launcher
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;

        private string launcherVersionLink;
        private string launcherVersionFile;
        private LauncherStatus status;

        private Build launcherUpdaterBuild;

        List<Software> apps = new List<Software>();

        private StartupEventArgs startupArguments;

        public static Software activeVisibleSoftware;

        public MainWindow(StartupEventArgs _startupArguments)
        {
            InitializeComponent();

            startupArguments = _startupArguments;

            rootPath = Directory.GetCurrentDirectory();

            launcherVersionLink = "https://www.dropbox.com/s/7fmu6ldaatzqpbv/Version_Launcher.txt?dl=1";
            launcherVersionFile = Path.Combine(rootPath, "Version_Launcher.txt");
            launcherUpdaterBuild = new Build("LauncherUpdater", BuildType.LauncherUpdater, rootPath, null,
                "https://www.dropbox.com/s/yv684yo5z2f2740/Version_LauncherUpdater.txt?dl=1",
                "https://www.dropbox.com/s/afud1bxd9m5wy9w/LauncherUpdater.zip?dl=1",
                null);

            string friendlyAppName = "Side & Heek";
            string safeAppName = "SideAndHeek";
            Software sideAndHeekApp = new Software(friendlyAppName, new[]
            {
                new VisualBuild(friendlyAppName,
                    "This is the client for Side And Heek. If you are a tester or player, then you want to use the button below.", 
                    BuildType.Client, rootPath, safeAppName,
                    "https://www.dropbox.com/s/zee4dpmflw59ksj/Version_Client.txt?dl=1",
                    "https://www.dropbox.com/s/rvfsr6f4hl3fegz/Build_Client.zip?dl=1",
                    "https://github.com/JhonTC/Side-And-Heek-Client/tree/development"),

                new VisualBuild($"{friendlyAppName} Server",
                    "This is the server for Side And Heek. If you want to run a custom server the use the button below. You will also need to port forward.", 
                    BuildType.Server, rootPath, safeAppName,
                    "https://www.dropbox.com/s/80n5d4j8ovst9x1/Version_Server.txt?dl=1",
                    "https://www.dropbox.com/s/6gs8otekcdlpleb/Build_Server.zip?dl=1",
                    "https://github.com/JhonTC/Side-And-Heek-Server/tree/development")
            });

            apps.Add(sideAndHeekApp);
            apps.Add(sideAndHeekApp);

            TabControl1.ItemsSource = apps;
            TabControl1.SelectionChanged += TabControl_SelectionChanged;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            activeVisibleSoftware = TabControl1.SelectedItem as Software;
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(launcherUpdaterBuild.CheckForUpdates(true));
            tasks.Add(CheckForLauncherUpdates());

            foreach(var app in apps) 
            {
                tasks.AddRange(app.CheckForUpdates());
            }
            

            await Task.WhenAll(tasks);

            for (int i = 0; i < startupArguments.Args.Length; i++)
            {
                switch (startupArguments.Args[i])
                {
                    case "relaunch_sah_server":
                        VisualBuild server = apps[0].builds[1] as VisualBuild; //todo: add value to relaunch?
                        if (server != null)
                        {
                            server.CloseAllActiveProcesses();
                            if (status == LauncherStatus.updateRequired || status == LauncherStatus.downloadRequired)
                            {
                                UpdateLauncher(startupArguments.Args[i]);
                            } else
                            {
                                await server.CheckForUpdates(true, true, true);
                            }
                        }
                        break;
                }
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Minimise_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /*private void LaunchClientButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild client = sideAndHeekApp.builds[0] as VisualBuild;
            if (client != null)
            {
                client.LaunchButton_Click();
            }
        }

        private void LaunchServerButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild server = sideAndHeekApp.builds[1] as VisualBuild;
            if (server != null)
            {
                server.LaunchButton_Click();
            }
        }

        private void OpenClientGithubButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild client = sideAndHeekApp.builds[0] as VisualBuild;
            if (client != null)
            {
                OpenUrl(client.gitProjectLink);
            }
        }

        private void OpenServerGithubButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild server = sideAndHeekApp.builds[1] as VisualBuild;
            if (server != null)
            {
                OpenUrl(server.gitProjectLink);
            }
        }*/

        private void UpdateLauncher_Click(object sender, RoutedEventArgs e)
        {
            UpdateLauncher();
        }

        private void UpdateLauncher(string args = null)
        {
            if (File.Exists(launcherUpdaterBuild.buildExe) && launcherUpdaterBuild.Status == LauncherStatus.readyToLaunch)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(launcherUpdaterBuild.buildExe);
                startInfo.WorkingDirectory = rootPath;
                if (args != null)
                {
                    startInfo.Arguments = args;
                }

                Process.Start(startInfo);

                Close();
            }
        }

        public async Task CheckForLauncherUpdates()
        {
            try
            {
                WebClient webClient = new WebClient();
                Version onlineVersion = new Version(await webClient.DownloadStringTaskAsync(launcherVersionLink));

                if (File.Exists(launcherVersionFile))
                {
                    Version localVersion = new Version(File.ReadAllText(launcherVersionFile));
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        status = LauncherStatus.updateRequired;
                    }
                }
                else
                {
                    status = LauncherStatus.downloadRequired;
                }
            }
            catch (Exception ex)
            {
                status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for launcher updates: {ex}");
            }

            if (status == LauncherStatus.updateRequired || status == LauncherStatus.downloadRequired)
            {
                UpdateLauncherButton.Opacity = 100;
                UpdateLauncherButton.IsEnabled = true;
            }
            else
            {
                UpdateLauncherButton.Opacity = 0;
                UpdateLauncherButton.IsEnabled = false;
            }
        }
    }

    public struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if (_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;

                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _other)
        {
            return major != _other.major || minor != _other.minor || subMinor != _other.subMinor;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
