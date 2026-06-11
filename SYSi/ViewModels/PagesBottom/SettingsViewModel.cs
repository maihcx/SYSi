using SYSi.Services.UpdateService;
using static SYSi.Resources.ThemeConfigs;

namespace SYSi.ViewModels.PagesBottom
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        public event Action? ScrollToUpdateRequested;

        private static ApplicationThemeManagerService? ThemeManagerService = WindowHelper.ThemeManagerService;

        private readonly UpdateHostService updateHostService;

        private readonly HardwareHostService hardwareHostService;

        private readonly OsHostService osHostService;

        [ObservableProperty] private string _appVersion = string.Empty;

        public SettingsViewModel(UpdateHostService updateHostService, HardwareHostService hardwareHostService, OsHostService osHostService)
        {
            this.updateHostService = updateHostService;
            this.hardwareHostService = hardwareHostService;
            this.osHostService = osHostService;
        }

        // ── Update ─────────────────────────────────────────────────────────
        [ObservableProperty]
        private UpdateStatus _updateStatus = UpdateStatus.Idle;

        [ObservableProperty]
        private string _updateStatusText = string.Empty;

        [ObservableProperty]
        private string _latestVersion = string.Empty;

        [ObservableProperty]
        private string _releaseNotes = string.Empty;

        [ObservableProperty]
        private double _downloadProgress = 0;

        [ObservableProperty]
        private bool _isUpdateAvailable = false;

        [ObservableProperty]
        private bool _isReadyToInstall = false;

        [ObservableProperty]
        private bool _isDownloadReady = false;

        [ObservableProperty]
        private bool _isChecking = false;

        [ObservableProperty]
        private bool _isDownloading = false;

        partial void OnUpdateStatusChanged(UpdateStatus value)
        {
            IsChecking        = value == UpdateStatus.Checking;
            IsDownloading     = value == UpdateStatus.Downloading;
            IsReadyToInstall  = value == UpdateStatus.ReadyToInstall;
            IsDownloadReady   = value == UpdateStatus.UpdateAvailable;
            IsUpdateAvailable = value is UpdateStatus.UpdateAvailable
                                      or UpdateStatus.Downloading
                                      or UpdateStatus.ReadyToInstall;
        }

        private void OnHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(UpdateHostService.Status):
                        SyncStatusFromHost();
                        break;

                    case nameof(UpdateHostService.DownloadProgress):
                        DownloadProgress = updateHostService.DownloadProgress * 100;
                        UpdateStatusText = string.Format(
                            LanguageBase.GetLangValue("page_settings_update_downloading_progress"),
                            (int)(updateHostService.DownloadProgress * 100));
                        break;

                    case nameof(UpdateHostService.LatestRelease):
                        if (updateHostService.LatestRelease is { } release)
                        {
                            LatestVersion = release.TagName;
                            ReleaseNotes  = release.Body;
                        }
                        break;
                }
            });
        }

        private void SyncStatusFromHost()
        {
            UpdateStatus = updateHostService.Status;

            UpdateStatusText = updateHostService.Status switch
            {
                UpdateStatus.Idle => LanguageBase.GetLangValue("page_settings_update_idle"),
                UpdateStatus.Checking => LanguageBase.GetLangValue("page_settings_update_checking"),
                UpdateStatus.UpToDate => LanguageBase.GetLangValue("page_settings_update_uptodate"),
                UpdateStatus.Downloading => LanguageBase.GetLangValue("page_settings_update_downloading"),
                UpdateStatus.ReadyToInstall => LanguageBase.GetLangValue("page_settings_update_ready"),
                UpdateStatus.UpdateAvailable => string.Format(LanguageBase.GetLangValue("page_settings_update_available", updateHostService.LatestRelease?.TagName ?? "0.0.0")),
                UpdateStatus.Error => updateHostService.ErrorMessage
                    ?? LanguageBase.GetLangValue("page_settings_update_error"),
                _ => string.Empty
            };
        }

        #region Navigation panel auto hide
        [ObservableProperty]
        private bool _autoHideNavigationPanel = WindowHelper.IsAutoHideNavPanel;

        partial void OnAutoHideNavigationPanelChanged(bool oldValue, bool newValue)
        {
            WindowHelper.IsAutoHideNavPanel = AutoHideNavigationPanel = newValue;
        }
        #endregion

        #region Language list handle
        [ObservableProperty]
        private LanguageItem _selectedLanguage = LanguageBase.GetCurrentLanguageItem();

        [ObservableProperty]
        private ObservableCollection<LanguageItem> _languages = LanguageBase.GetLanguageItems();

        partial void OnSelectedLanguageChanged(LanguageItem value)
        {
            LanguageBase.SetLanguage(value.Code ?? "en");
        }
        #endregion

        #region Theme list handle
        [ObservableProperty]
        private Models.ComboBoxItem? _selectedTheme = ThemeManagerService?.GetThemeCBBSelected();

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem>? _themeList = ThemeManagerService?.GetThemeCBBs();

        partial void OnSelectedThemeChanged(Models.ComboBoxItem? value)
        {
            ThemeManagerService?.SetApplicationTheme(Enum.Parse<IThemeType>(value?.Value ?? "Mica"));
        }
        #endregion

        #region Material list handle
        [ObservableProperty]
        private Models.ComboBoxItem? _selectedMaterial = ThemeManagerService?.GetMaterialCBBSelected();

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem>? _materialList = ThemeManagerService?.GetMaterialCBBs();

        partial void OnSelectedMaterialChanged(Models.ComboBoxItem? value)
        {
            ThemeManagerService?.SetBackdropType(Enum.Parse<WindowBackdropType>(value?.Value ?? "Mica"));
            ThemeManagerService?.SetApplicationTheme(Enum.Parse<IThemeType>(SelectedTheme?.Value ?? "Auto"));
        }
        #endregion

        #region CornerRadius list handle
        [ObservableProperty]
        private int _sliderCornerRadius = ThemeManagerService?.GlobalCornerRadius ?? 0;

        partial void OnSliderCornerRadiusChanged(int oldValue, int newValue)
        {
            ThemeManagerService?.GlobalCornerRadius = newValue;
        }
        #endregion

        #region Timer Interval Refresher handle
        private static readonly Dictionary<int, string> refreshIntervalList = new()
        {
            {500, "high_title" },
            {1000, "normal_title" },
            {2000, "low_title" },
            {-1, "paused_title" },

        };

        [ObservableProperty]
        private ObservableCollection<ComboBoxItemInt>? _refreshIntervalList = new() {
            new()
            {
                Value = 500, ContentKey = refreshIntervalList[500]
            },
            new()
            {
                Value = 1000, ContentKey = refreshIntervalList[1000]
            },
            new()
            {
                Value = 2000, ContentKey = refreshIntervalList[2000]
            },
            new()
            {
                Value = -1, ContentKey = refreshIntervalList[-1]
            }
        };

        [ObservableProperty]
        private ComboBoxItemInt? _selectedRefreshInterval = new() 
        { 
            Value = UserDataStore.GetValue<int>("RefreshInfoInterval"), 
            ContentKey = refreshIntervalList[UserDataStore.GetValue<int>("RefreshInfoInterval")] 
        };

        partial void OnSelectedRefreshIntervalChanged(ComboBoxItemInt? value)
        {
            int intValue = value?.Value ?? -1;
            UserDataStore.SetValue("RefreshInfoInterval", intValue);
            hardwareHostService.SetRefreshInterval(intValue);
            osHostService.SetRefreshInterval(intValue);
        }
        #endregion

        [ObservableProperty]
        private string _copyRight = AppInfoHelper.CopyRight;

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            ScrollToUpdateRequested?.Invoke();

            updateHostService.PropertyChanged += OnHostPropertyChanged;

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            updateHostService.PropertyChanged -= OnHostPropertyChanged;

            return Task.CompletedTask;
        }

        private void InitializeViewModel()
        {
            var v = UpdateService.GetCurrentVersion();
            AppVersion = $"SYSi - {v.Major}.{v.Minor}.{v.Build}";
            UpdateStatusText = LanguageBase.GetLangValue("page_settings_update_idle");
            _isInitialized = true;

            _ = CheckForUpdateAsync();

            LanguageBase.LanguageChanged += (string lang) =>
            {
                _ = CheckForUpdateAsync();
            };
        }


        [RelayCommand]
        private Task CheckForUpdateAsync()
        {
            LatestVersion    = string.Empty;
            ReleaseNotes     = string.Empty;
            DownloadProgress = 0;
            return updateHostService.CheckAsync();
        }

        [RelayCommand]
        private Task DownloadAndInstallAsync()
        {
            return updateHostService.DownloadAsync();
        }

        [RelayCommand]
        private void InstallUpdate()
        {
            try { updateHostService.LaunchInstaller(); }
            catch (Exception ex)
            {
                UpdateStatus     = UpdateStatus.Error;
                UpdateStatusText = ex.Message;
            }
        }

        [RelayCommand]
        private void CancelUpdate()
        {
            updateHostService.Cancel();
            DownloadProgress = 0;
        }
    }
}
