namespace SYSi.ViewModels.Pages
{
    public partial class NetworkViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        public NetworkViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private List<NetworkAdapterInfo>? _adapters;

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