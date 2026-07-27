namespace SYSi.Services.HostServices
{
    public class ThemeManagerHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public ThemeManagerHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public WindowBackdropType GetBackdropType()
        {
            return (WindowBackdropType)Enum.Parse(
                typeof(WindowBackdropType),
                UserDataStore.GetValue<string>("IWindowBackdropType")
            );
        }

        public delegate void ThemeChangedHandle(ThemeType theme);

        public event ThemeChangedHandle? OnThemeChanged;

        public Window? MainWindowHandle { get; private set; }

        public bool IsWatcher { get; set; }

        public void Init(Window mainWindow)
        {
            MainWindowHandle = mainWindow;

            InitCornerRadius();
            SetApplicationTheme(GetApplicationTheme());
        }

        public void SetBackdropType(WindowBackdropType _WindowBackdropType)
        {
            UserDataStore.SetValue("IWindowBackdropType", _WindowBackdropType.ToString());

            ThemeApply(GetSysApplicationTheme(), _WindowBackdropType);
        }

        public ThemeConfigs.IThemeType GetApplicationTheme()
        {
            try
            {
                return (ThemeConfigs.IThemeType)Enum.Parse(
                    typeof(ThemeConfigs.IThemeType),
                    UserDataStore.GetValue<string>("IThemeType")
                );
            }
            catch
            {
                return ThemeConfigs.IThemeType.Auto;
            }
        }

        public ThemeType GetSysApplicationTheme()
        {
            ThemeType _ThemeType = ThemeType.Unknown;
            if (UserDataStore.GetValue<string>("IThemeType") == "Auto")
            {
                WindowBackdropType windowBackdropType = GetBackdropType();
                ApplicationThemeManager.ApplySystemTheme(windowBackdropType, true);
                _ThemeType = ApplicationThemeManager.GetSystemAppTheme();
            }
            else
            {
                _ThemeType = (ThemeType)Enum.Parse(
                    typeof(ThemeType),
                    UserDataStore.GetValue<string>("IThemeType")
                );
            }

            return _ThemeType;
        }

        public int GlobalCornerRadius
        {
            get => field;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;

                Application.Current.Resources["ControlCornerRadius"] = new CornerRadius(value);
                UserDataStore.SetValue("ObjectCornerRadius", value);
            }
        } = UserDataStore.GetValue<int>("ObjectCornerRadius");

        public void SetApplicationTheme(ThemeConfigs.IThemeType _IThemeType)
        {
            UnWatch();
            UserDataStore.SetValue("IThemeType", _IThemeType.ToString());
            ThemeType applicationTheme = GetSysApplicationTheme();
            WindowBackdropType windowBackdropType = GetBackdropType();

            if (_IThemeType == ThemeConfigs.IThemeType.Auto)
            {
                Watch(applicationTheme, windowBackdropType);
            }
            else
            {
                ThemeApply(applicationTheme, windowBackdropType);
            }
            OnThemeChanged?.Invoke(applicationTheme);
        }

        public void Watch(ThemeType applicationTheme = ThemeType.Unknown, WindowBackdropType windowBackdrop = WindowBackdropType.Mica, bool updateAccents = true)
        {
            if (!IsWatcher)
            {
                ThemeApply(applicationTheme, windowBackdrop);
                Watcher.Watch(MainWindowHandle, windowBackdrop, updateAccents);
                SystemThemeWatcher.Watch(MainWindowHandle, this.GetBackdropType(), updateAccents);

                IsWatcher = true;
            }
        }

        private void ThemeApply(ThemeType applicationTheme = ThemeType.Light, WindowBackdropType backgroundEffect = WindowBackdropType.Mica)
        {
            ApplicationThemeManager.Apply(applicationTheme, backgroundEffect, true);
        }

        public void UnWatch()
        {
            if (IsWatcher)
            {
                Watcher.UnWatch(MainWindowHandle);
                SystemThemeWatcher.UnWatch(MainWindowHandle);
                IsWatcher = false;
            }
        }

        public void InitCornerRadius()
        {
            Application.Current.Resources["ControlCornerRadius"] = new CornerRadius(GlobalCornerRadius);
        }

        public ObservableCollection<Models.ComboBoxItem> GetThemeCBBs()
        {
            return new ObservableCollection<Models.ComboBoxItem>(
                Enum.GetValues(typeof(ThemeConfigs.IThemeType))
                    .Cast<ThemeConfigs.IThemeType>()
                    .Where(e => e != ThemeConfigs.IThemeType.Unknown)
                    .Select(e => new Models.ComboBoxItem
                    {
                        Value = ((int)e).ToString(),
                        Content = e.ToString()
                    })
            );
        }

        public Models.ComboBoxItem? GetThemeCBBSelected()
        {
            return GetThemeCBBs().FirstOrDefault(x => x.Content == GetApplicationTheme().ToString());
        }

        public ObservableCollection<Models.ComboBoxItem> GetMaterialCBBs()
        {
            return new ObservableCollection<Models.ComboBoxItem>(
                Enum.GetValues(typeof(WindowBackdropType))
                    .Cast<WindowBackdropType>()
                    .Select(e => new Models.ComboBoxItem
                    {
                        Value = ((int)e).ToString(),
                        Content = e.ToString()
                    })
            );
        }

        public Models.ComboBoxItem? GetMaterialCBBSelected()
        {
            return GetMaterialCBBs().FirstOrDefault(x => x.Content == GetBackdropType().ToString());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Init(App.Current.MainWindow);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
