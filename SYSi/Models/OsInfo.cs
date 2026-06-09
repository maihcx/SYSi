namespace SYSi.Models
{
    public partial class OsInfo : ObservableObject
    {
        [ObservableProperty]
        private string _osName = string.Empty;

        [ObservableProperty]
        private string _osVersion = string.Empty;

        [ObservableProperty]
        private string _osArchitecture = string.Empty;

        [ObservableProperty]
        private string _uptime = string.Empty;

        [ObservableProperty]
        private string _installDate = string.Empty;

        [ObservableProperty]
        private string _lastBoot = string.Empty;

        [ObservableProperty]
        private string _hostname = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;
    }
}