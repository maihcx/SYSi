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
        private string _installTime = string.Empty;

        [ObservableProperty]
        private string _lastBoot = string.Empty;

        [ObservableProperty]
        private string _lastBootDate = string.Empty;

        [ObservableProperty]
        private string _lastBootTime = string.Empty;

        [ObservableProperty]
        private string _hostname = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _osEdition = string.Empty;

        [ObservableProperty]
        private string _locale = string.Empty;

        [ObservableProperty]
        private string _timeZone = string.Empty;

        [ObservableProperty]
        private string _systemRoot = string.Empty;

        [ObservableProperty] 
        private string _userProfile = string.Empty;

        [ObservableProperty]
        private string _buildNumber = string.Empty;

        [ObservableProperty]
        private string _activationStatus = string.Empty;

        [ObservableProperty]
        private bool _isActivated = false;

        [ObservableProperty]
        private string _windowsUpdateStatus = string.Empty;

        [ObservableProperty]
        private bool _isUpToDate = false;
    }
}