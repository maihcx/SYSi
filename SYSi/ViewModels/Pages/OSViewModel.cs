namespace SYSi.ViewModels.Pages
{
    public partial class OSViewModel : ObservableObject
    {
        private readonly OsHostService osHostService;

        public OSViewModel(OsHostService osHostService)
        {
            this.osHostService = osHostService;

            InitializeViewModel();
        }

        [ObservableProperty]
        private OsInfo _osInfo = new();

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
