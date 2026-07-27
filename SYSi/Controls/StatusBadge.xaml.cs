namespace SYSi.Controls;

public class StatusBadge : Control
{
    private static readonly DependencyProperty IsHighContrastProperty =
        DependencyProperty.Register(nameof(IsHighContrast), typeof(bool), typeof(StatusBadge),
            new PropertyMetadata(false));

    private ThemeManagerHostService? themeManagerService;

    public bool IsHighContrast
    {
        get => (bool)GetValue(IsHighContrastProperty);
        set => SetValue(IsHighContrastProperty, value);
    }

    static StatusBadge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(StatusBadge),
            new FrameworkPropertyMetadata(typeof(StatusBadge)));
    }

    public StatusBadge()
    {
        themeManagerService = App.GetRequiredService<ThemeManagerHostService>();
        IsHighContrast = themeManagerService?.GetSysApplicationTheme() == ApplicationTheme.HighContrast;
        themeManagerService?.OnThemeChanged += StatusBadge_OnThemeChanged;
    }

    private void StatusBadge_OnThemeChanged(ThemeType theme)
    {
        IsHighContrast = theme == ApplicationTheme.HighContrast;
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge));

    public static readonly DependencyProperty IsSuccessProperty =
        DependencyProperty.Register(nameof(IsSuccess), typeof(bool), typeof(StatusBadge));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsSuccess
    {
        get => (bool)GetValue(IsSuccessProperty);
        set => SetValue(IsSuccessProperty, value);
    }
}
