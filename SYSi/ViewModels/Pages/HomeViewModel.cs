using System.Management;

namespace SYSi.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        private readonly HardwareHostService hardwareHostService;

        private readonly OsHostService osHostService;

        private INavigationService navigationService;

        [ObservableProperty]
        private ICollection<NavigationCard> _navigationCards = NavigationHandle.GetNavigationCards(["SYSi.Views.Pages", "SYSi.Views.PagesBottom"], typeof(HomePage), typeof(CpuPage), typeof(GpuPage), typeof(MemoryPage), typeof(OSPage));

        public HomeViewModel(
            INavigationService navigationService, 
            HardwareHostService hardwareHostService, 
            OsHostService osHostService)
        {
            this.navigationService = navigationService;
            this.hardwareHostService = hardwareHostService;
            this.osHostService = osHostService;

            InitializeViewModel();
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
            LoadStaticInfo();
        }

        [ObservableProperty]
        private string _appName = AppInfoHelper.AppName;

        [ObservableProperty]
        private string _author = AppInfoHelper.Author;

        [ObservableProperty]
        private string _sortAuthor = AppInfoHelper.SortAuthor;

        [ObservableProperty]
        private string _authorCreated = AppInfoHelper.AuthorCreated;

        [ObservableProperty]
        private string _appDescription = AppInfoHelper.AppDescription;

        [ObservableProperty]
        private ObservableCollection<GpuInfo> _gpuList = [];

        [ObservableProperty]
        private CpuInfo _cpuInfo = new CpuInfo();

        [ObservableProperty]
        private RamInfo _ramInfo = new RamInfo();

        [ObservableProperty]
        private OsInfo _osInfo = new OsInfo();

        [RelayCommand]
        private void NavigateCPU()
        {
            navigationService.Navigate(typeof(CpuPage));
        }

        [RelayCommand]
        private void NavigateGPU()
        {
            navigationService.Navigate(typeof(GpuPage));
        }

        [RelayCommand]
        private void NavigateMemory()
        {
            navigationService.Navigate(typeof(MemoryPage));
        }

        [RelayCommand]
        private void NavigateOS()
        {
            navigationService.Navigate(typeof(OSPage));
        }

        private void LoadStaticInfo()
        {
            try
            {
                OsInfo = osHostService.OsInfo;

                GpuList  = new ObservableCollection<GpuInfo>(hardwareHostService?.Gpus ?? []);
                CpuInfo  = hardwareHostService?.CpuInfo ?? new();
                RamInfo  = hardwareHostService?.RamInfo ?? new();
            }
            catch { }
        }
    }
}
