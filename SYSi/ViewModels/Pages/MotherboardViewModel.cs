namespace SYSi.ViewModels.Pages
{
    public partial class MotherboardViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private readonly HardwareHostService hardwareHostService;

        public MotherboardViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        [ObservableProperty]
        private MotherboardInfo _motherboardInfo = new();

        private void setLoading()
        {

        }

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
            _isInitialized = true;

            setLoading();

            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                MotherboardInfo = hardwareHostService?.Motherboard ?? new();
            }
            catch
            {
            }
        }
    }
}
