namespace SYSi.ViewModels.Pages
{
    public partial class CpuViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private HardwareService? _hw;

        private DispatcherTimer? _timer;

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        public CpuViewModel()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private CpuInfo _cpuInfo = new()
        {
            Architecture = loadingText,
            Name = loadingText,
            Family = loadingText,
            L1Cache = loadingText,
            L2Cache = loadingText,
            L3Cache = loadingText,  
            Manufacturer = loadingText,
            Model = loadingText,
            Socket = loadingText,
            Stepping = loadingText,
            ProcessorId = loadingText,
        };

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

            _hw = await Task.Run(() => new HardwareService());

            await LoadStaticInfo();

            await RefreshDynamic();
        }

        private async Task LoadStaticInfo()
        {
            try
            {
                var cpuTask = Task.Run(() => _hw?.GetCpuInfo());

                CpuInfo = _hw?.GetCpuInfo() ?? new CpuInfo { Name = "N/A" };

                await Task.WhenAll(cpuTask);

                //OsName = await osTask;
                //CpuName = cpuTask.Result.Name;
                //GpuList = new ObservableCollection<GpuInfo>(gpuTask.Result);
                //RamType = ramTask.Result.MemoryType;
                //RamSpeed = ramTask.Result.SpeedText;
            }
            catch
            {
            }
        }

        private async Task RefreshDynamic()
        {
            var data = await Task.Run(() =>
            {
                var cpuUsage = _hw?.GetCpuUsage();

                return new
                {
                    CpuUsage = cpuUsage ?? 0,
                };
            });

            CpuUsage = data.CpuUsage;

            //RamUsagePercent = data.RamUsagePercent;
            //RamUsage = data.RamUsage;
            //TotalRam = data.TotalRam;
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
