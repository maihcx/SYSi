namespace SYSi.ViewModels.Pages
{
    public partial class MemoryViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        private const int HistoryCapacitySecond = 30;
        private int RamHistorySize = 0;
        private double[] _ramRing = [];
        private int _ramHead = 0;
        private TimeSpan refreshInterval;

        public MemoryViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private RamInfo? _ramInfo;

        [ObservableProperty]
        private IReadOnlyList<double> _ramUsageHistory = Array.Empty<double>();

        private void InitializeViewModel()
        {
            refreshInterval = TimeSpan.FromMilliseconds(UserDataStore.GetValue<double>("RefreshInfoInterval"));
            RamHistorySize = (int)((1000 / refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _ramRing = new double[RamHistorySize];

            LoadStaticInfo();

            App.GetRequiredService<HardwareHostService>().RefreshIntervalChanged += MemoryViewModel_RefreshIntervalChanged;
        }

        private void LoadStaticInfo()
        {
            RamInfo = hardwareHostService.RamInfo;
            hardwareHostService.PropertyChanged += HardwareHostService_PropertyChanged; ;
        }

        private void MemoryViewModel_RefreshIntervalChanged(TimeSpan refreshInterval)
        {
            if (this.refreshInterval.TotalMilliseconds == refreshInterval.TotalMilliseconds)
            {
                return;
            }
            if (refreshInterval.TotalMilliseconds == 0)
            {
                _ramHead = 0;
                _ramRing = new double[RamHistorySize];
                RamUsageHistory = _ramRing;
                return;
            }
            this.refreshInterval = refreshInterval;
            RamHistorySize = (int)(1000 / (double)(refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _ramRing = new double[RamHistorySize];
            _ramHead = 0;
            RamUsageHistory = _ramRing;
        }

        private void HardwareHostService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RamInfo))
            {
                PushRamUsage(RamInfo?.UsagePercent ?? 0);
            }
        }

        private void PushRamUsage(double usagePercent)
        {
            if (RamHistorySize == 0)
            {
                return;
            }
            _ramRing[_ramHead] = usagePercent;
            _ramHead = (_ramHead + 1) % RamHistorySize;

            var snapshot = new double[RamHistorySize];
            for (int i = 0; i < RamHistorySize; i++)
            {
                snapshot[i] = _ramRing[(_ramHead + i) % RamHistorySize];
            }

            RamUsageHistory = snapshot;
        }
    }
}
