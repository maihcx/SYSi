using System.Windows;

namespace SYSi.BugTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            string? logPath = null;
            string? appName = null;

            // Parse args: --crash-report "<path>" [--app "SYSi"]
            for (int i = 0; i < e.Args.Length; i++)
            {
                if (e.Args[i] == "--crash-report" && i + 1 < e.Args.Length)
                {
                    logPath = e.Args[++i];
                }
                else if (e.Args[i] == "--app" && i + 1 < e.Args.Length)
                {
                    appName = e.Args[++i];
                }
            }

            var window = new CrashWindow(logPath, appName ?? "SYSi");
            window.Show();
        }
    }

}
