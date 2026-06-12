namespace SYSi.Models
{
    public partial class MotherboardInfo : ObservableObject
    {
        [ObservableProperty]
        public string _manufacturer = string.Empty;

        [ObservableProperty]
        public string _product = string.Empty;

        [ObservableProperty]
        public string _version = string.Empty;

        [ObservableProperty]
        public string _serialNumber = string.Empty;

        [ObservableProperty]
        public string _biosManufacturer = string.Empty;

        [ObservableProperty]
        public string _biosVersion = string.Empty;

        [ObservableProperty]
        public string _biosDate = string.Empty;

        [ObservableProperty]
        public string _systemFamily = string.Empty;

        [ObservableProperty]
        public string _systemModel = string.Empty;

        [ObservableProperty]
        public string _microcode = string.Empty;
    }
}
