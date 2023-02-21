using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Process current = Process.GetCurrentProcess();
            Process[] activeInstances = Process.GetProcessesByName(current.ProcessName);

            if (activeInstances.Length > 1)
            {
                foreach (Process process in activeInstances)
                {
                    if (process.Id != current.Id)
                    {
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                Current.Shutdown();
            } else
            {
                new MainWindow(e).Show();
            }
        }
    }
}
