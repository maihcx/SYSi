namespace SYSi.ViewModels.PagesBottom
{
    public partial class AboutViewModel : ObservableObject, INavigationAware
    {
        public AboutViewModel()
        {
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

        [ObservableProperty]
        private string _appVersion = string.Empty;

        private void InitializeViewModel()
        {
            var v = Services.UpdateService.UpdateService.GetCurrentVersion();
            AppVersion = $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}
