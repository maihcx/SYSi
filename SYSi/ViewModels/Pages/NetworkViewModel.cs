namespace SYSi.ViewModels.Pages
{
    public partial class NetworkViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private readonly HardwareHostService hardwareHostService;

        public NetworkViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        [ObservableProperty]
        private List<NetworkAdapterInfo> _adapters = new();

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
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                Adapters = hardwareHostService?.Networks ?? new();
            }
            catch { }
        }
    }
}