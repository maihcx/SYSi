namespace SYSi.ViewModels.Pages
{
    public partial class CpuViewModel : ObservableObject, INavigationAware
    {
        private readonly HardwareHostService hardwareHostService;

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        public CpuViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
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

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

        private void InitializeViewModel()
        {
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                CpuInfo = hardwareHostService?.CpuInfo ?? new CpuInfo { Name = "N/A" };
            }
            catch
            {
            }
        }
    }
}
