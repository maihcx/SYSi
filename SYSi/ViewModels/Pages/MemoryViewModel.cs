namespace SYSi.ViewModels.Pages
{
    public partial class MemoryViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private readonly HardwareHostService hardwareHostService;

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        public MemoryViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        [ObservableProperty]
        private RamInfo _ramInfo = new();

        private void setLoading()
        {
            RamInfo.SpeedText = loadingText;
            RamInfo.AvailableText = loadingText;
            RamInfo.TotalText = loadingText;
            RamInfo.UsedText = loadingText;
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
            _isInitialized = true;

            setLoading();

            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                RamInfo = hardwareHostService?.RamInfo ?? new();
            }
            catch
            {
            }
        }
    }
}
