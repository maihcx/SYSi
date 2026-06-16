namespace SYSi.ViewModels.PagesBottom
{
    public partial class AboutViewModel : ObservableObject
    {
        public AboutViewModel()
        {
            InitializeViewModel();
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
