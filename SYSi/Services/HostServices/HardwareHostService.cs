using System.Timers;

namespace SYSi.Services.HostServices
{
    public sealed class HardwareHostService : INotifyPropertyChanged, IDisposable
    {
        private readonly HardwareService.HardwareService _hardware;
        private readonly System.Timers.Timer _timer;

        public CpuInfo CpuInfo { get; private set; } = new();
        public RamInfo RamInfo { get; private set; } = new();

        public List<GpuInfo> Gpus { get; private set; } = [];
        public List<StorageDriveInfo> Drives { get; private set; } = [];
        public List<NetworkAdapterInfo> Networks { get; private set; } = [];

        public MotherboardInfo Motherboard { get; private set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public HardwareHostService(HardwareService.HardwareService hardware)
        {
            _hardware = hardware;

            LoadStaticInfo();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void LoadStaticInfo()
        {
            CpuInfo = _hardware.GetCpuInfo();
            RamInfo = _hardware.GetRamInfo();

            Gpus = _hardware.GetGpuInfoList();
            Drives = _hardware.GetStorageInfo();
            Networks = _hardware.GetNetworkInfo();

            Motherboard = _hardware.GetMotherboardInfo();

            _hardware.InitGpuPdh(Gpus);

            NotifyAll();
        }

        private async void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await Task.WhenAll(
                    Task.Run(() => _hardware.RefreshCPUInfo(CpuInfo)),
                    Task.Run(() => _hardware.RefreshGpuUsage(Gpus)),
                    Task.Run(() => _hardware.RefreshRamInfo(RamInfo))
                );

                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(CpuInfo));
                    OnPropertyChanged(nameof(Gpus));
                    OnPropertyChanged(nameof(RamInfo));
                });
            }
            catch
            {
            }
        }

        private void NotifyAll()
        {
            OnPropertyChanged(nameof(CpuInfo));
            OnPropertyChanged(nameof(RamInfo));
            OnPropertyChanged(nameof(Gpus));
            OnPropertyChanged(nameof(Drives));
            OnPropertyChanged(nameof(Networks));
            OnPropertyChanged(nameof(Motherboard));
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
