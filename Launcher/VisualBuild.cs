using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    class VisualBuild : Build
    {
        public Button button;

        public TextBlock versionTextBlock;
        public TextBlock downloadProgressText;

        public override LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.idle:
                        button.Content = "Check For Updates";
                        break;
                    case LauncherStatus.downloadRequired:
                        button.Content = $"Download {friendlyName}";
                        break;
                    case LauncherStatus.updateRequired:
                        button.Content = $"Update {friendlyName}";
                        break;
                    case LauncherStatus.readyToLaunch:
                        button.Content = $"Launch {friendlyName}";
                        break;
                    case LauncherStatus.failed:
                        button.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        button.Content = "Downloading...";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        button.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }

        internal VisualBuild(string _friendlyName, BuildType _buildType, string _rootPath, string _subFolderName, string _versionFileLink, string _buildFileLink, string _gitProjectLink,  Button _button, TextBlock _versionTextBlock, TextBlock _downloadProgressText) 
            : base(_friendlyName, _buildType, _rootPath, _subFolderName, _versionFileLink, _buildFileLink, _gitProjectLink)
        {
            button = _button;

            versionTextBlock = _versionTextBlock;
            downloadProgressText = _downloadProgressText;
        }

        public override async Task CheckForUpdates(bool autoDownload = false, bool autoLaunch = false, bool autoClose = false)
        {
            if (Status == LauncherStatus.downloadingGame || Status == LauncherStatus.downloadingUpdate)
            {
                return;
            }

            button.IsEnabled = false;
            try
            {
                WebClient webClient = new WebClient();
                onlineVersion = new Version(await webClient.DownloadStringTaskAsync(versionFileLink));

                if (File.Exists(versionFile) && File.Exists(buildExe))
                {
                    Version localVersion = new Version(File.ReadAllText(versionFile));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        versionTextBlock.Text = $"{localVersion} (Update {onlineVersion})";
                        Status = LauncherStatus.updateRequired;
                    }
                    else
                    {
                        versionTextBlock.Text = localVersion.ToString();
                        Status = LauncherStatus.readyToLaunch;
                    }
                }
                else
                {
                    versionTextBlock.Text = onlineVersion.ToString();
                    Status = LauncherStatus.downloadRequired;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for updates: {ex}");
            }
            button.IsEnabled = true;

            if (autoDownload)
            {
                if (Status == LauncherStatus.updateRequired)
                {
                    await DownloadFiles(true, onlineVersion, autoLaunch, autoClose);
                }
                else if (Status == LauncherStatus.downloadRequired)
                {
                    await DownloadFiles(false, Version.zero, autoLaunch, autoClose);
                }
            }

            if (autoLaunch && Status == LauncherStatus.readyToLaunch)
            {
                LaunchButton_Click(autoClose);
            }
        }

        protected override async Task DownloadFiles(bool isUpdate, Version _onlineVersion, bool autoLaunch = false, bool autoClose = false)
        {
            button.IsEnabled = false;
            try
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                WebClient webClient = new WebClient();
                if (isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(await webClient.DownloadStringTaskAsync(versionFileLink));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler((object sender, AsyncCompletedEventArgs e) => { DownloadCompletedCallback(onlineVersion, autoLaunch, autoClose); });
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChangedCallback);

                downloadProgressText.Margin = new Thickness(10, 0, 0, 0);
                downloadProgressText.Text = "0%";
                //webClient.DownloadFileAsync(new Uri(buildFileLink), buildZip, _onlineVersion);
                await webClient.DownloadFileTaskAsync(new Uri(buildFileLink), buildZip);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading files: {ex}");
                button.IsEnabled = true;
            }
        }

        private void DownloadProgressChangedCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            downloadProgressText.Text = $"{Math.Truncate(percentage)}%";
        }

        protected override void DownloadCompletedCallback(Version _onlineVersion, bool autoLaunch = false, bool autoClose = false)
        {
            downloadProgressText.Margin = new Thickness(0, 0, 0, 0);
            downloadProgressText.Text = "";
            string onlineVersion = _onlineVersion.ToString();

            try
            {
                ZipFile.ExtractToDirectory(buildZip, rootPath, true);
                File.Delete(buildZip);

                File.WriteAllText(versionFile, onlineVersion);

                versionTextBlock.Text = onlineVersion;
                Status = LauncherStatus.readyToLaunch;

                if (autoLaunch)
                {
                    LaunchButton_Click(autoClose);
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing files: {ex}");
            }

            button.IsEnabled = true;
        }

        public async void LaunchButton_Click(bool autoClose = false)
        {
            if (Status == LauncherStatus.downloadingGame || Status == LauncherStatus.downloadingUpdate)
            {
                return;
            }

            if (File.Exists(buildExe) && Status == LauncherStatus.readyToLaunch)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(buildExe);
                startInfo.WorkingDirectory = buildPath;
                Process.Start(startInfo);

                if (autoClose)
                {
                    var mainWindow = (Application.Current.MainWindow as MainWindow);
                    if (mainWindow != null)
                    {
                        mainWindow.Close();
                    }
                }
            }
            else if (Status == LauncherStatus.updateRequired)
            {
                await DownloadFiles(true, onlineVersion);
            }
            else if (Status == LauncherStatus.downloadRequired)
            {
                await DownloadFiles(false, Version.zero);
            }
            else
            {
                await CheckForUpdates();
            }
        }
    }
}
