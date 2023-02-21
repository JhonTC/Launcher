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
    class Build
    {
        public string friendlyName;
        protected string rootPath;
        protected string buildPath;
        public string versionFile;
        public string buildZip;
        public string buildExe;
        public BuildType buildType;

        public string versionFileLink { get; protected set; }
        public string buildFileLink { get; protected set; }
        public string gitProjectLink { get; protected set; }

        protected Version onlineVersion;

        protected LauncherStatus _status;
        public virtual LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
            }
        }

        internal Build(string _friendlyName, BuildType _buildType, string _rootPath, string _subFolderName, string _versionFileLink, string _buildFileLink, string _gitProjectLink)
        {
            friendlyName = _friendlyName;

            buildType = _buildType;

            rootPath = (_subFolderName != null) ? rootPath = Path.Combine(_rootPath, _subFolderName) : _rootPath;
            versionFile = Path.Combine(rootPath, $"Version_{buildType}.txt");
            buildZip = Path.Combine(rootPath, $"Build_{buildType}.zip");
            buildPath = (buildType == BuildType.LauncherUpdater) ? rootPath : Path.Combine(rootPath, $"Build_{buildType}");
            buildExe = Path.Combine(buildPath, $"{friendlyName}.exe");

            versionFileLink = _versionFileLink;
            buildFileLink = _buildFileLink;
            gitProjectLink = _gitProjectLink;
        }

        public virtual async Task CheckForUpdates(bool autoDownload = false, bool autoLaunch = false, bool autoClose = false)
        {
            if (Status == LauncherStatus.downloadingGame || Status == LauncherStatus.downloadingUpdate)
            {
                return;
            }

            try
            {
                WebClient webClient = new WebClient();
                onlineVersion = new Version(await webClient.DownloadStringTaskAsync(versionFileLink));

                if (File.Exists(versionFile) && File.Exists(buildExe))
                {
                    Version localVersion = new Version(File.ReadAllText(versionFile));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        Status = LauncherStatus.updateRequired;
                    }
                    else
                    {
                        Status = LauncherStatus.readyToLaunch;
                    }
                }
                else
                {
                    Status = LauncherStatus.downloadRequired;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for updates: {ex}");
            }

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
        }

        protected virtual async Task DownloadFiles(bool isUpdate, Version _onlineVersion, bool autoLaunch = false, bool autoClose = false)
        {
            try
            {
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
                //webClient.DownloadFileAsync(new Uri(buildFileLink), buildZip, _onlineVersion);
                await webClient.DownloadFileTaskAsync(new Uri(buildFileLink), buildZip);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error downloading files: {ex}");
            }
        }

        protected virtual void DownloadCompletedCallback(Version _onlineVersion, bool autoLaunch = false, bool autoClose = false)
        {
            string onlineVersion = _onlineVersion.ToString();

            try
            {
                ZipFile.ExtractToDirectory(buildZip, rootPath, true);
                File.Delete(buildZip);

                File.WriteAllText(versionFile, onlineVersion);

                Status = LauncherStatus.readyToLaunch;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing files: {ex}");
            }
        }

        public void CloseAllActiveProcesses()
        {
            var activeProcesses = Process.GetProcessesByName(friendlyName);
            foreach (var process in activeProcesses)
            {
                process.Kill();
            }
        }
    }
}
