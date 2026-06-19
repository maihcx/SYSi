namespace SYSi.ViewModels.Pages
{
    public partial class GpuViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        private List<GpuInfo> Gpus = new();

        private const int HistoryCapacitySecond = 30;
        private int GpuHistorySize = 0;
        private double[] _gpuRing = [];
        private int _gpuHead = 0;
        private TimeSpan refreshInterval;

        public GpuViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private GpuInfo _gpuInfo = new();

        [ObservableProperty]
        private bool _noneGpus = true;

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem?> _gpuNames = new();

        [ObservableProperty]
        private Models.ComboBoxItem? _selectedGpuName = new();

        [ObservableProperty]
        private IReadOnlyList<double> _gpuUsageHistory = Array.Empty<double>();

        partial void OnSelectedGpuNameChanged(Models.ComboBoxItem? value)
        {
            GpuInfo = Gpus[Convert.ToInt32(value?.Value ?? "0")];

            _gpuRing = new double[GpuHistorySize];
            _gpuHead = 0;
            GpuUsageHistory = _gpuRing;
        }

        private void InitializeViewModel()
        {
            refreshInterval = TimeSpan.FromMilliseconds(UserDataStore.GetValue<double>("RefreshInfoInterval"));
            GpuHistorySize = (int)((1000 / refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _gpuRing = new double[GpuHistorySize];

            LoadStaticInfo();

            App.GetRequiredService<HardwareHostService>().RefreshIntervalChanged += GpuViewModel_RefreshIntervalChanged;
        }

        private void LoadStaticInfo()
        {
            Gpus = hardwareHostService.Gpus;
            NoneGpus = Gpus.Count < 1;

            for (int i = 0; i < Gpus.Count; i++)
            {
                var cbbItem = new Models.ComboBoxItem()
                {
                    Content = Gpus[i].Name,
                    Value = i.ToString()
                };

                GpuNames.Add(cbbItem);

                if (i == 0)
                {
                    SelectedGpuName = cbbItem;
                }
            }

            hardwareHostService.PropertyChanged += HardwareHostService_PropertyChanged;
        }

        private void GpuViewModel_RefreshIntervalChanged(TimeSpan refreshInterval)
        {
            if (this.refreshInterval.TotalMilliseconds == refreshInterval.TotalMilliseconds)
            {
                return;
            }
            if (refreshInterval.TotalMilliseconds == 0)
            {
                _gpuHead = 0;
                _gpuRing = new double[GpuHistorySize];
                GpuUsageHistory = _gpuRing;
                return;
            }
            this.refreshInterval = refreshInterval;
            GpuHistorySize = (int)(1000 / (double)(refreshInterval.TotalMilliseconds) * HistoryCapacitySecond);
            _gpuRing = new double[GpuHistorySize];
            _gpuHead = 0;
            GpuUsageHistory = _gpuRing;
        }

        private void HardwareHostService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(hardwareHostService.Gpus))
            {
                PushGpuUsage(GpuInfo?.UsagePercent ?? 0);
            }
        }

        private void PushGpuUsage(double usagePercent)
        {
            if (GpuHistorySize == 0)
            {
                return;
            }
            _gpuRing[_gpuHead] = usagePercent;
            _gpuHead = (_gpuHead + 1) % GpuHistorySize;

            var snapshot = new double[GpuHistorySize];
            for (int i = 0; i < GpuHistorySize; i++)
            {
                snapshot[i] = _gpuRing[(_gpuHead + i) % GpuHistorySize];
            }

            GpuUsageHistory = snapshot;
        }
    }
}
