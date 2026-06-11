namespace SYSi.Models
{
    public partial class OsInfo : ObservableObject
    {
        public OsInfo()
        {
            LanguageBase.LanguageChanged += LanguageBase_LanguageChanged;
        }

        public enum IUpdateType
        {
            Unknown = -1,
            UpToDate = 0,
            UpdatesAvailable = 1,
            RestartRequired = 2
        }

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
        private IUpdateType _windowsUpdateType = IUpdateType.Unknown;

        [ObservableProperty]
        private bool _isUpToDate = false;

        private void LanguageBase_LanguageChanged(string language)
        {
            OnIsActivatedChanged(IsActivated);
            OnWindowsUpdateTypeChanged(WindowsUpdateType);
        }

        partial void OnIsActivatedChanged(bool value)
        {
            ActivationStatus = value ? LanguageBase.GetLangValue("wactiv_actived_title") : LanguageBase.GetLangValue("wactiv_inactived_title");
        }

        partial void OnWindowsUpdateTypeChanged(IUpdateType value)
        {
            WindowsUpdateStatus = value switch
            {
                IUpdateType.Unknown => "Unknown",
                IUpdateType.UpToDate => LanguageBase.GetLangValue("wus_up_to_date_title"),
                IUpdateType.UpdatesAvailable => LanguageBase.GetLangValue("wus_updates_available_title"),
                IUpdateType.RestartRequired => LanguageBase.GetLangValue("wus_restart_required_title"),
                _ => "N/A"
            };
        }
    }
}