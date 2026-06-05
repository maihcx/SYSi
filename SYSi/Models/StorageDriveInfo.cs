namespace SYSi.Models
{
    public partial class StorageDriveInfo : ObservableObject
    {
        [ObservableProperty]
        public string _letter = string.Empty;

        [ObservableProperty]
        public string _model = string.Empty;

        [ObservableProperty]
        public string _totalText = string.Empty;

        [ObservableProperty]
        public string _freeText = string.Empty;

        [ObservableProperty]
        public string _usedText = string.Empty;

        [ObservableProperty]
        public double _usagePercent = 0;

        [ObservableProperty]
        public string _fileSystem = string.Empty;

        [ObservableProperty]
        public string _interface = string.Empty;

        [ObservableProperty]
        public string _driveType = string.Empty;

        [ObservableProperty]
        public string _firmware = string.Empty;

        [ObservableProperty]
        public string _serialNumber = string.Empty;

        [ObservableProperty]
        public string _status = string.Empty;

        [ObservableProperty]
        public string _volumeLabel = string.Empty;
    }
}
