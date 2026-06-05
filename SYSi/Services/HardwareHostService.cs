using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace SYSi.Services
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

            NotifyAll();
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                CpuInfo.UsagePercent = _hardware.GetCpuUsage();

                var ram = _hardware.GetRamInfo();

                RamInfo.UsagePercent = ram.UsagePercent;
                RamInfo.AvailableText = ram.AvailableText;
                RamInfo.UsedText = ram.UsedText;

                OnPropertyChanged(nameof(CpuInfo));
                OnPropertyChanged(nameof(RamInfo));
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
