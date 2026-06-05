namespace SYSi.ViewModels.PagesBottom
{
    public partial class AboutViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }

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
            _isInitialized = true;

            var v = UpdateService.GetCurrentVersion();
            AppVersion = $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}
