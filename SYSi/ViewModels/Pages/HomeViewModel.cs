using System.Management;
using System.Timers;

namespace SYSi.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private HardwareService? _hw;

        private DispatcherTimer? _timer;

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        private INavigationService navigationService;

        [ObservableProperty]
        private ICollection<NavigationCard> _navigationCards = NavigationHandle.GetNavigationCards(["SYSi.Views.Pages", "SYSi.Views.PagesBottom"], typeof(HomePage), typeof(CpuPage));

        public HomeViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await InitializeViewModel();
            }

            StartTimer();
        }

        public Task OnNavigatedFromAsync() {
            StopTimer();

            return Task.CompletedTask;
        }

        private async Task InitializeViewModel()
        {
            _isInitialized = true;

            _hw = await Task.Run(() => new HardwareService());

            await LoadStaticInfo();

            await RefreshDynamic();
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
        private string _osName = loadingText;

        [ObservableProperty]
        private string _uptime = loadingText;

        [ObservableProperty]
        private string _totalRam = loadingText;

        [ObservableProperty]
        private string _cpuName = loadingText;

        [ObservableProperty]
        private string _ramUsage = loadingText;

        [ObservableProperty]
        private string _ramType = loadingText;

        [ObservableProperty]
        private string _ramSpeed = loadingText;

        [ObservableProperty]
        private ObservableCollection<GpuInfo> _gpuList = [];

        [RelayCommand]
        private void NavigateCPU()
        {
            navigationService.Navigate(typeof(CpuPage));
        }

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

                var cpuTask = Task.Run(() => _hw?.GetCpuInfo());
                var gpuTask = Task.Run(() => _hw?.GetGpuInfoList());
                var ramTask = Task.Run(() => _hw?.GetRamInfo());

                await Task.WhenAll(osTask, cpuTask, gpuTask, ramTask);

                OsName = await osTask;
                CpuName = cpuTask.Result?.Name ?? "N/A";
                GpuList = new ObservableCollection<GpuInfo>(gpuTask.Result ?? new());
                RamType = ramTask.Result?.MemoryType ?? "N/A";
                RamSpeed = ramTask.Result?.SpeedText ?? "N/A";
            }
            catch
            {
            }
        }

        private async Task RefreshDynamic()
        {
            var data = await Task.Run(() =>
            {
                var cpu = _hw?.GetCpuUsage() ?? 0;
                var ram = _hw?.GetRamInfo();
                var gpu = _hw?.GetGpuInfoList().FirstOrDefault();

                return new
                {
                    Cpu = cpu,
                    RamUsagePercent = ram?.UsagePercent ?? 0,
                    RamUsage = ram?.UsedText ?? "N/A",
                    TotalRam = ram?.TotalText ?? "N/A"
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

        public void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => _ = RefreshDynamic();
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }
    }
}
