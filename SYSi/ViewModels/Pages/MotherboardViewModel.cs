namespace SYSi.ViewModels.Pages
{
    public partial class MotherboardViewModel : ObservableObject, INavigationAware
    {
        private readonly HardwareHostService hardwareHostService;

        public MotherboardViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private MotherboardInfo _motherboardInfo = new();

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
            MotherboardInfo = hardwareHostService.Motherboard;
        }
    }
}
