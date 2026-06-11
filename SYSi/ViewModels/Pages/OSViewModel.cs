namespace SYSi.ViewModels.Pages
{
    public partial class OSViewModel : ObservableObject, INavigationAware
    {
        private readonly OsHostService osHostService;

        public OSViewModel(OsHostService osHostService)
        {
            this.osHostService = osHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private OsInfo _osInfo = new();

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
            OsInfo = osHostService.OsInfo;
        }
    }
}
