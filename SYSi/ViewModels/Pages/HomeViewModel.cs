using System.Management;

namespace SYSi.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private readonly HardwareHostService hardwareHostService;

        private DispatcherTimer? _timer;

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        private INavigationService navigationService;

        [ObservableProperty]
        private ICollection<NavigationCard> _navigationCards = NavigationHandle.GetNavigationCards(["SYSi.Views.Pages", "SYSi.Views.PagesBottom"], typeof(HomePage), typeof(CpuPage), typeof(GpuPage), typeof(MemoryPage));

        public HomeViewModel(INavigationService navigationService, HardwareHostService hardwareHostService)
        {
            this.navigationService = navigationService;
            this.hardwareHostService = hardwareHostService;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await InitializeViewModel();
            }

            StartTimer();
        }

        public Task OnNavigatedFromAsync()
        {
            StopTimer();

            return Task.CompletedTask;
        }

        private async Task InitializeViewModel()
        {
            _isInitialized = true;

            await LoadStaticInfo();
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
        private string _osName = loadingText;

        [ObservableProperty]
        private string _uptime = loadingText;

        [ObservableProperty]
        private ObservableCollection<GpuInfo> _gpuList = [];

        [ObservableProperty]
        private CpuInfo _cpuInfo = new CpuInfo();

        [ObservableProperty]
        private RamInfo _ramInfo = new RamInfo();

        [RelayCommand]
        private void NavigateCPU()
        {
            navigationService.Navigate(typeof(CpuPage));
        }

        [RelayCommand]
        private void NavigateGPU()
        {
            navigationService.Navigate(typeof(GpuPage));
        }

        [RelayCommand]
        private void NavigateMemory()
        {
            navigationService.Navigate(typeof(MemoryPage));
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

                await Task.WhenAll(osTask);

                OsName = await osTask;
                GpuList = new ObservableCollection<GpuInfo>(hardwareHostService?.Gpus ?? new());

                CpuInfo = hardwareHostService?.CpuInfo ?? new CpuInfo();
                RamInfo = hardwareHostService?.RamInfo ?? new RamInfo();

            }
            catch
            {
            }
        }

        private async Task RefreshDynamic()
        {
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
