namespace SYSi.ViewModels.Pages
{
    public partial class MemoryViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        public MemoryViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private RamInfo? _ramInfo;

        private void InitializeViewModel()
        {
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            RamInfo = hardwareHostService.RamInfo;
        }
    }
}
