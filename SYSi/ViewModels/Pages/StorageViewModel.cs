namespace SYSi.ViewModels.Pages
{
    public partial class StorageViewModel : ObservableObject, INavigationAware
    {
        private readonly HardwareHostService hardwareHostService;

        public StorageViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private List<StorageDriveInfo> _drives = new();

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
            Drives = hardwareHostService.Drives;
        }
    }
}
