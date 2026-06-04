using System.Management;
using System.Timers;

namespace SYSi.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        private HardwareService _hw;
        private DispatcherTimer _timer;

        [ObservableProperty]
        private ICollection<NavigationCard> _navigationCards = NavigationHandle.GetNavigationCards(["SYSi.Views.Pages", "SYSi.Views.PagesBottom"], typeof(HomePage));

        public HomeViewModel()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async void InitializeViewModel()
        {
            _isInitialized = true;

            _hw = await Task.Run(() => new HardwareService());

            await LoadStaticInfo();

            await RefreshDynamic();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => _ = RefreshDynamic();
            _timer.Start();
        }

        [ObservableProperty]
        private string _appName = AppInfoHelper.AppName;

        [ObservableProperty]
        private string _author = AppInfoHelper.Author;

        [ObservableProperty]
        private string _sortAuthor = AppInfoHelper.SortAuthor;

        [ObservableProperty]
        private string _authorCreated = AppInfoHelper.AuthorCreated;

        [ObservableProperty]
        private string _appDescription = AppInfoHelper.AppDescription;

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private double _ramUsagePercent;

        [ObservableProperty]
        private double _gpuUsage;

        [ObservableProperty]
        private string _osName = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _uptime = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _totalRam = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _cpuName = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _ramUsage = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _ramType = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private string _ramSpeed = LanguageBase.GetLangValue("loading_title");

        [ObservableProperty]
        private ObservableCollection<GpuInfo> _gpuList = [];

        private async Task LoadStaticInfo()
        {
            try
            {
                var osTask = Task.Run(() =>
                {
                    try
                    {
                        using var searcher =
                            new ManagementObjectSearcher(
                                "SELECT * FROM Win32_OperatingSystem");

                        foreach (ManagementObject obj in searcher.Get())
                        {
                            return obj["Caption"]?.ToString()?.Trim() ?? "N/A";
                        }
                    }
                    catch
                    {
                    }

                    return "Windows";
                });

                var cpuTask = Task.Run(() => _hw.GetCpuInfo());
                var gpuTask = Task.Run(() => _hw.GetGpuInfoList());
                var ramTask = Task.Run(() => _hw.GetRamInfo());

                await Task.WhenAll(osTask, cpuTask, gpuTask, ramTask);

                OsName = await osTask;
                CpuName = cpuTask.Result.Name;
                GpuList = new ObservableCollection<GpuInfo>(gpuTask.Result);
                RamType = ramTask.Result.MemoryType;
                RamSpeed = ramTask.Result.SpeedText;
            }
            catch
            {
            }
        }

        private async Task RefreshDynamic()
        {
            var data = await Task.Run(() =>
            {
                var cpu = _hw.GetCpuUsage();
                var ram = _hw.GetRamInfo();
                var gpu = _hw.GetGpuInfoList().FirstOrDefault();

                return new
                {
                    Cpu = cpu,
                    RamUsagePercent = ram.UsagePercent,
                    RamUsage = ram.UsedText,
                    TotalRam = ram.TotalText
                };
            });

            CpuUsage = data.Cpu;
            RamUsagePercent = data.RamUsagePercent;
            RamUsage = data.RamUsage;
            TotalRam = data.TotalRam;
            Uptime = GetUptime();
        }

        private static string GetUptime()
        {
            var ts = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m";
        }

        public void StopTimer() => _timer.Stop();
    }
}
