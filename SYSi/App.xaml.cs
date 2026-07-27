namespace SYSi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            Bootstrap.OnBeforeStartup();

            TranslationSource.Instance.CurrentCulture = LanguageBase.GetSetupLanguage();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                CrashHandler.Handle(ex, "AppDomain");
            }
        }

        public void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            CrashHandler.WriteOnly(e.Exception, "TaskScheduler");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CrashHandler.Handle(e.Exception, "Dispatcher");
        }

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddSingleton<NavigationPanelHostService>();

                services.AddSingleton<IHostedService>(ihsv => ihsv.GetRequiredService<NavigationPanelHostService>());

                services.AddHostedService<ApplicationHostService>();

                services.AddHostedService<PowerModeHostService>();

                // Main window with navigation
                services.AddSingleton<IWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<ThemeManagerHostService>();
                services.AddSingleton<IHostedService>(ihsv => ihsv.GetRequiredService<ThemeManagerHostService>());
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<WindowsProviderService>();

                // Hardware Service
                services.AddSingleton<Services.HardwareService.HardwareService>();
                services.AddSingleton<HardwareHostService>();

                services.AddSingleton<Services.UpdateService.UpdateService>();
                services.AddSingleton<UpdateHostService>();

                services.AddSingleton<OsHostService>();

                NavigationHandle.SetupPageViewModelPairs(services, "SYSi.Views.Pages", "SYSi.ViewModels.Pages");
                NavigationHandle.SetupPageViewModelPairs(services, "SYSi.Views.PagesBottom", "SYSi.ViewModels.PagesBottom");
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Bootstrap.OnStartup();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private void OnExit(object sender, ExitEventArgs e)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

            Bootstrap.OnExit();

            _host.Dispose();
        }

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetRequiredService<T>()
            where T : class
        {
            return _host.Services.GetRequiredService<T>();
        }
    }
}
