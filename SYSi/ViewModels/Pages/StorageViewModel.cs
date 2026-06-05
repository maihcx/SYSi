namespace SYSi.ViewModels.Pages
{
    public partial class StorageViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private readonly HardwareHostService hardwareHostService;

        public StorageViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
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
            _isInitialized = true;

            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                Drives = hardwareHostService?.Drives ?? new();
            }
            catch
            {
            }
        }
    }
}
