namespace SYSi.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "SYSTEM INFO";

        [ObservableProperty]
        private bool _isPaneOpen = UserDataStore.GetValue<bool>("IsNavPaneOpen");

        [ObservableProperty]
        private ObservableCollection<object> _menuItems;

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems;

        public MainWindowViewModel(INavigationService navigationService)
        {
            NavigationHandle.NavigationService = navigationService;
            _menuItems = NavigationHandle.GetNavCardsInNamespace("SYSi.Views.Pages");
            _footerMenuItems = NavigationHandle.GetNavCardsInNamespace("SYSi.Views.PagesBottom");
        }

        partial void OnIsPaneOpenChanged(bool value)
        {
            UserDataStore.SetValue("IsNavPaneOpen", value);
        }
    }
}
