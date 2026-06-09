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
            LoadStaticInfo();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void LoadStaticInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption, Version, OSArchitecture, InstallDate, LastBootUpTime "
                    + "FROM Win32_OperatingSystem");

                foreach (ManagementObject obj in searcher.Get())
                {
                    OsInfo.OsName         = obj["Caption"]?.ToString()?.Trim()         ?? "Windows";
                    OsInfo.OsVersion      = obj["Version"]?.ToString()?.Trim()         ?? "N/A";
                    OsInfo.OsArchitecture = obj["OSArchitecture"]?.ToString()?.Trim()  ?? "N/A";
                    OsInfo.InstallDate    = ParseWmiDate(obj["InstallDate"]?.ToString());
                    OsInfo.LastBoot       = ParseWmiDate(obj["LastBootUpTime"]?.ToString());
                    break;
                }
            }
            catch { OsInfo.OsName = "Windows"; }

            OsInfo.Hostname = Environment.MachineName;
            OsInfo.Username = Environment.UserName;

            OnPropertyChanged(nameof(OsInfo));
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

        /// <summary>
        /// WMI date format: "20240115123045.000000+420" → "15/01/2024 12:30"
        /// </summary>
        private static string ParseWmiDate(string? wmi)
        {
            if (string.IsNullOrWhiteSpace(wmi) || wmi.Length < 14)
                return "N/A";
            try
            {
                var dt = ManagementDateTimeConverter.ToDateTime(wmi);
                return dt.ToString("dd/MM/yyyy HH:mm");
            }
            catch { return "N/A"; }
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