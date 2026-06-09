using System.Management;
using System.Timers;

namespace SYSi.Services.HostServices
{
    public sealed class OsHostService : INotifyPropertyChanged, IDisposable
    {
        private readonly System.Timers.Timer _timer;

        public OsInfo OsInfo { get; private set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public OsHostService()
        {
            Task.Run(LoadStaticInfo);

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void LoadStaticInfo()
        {
            var wmiTask = Task.Run(LoadFromWmi);
            var activationTask = Task.Run(LoadActivationStatus);
            var updateTask = Task.Run(LoadWindowsUpdateStatus);

            LoadFromEnvironment();
            OnPropertyChanged(nameof(OsInfo));

            Task.WaitAll(wmiTask, activationTask, updateTask);
            OnPropertyChanged(nameof(OsInfo));

            LanguageBase.LanguageChanged += async (lang) =>
            {
                await Task.WhenAll(
                    Task.Run(LoadActivationStatus),
                    Task.Run(LoadWindowsUpdateStatus)
                );

                OnPropertyChanged(nameof(OsInfo));
            };
        }

        private void LoadFromWmi()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption, Version, OSArchitecture, InstallDate, LastBootUpTime " +
                    "FROM Win32_OperatingSystem");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string version = obj["Version"]?.ToString()?.Trim() ?? "N/A";
                    var dtInstall = ManagementDateTimeConverter.ToDateTime(obj["InstallDate"]?.ToString() ?? "");
                    var dtLastBoot = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"]?.ToString() ?? "");

                    OsInfo.OsName         = ParseOsName(obj["Caption"]?.ToString());
                    OsInfo.OsVersion      = version;
                    OsInfo.BuildNumber    = ParseBuildNumber(version);
                    OsInfo.OsArchitecture = obj["OSArchitecture"]?.ToString()?.Trim() ?? "N/A";

                    OsInfo.InstallDate    = dtInstall.ToString("dd/MM/yyyy");
                    OsInfo.InstallTime    = dtInstall.ToString("HH:mm");

                    OsInfo.LastBoot       = dtLastBoot.ToString("dd/MM/yyyy HH:mm");
                    OsInfo.LastBootDate   = dtLastBoot.ToString("dd/MM/yyyy");
                    OsInfo.LastBootTime   = dtLastBoot.ToString("HH:mm");
                    break;
                }
            }
            catch
            {
                OsInfo.OsName = "Windows";
            }
        }

        private void LoadFromEnvironment()
        {
            OsInfo.Hostname    = Environment.MachineName;
            OsInfo.Username    = Environment.UserName;
            OsInfo.SystemRoot  = Environment.GetEnvironmentVariable("SystemRoot")  ?? "N/A";
            OsInfo.UserProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "N/A";
            OsInfo.Locale      = CultureInfo.CurrentCulture.Name;
            OsInfo.TimeZone    = TimeZoneInfo.Local.DisplayName;

            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            OsInfo.OsEdition = key?.GetValue("EditionID")?.ToString() ?? "N/A";
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                OsInfo.Uptime = GetUptime();
                OnPropertyChanged(nameof(OsInfo));
            }
            catch { }
        }

        private static string GetUptime()
        {
            var ts = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        }

        private void LoadActivationStatus()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT LicenseStatus FROM SoftwareLicensingProduct " +
                    "WHERE PartialProductKey IS NOT NULL " +
                    "AND ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    uint status = (uint)(obj["LicenseStatus"] ?? 0u);
                    OsInfo.IsActivated       = status == 1;
                    OsInfo.ActivationStatus  = status == 1 ? LanguageBase.GetLangValue("wactiv_actived_title") : LanguageBase.GetLangValue("wactiv_inactived_title");
                    return;
                }
            }
            catch { }

            OsInfo.ActivationStatus = "N/A";
        }

        private void LoadWindowsUpdateStatus()
        {
            try
            {
                using var rebootKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");

                if (rebootKey != null)
                {
                    OsInfo.IsUpToDate           = false;
                    OsInfo.WindowsUpdateStatus  = LanguageBase.GetLangValue("wus_restart_required_title");
                    return;
                }

                using var pendingKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RequestedUpdates");

                if (pendingKey?.GetSubKeyNames().Length > 0)
                {
                    OsInfo.IsUpToDate           = false;
                    OsInfo.WindowsUpdateStatus  = LanguageBase.GetLangValue("wus_updates_available_title");
                    return;
                }

                OsInfo.IsUpToDate          = true;
                OsInfo.WindowsUpdateStatus = LanguageBase.GetLangValue("wus_up_to_date_title");
            }
            catch
            {
                OsInfo.WindowsUpdateStatus = "N/A";
            }
        }

        private static string ParseOsName(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Windows";
            return raw.Replace("Microsoft Windows", "Windows")
                      .Replace("Microsoft", "")
                      .Trim();
        }

        private static string ParseBuildNumber(string version)
        {
            var parts = version.Split('.');
            return parts.Length >= 3 ? parts[2] : "N/A";
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}