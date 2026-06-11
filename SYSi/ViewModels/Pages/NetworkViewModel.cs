namespace SYSi.ViewModels.Pages
{
    public partial class NetworkViewModel : ObservableObject, INavigationAware
    {
        private readonly HardwareHostService hardwareHostService;

        public NetworkViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
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
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            Adapters = hardwareHostService.Networks;
        }
    }
}