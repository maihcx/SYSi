namespace SYSi.ViewModels.Pages
{
    public partial class CpuViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        public CpuViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private CpuInfo? _cpuInfo;

        private void InitializeViewModel()
        {
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            CpuInfo = hardwareHostService.CpuInfo;
        }
    }
}
