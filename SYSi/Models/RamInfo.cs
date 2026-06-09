namespace SYSi.Models
{
    public partial class RamSlotInfo : ObservableObject
    {
        [ObservableProperty]
        private string _bankLabel = string.Empty;

        [ObservableProperty]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        private string _capacityText = string.Empty;

        [ObservableProperty]
        private uint _speedMHz;

        [ObservableProperty]
        private string _memoryType = string.Empty;

        [ObservableProperty]
        private string _formFactor = string.Empty;

        [ObservableProperty]
        private string _partNumber = string.Empty;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private ushort _dataWidth;
    }

    public partial class RamInfo : ObservableObject
    {
        [ObservableProperty]
        private string _totalText = "N/A";

        [ObservableProperty]
        private string _availableText = string.Empty;

        [ObservableProperty]
        private string _usedText = string.Empty;

        [ObservableProperty]
        private double _usagePercent;

        [ObservableProperty]
        private string _speedText = string.Empty;

        [ObservableProperty]
        private string _memoryType = string.Empty;

        public List<RamSlotInfo> Slots { get; set; } = new();
    }
}
