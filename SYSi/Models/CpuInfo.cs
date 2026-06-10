namespace SYSi.Models
{
    public partial class CpuInfo : ObservableObject
    {
        [ObservableProperty]
        public string _name = string.Empty;

        [ObservableProperty]
        public string _shortName = string.Empty;

        [ObservableProperty]
        public string _manufacturer = string.Empty;

        [ObservableProperty]
        public string _architecture = string.Empty;

        [ObservableProperty]
        public int _physicalCores;

        [ObservableProperty]
        public int _logicalProcessors;

        [ObservableProperty]
        public string _baseClockGHz = string.Empty;

        [ObservableProperty]
        public string _currentClockGHz = string.Empty;

        [ObservableProperty]
        public string _l1Cache = string.Empty;

        [ObservableProperty]
        public string _l2Cache = string.Empty;

        [ObservableProperty]
        public string _l3Cache = string.Empty;

        [ObservableProperty]
        public string _socket = string.Empty;

        [ObservableProperty]
        public bool _virtualizationEnabled;

        [ObservableProperty]
        public string _processorId = string.Empty;

        [ObservableProperty]
        public string _stepping = string.Empty;

        [ObservableProperty]
        public string _family = string.Empty;

        [ObservableProperty]
        public string _model = string.Empty;

        [ObservableProperty]
        public double _usagePercent;

        public double TemperatureCelsius { get; set; }
    }
}
