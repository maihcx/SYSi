namespace SYSi.ViewModels.Pages
{
    public partial class GpuViewModel : ObservableObject
    {
        private readonly HardwareHostService hardwareHostService;

        private List<GpuInfo> Gpus = new();

        public GpuViewModel(HardwareHostService hardwareHostService)
        {
            this.hardwareHostService = hardwareHostService;

            InitializeViewModel();
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

        private void InitializeViewModel()
        {
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            try
            {
                Gpus = hardwareHostService?.Gpus ?? new();
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
