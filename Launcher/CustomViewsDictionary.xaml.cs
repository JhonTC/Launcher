using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Launcher
{
    public partial class CustomViewsDictionary
    {
        private void LaunchClientButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild client = MainWindow.activeVisibleSoftware.builds[0] as VisualBuild;
            if (client != null)
            {
                client.LaunchButton_Click();
            }
        }

        private void LaunchServerButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild server = MainWindow.activeVisibleSoftware.builds[1] as VisualBuild;
            if (server != null)
            {
                server.LaunchButton_Click();
            }
        }

        private void OpenClientGithubButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild client = MainWindow.activeVisibleSoftware.builds[0] as VisualBuild;
            if (client != null)
            {
                OpenUrl(client.gitProjectLink);
            }
        }

        private void OpenServerGithubButton_Click(object sender, RoutedEventArgs e)
        {
            VisualBuild server = MainWindow.activeVisibleSoftware.builds[1] as VisualBuild;
            if (server != null)
            {
                OpenUrl(server.gitProjectLink);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

    }
}
