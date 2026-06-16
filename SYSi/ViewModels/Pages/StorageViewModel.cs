namespace SYSi.ViewModels.Pages
{
    public partial class StorageViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        public StorageViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private List<StorageDriveInfo> _drives = new();

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
