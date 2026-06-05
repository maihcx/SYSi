using static SYSi.Resources.ThemeConfigs;

namespace SYSi.ViewModels.Pages
{
    public partial class GpuViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private HardwareService? _hw;

        private List<GpuInfo> Gpus = new();

        static string loadingText = LanguageBase.GetLangValue("loading_title");

        public GpuViewModel()
        {
            if (!_isInitialized)
            {
                _ = InitializeViewModel();
            }
        }

        [ObservableProperty]
        private GpuInfo _gpuInfo = new();

        [ObservableProperty]
        private bool _noneGpus = true;

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem?> _gpuNames = new();

        [ObservableProperty]
        private Models.ComboBoxItem? _selectedGpuName = new();

        partial void OnSelectedGpuNameChanged(Models.ComboBoxItem? value)
        {
            GpuInfo = Gpus[Convert.ToInt32(value?.Value ?? "0")];
        }

        private void setLoading()
        {
            GpuInfo.BitsPerPixel = loadingText;
            GpuInfo.Manufacturer = loadingText;
            GpuInfo.DriverVersion = loadingText;
            GpuInfo.VramText = loadingText;
            GpuInfo.VideoArchitecture = loadingText;
            GpuInfo.VideoProcessor = loadingText;
            GpuInfo.DriverDate = loadingText;
            GpuInfo.PnpDeviceId = loadingText;
            GpuInfo.Name = loadingText;
            GpuInfo.RefreshRate = loadingText;
            GpuInfo.Resolution = loadingText;
            GpuInfo.VideoMemoryType = loadingText;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await InitializeViewModel();
            }
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

        private async Task InitializeViewModel()
        {
            _isInitialized = true;

            setLoading();

            _hw = await Task.Run(() => new HardwareService());

            await LoadStaticInfo();
        }

        private async Task LoadStaticInfo()
        {
            try
            {
                var gpuTask = Task.Run(() => _hw?.GetGpuInfoList());

                //GpuInfo = _hw?.GetGpuInfoList() ?? new CpuInfo { Name = "N/A" };

                await Task.WhenAll(gpuTask);

                Gpus = gpuTask.Result ?? new();
                NoneGpus = Gpus.Count < 1;


                for (int i = 0; i < Gpus.Count; i++)
                {
                    var cbbItem = new Models.ComboBoxItem()
                    {
                        Content = Gpus[i].Name,
                        Value = i.ToString()
                    };

                    GpuNames.Add(cbbItem);

                    if (i == 0)
                    {
                        SelectedGpuName = cbbItem;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
