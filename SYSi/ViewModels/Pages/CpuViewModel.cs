namespace SYSi.ViewModels.Pages
{
    public partial class CpuViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        private const int HistoryCapacitySecond = 30;
        private int CpuHistorySize = 0;
        private double[] _cpuRing = [];
        private int _cpuHead = 0;
        private TimeSpan refreshInterval;

        public CpuViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private CpuInfo? _cpuInfo;

        [ObservableProperty]
        private IReadOnlyList<double> _cpuUsageHistory = Array.Empty<double>();

        private void InitializeViewModel()
        {
            refreshInterval = TimeSpan.FromMilliseconds(UserDataStore.GetValue<double>("RefreshInfoInterval"));
            CpuHistorySize = (int)((1000 / refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _cpuRing = new double[CpuHistorySize];
            LoadStaticInfo();

            App.GetRequiredService<HardwareHostService>().RefreshIntervalChanged += CpuViewModel_RefreshIntervalChanged;
        }

        private void CpuViewModel_RefreshIntervalChanged(TimeSpan refreshInterval)
        {
            if (this.refreshInterval.TotalMilliseconds == refreshInterval.TotalMilliseconds)
            {
                return;
            }
            if (refreshInterval.TotalMilliseconds == 0)
            {
                _cpuHead = 0;
                _cpuRing = new double[CpuHistorySize];
                CpuUsageHistory = _cpuRing;
                return;
            }
            this.refreshInterval = refreshInterval;
            CpuHistorySize = (int)(1000 / (double)(refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _cpuRing = new double[CpuHistorySize];
            _cpuHead = 0;
            CpuUsageHistory = _cpuRing;
        }

        private void LoadStaticInfo()
        {
            CpuInfo = hardwareHostService.CpuInfo;
            CpuInfo.PropertyChanged += CpuInfo_PropertyChanged;
        }

        private void CpuInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CpuInfo.UsagePercent))
            {
                PushCpuUsage(CpuInfo?.UsagePercent ?? 0);
            }
        }

        private void PushCpuUsage(double usagePercent)
        {
            if (CpuHistorySize == 0)
            {
                return;
            }
            _cpuRing[_cpuHead] = usagePercent;
            _cpuHead = (_cpuHead + 1) % CpuHistorySize;

            var snapshot = new double[CpuHistorySize];
            for (int i = 0; i < CpuHistorySize; i++)
            {
                snapshot[i] = _cpuRing[(_cpuHead + i) % CpuHistorySize];
            }

            CpuUsageHistory = snapshot;
        }
    }
}