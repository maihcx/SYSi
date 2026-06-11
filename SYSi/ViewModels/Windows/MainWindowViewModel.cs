namespace SYSi.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        private readonly INavigationService _navigationService;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        [ObservableProperty]
        private string _applicationTitle = "SYSTEM INFO";

        [ObservableProperty]
        private bool _isPaneOpen = UserDataStore.GetValue<bool>("IsNavPaneOpen");

        [ObservableProperty]
        private ObservableCollection<object> _menuItems;

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems;

        public MainWindowViewModel(INavigationService navigationService, HardwareHostService hardwareHostService, OsHostService osHostService)
        {
            NavigationHandle.NavigationService = navigationService;
            _navigationService = navigationService;
            _menuItems = NavigationHandle.GetNavCardsInNamespace("SYSi.Views.Pages");
            _footerMenuItems = NavigationHandle.GetNavCardsInNamespace("SYSi.Views.PagesBottom");

            int refreshInfoInterval = UserDataStore.GetValue<int>("RefreshInfoInterval");
            hardwareHostService.SetRefreshInterval(refreshInfoInterval);
            osHostService.SetRefreshInterval(refreshInfoInterval);
        }

        partial void OnIsPaneOpenChanged(bool value)
        {
            UserDataStore.SetValue("IsNavPaneOpen", value);
        }
    }
}
