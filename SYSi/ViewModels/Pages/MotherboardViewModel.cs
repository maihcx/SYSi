namespace SYSi.ViewModels.Pages
{
    public partial class MotherboardViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        public MotherboardViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private MotherboardInfo? _motherboardInfo;

        private void InitializeViewModel()
        {
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            MotherboardInfo = hardwareHostService.Motherboard;
        }
    }
}
