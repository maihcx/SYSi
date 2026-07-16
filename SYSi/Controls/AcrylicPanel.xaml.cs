using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SYSi.Controls;

/// <summary>
/// Displays a cached backdrop through a real GPU displacement shader.
/// The shader bends the background near a rounded perimeter and samples the
/// red, green and blue channels at different offsets to produce the optical
/// fringe used by Liquid Glass interfaces.
///
/// The backdrop snapshot and optional blur remain shared between panels, so
/// scrolling does not recreate a bitmap or a BlurEffect for every control.
/// </summary>
public class AcrylicPanel : ContentControl
{
    private static readonly ConditionalWeakTable<FrameworkElement, BackdropSourceCache>
        BackdropCaches = new();

    private ImageBrush? _backdropBrush;
    private Grid? _rootGrid;
    private Grid? _effectHost;
    private Border? _backdropLayer;
    private Grid? _glassRimHost;
    private Border? _glassRimLayer;
    private Geometry? _roundedClipGeometry;
    private LiquidGlassEffect? _liquidGlassEffect;
    private LiquidGlassEffect? _glassRimEffect;
    private ScrollViewer? _scrollViewer;
    private BackdropSourceCache? _sourceCache;
    private DispatcherTimer? _scrollIdleTimer;
    private bool _backdropRefreshQueued;
    private bool _viewboxUpdateQueued;
    private bool _isScrolling;
    private bool _shaderInitializationFailed;

    static AcrylicPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(typeof(AcrylicPanel)));
    }

    public AcrylicPanel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    #region BackgroundSource

    public static readonly DependencyProperty BackgroundSourceProperty =
        DependencyProperty.Register(
            nameof(BackgroundSource),
            typeof(FrameworkElement),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                null,
                OnBackgroundSourceChanged));

    /// <summary>
    /// The dedicated visual that contains the page/window background.
    /// For best results, point this to a background-only element rather than
    /// a root element that also contains AcrylicPanel instances.
    /// </summary>
    public FrameworkElement? BackgroundSource
    {
        get => (FrameworkElement?)GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    private static void OnBackgroundSourceChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is AcrylicPanel panel)
        {
            panel.ChangeBackgroundSource();
        }
    }

    #endregion

    #region BlurRadius

    public static readonly DependencyProperty BlurRadiusProperty =
        DependencyProperty.Register(
            nameof(BlurRadius),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.0,
                OnBackdropSettingChanged,
                CoerceBlurRadius));

    public double BlurRadius
    {
        get => (double)GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    private static object CoerceBlurRadius(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double radius = (double)baseValue;

        if (!double.IsFinite(radius))
        {
            return 0.0;
        }

        return Math.Clamp(radius, 0.0, 100.0);
    }

    #endregion

    #region BackdropScale

    public static readonly DependencyProperty BackdropScaleProperty =
        DependencyProperty.Register(
            nameof(BackdropScale),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.65,
                OnBackdropSettingChanged,
                CoerceBackdropScale));

    /// <summary>
    /// Resolution scale used for the shared backdrop snapshot.
    /// 0.5 is normally the best balance for scrolling performance.
    /// Use 1.0 for maximum quality or 0.35 for weaker GPUs.
    /// </summary>
    public double BackdropScale
    {
        get => (double)GetValue(BackdropScaleProperty);
        set => SetValue(BackdropScaleProperty, value);
    }

    private static object CoerceBackdropScale(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double scale = (double)baseValue;

        if (!double.IsFinite(scale))
        {
            return 0.65;
        }

        return Math.Clamp(scale, 0.25, 1.0);
    }

    private static void OnBackdropSettingChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is AcrylicPanel panel)
        {
            panel.QueueBackdropRefresh();
        }
    }

    #endregion

    #region TintBrush

    public static readonly DependencyProperty TintBrushProperty =
        DependencyProperty.Register(
            nameof(TintBrush),
            typeof(Brush),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                CreateDefaultTintBrush(),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush TintBrush
    {
        get => (Brush)GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }

    private static Brush CreateDefaultTintBrush()
    {
        var brush = new SolidColorBrush(
            Color.FromArgb(255, 255, 255, 255));

        brush.Freeze();
        return brush;
    }

    #endregion

    #region BlurBrush

    private static readonly DependencyPropertyKey BlurBrushPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(BlurBrush),
            typeof(Brush),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty BlurBrushProperty =
        BlurBrushPropertyKey.DependencyProperty;

    public Brush? BlurBrush =>
        (Brush?)GetValue(BlurBrushProperty);

    #endregion

    #region CornerRadius

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                new CornerRadius(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLiquidGlassSettingChanged));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    #endregion

    #region Liquid Glass appearance

    public static readonly DependencyProperty IsLiquidGlassEnabledProperty =
        DependencyProperty.Register(
            nameof(IsLiquidGlassEnabled),
            typeof(bool),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                true,
                OnLiquidGlassSettingChanged));

    public bool IsLiquidGlassEnabled
    {
        get => (bool)GetValue(IsLiquidGlassEnabledProperty);
        set => SetValue(IsLiquidGlassEnabledProperty, value);
    }

    public static readonly DependencyProperty GlassOpacityProperty =
        DependencyProperty.Register(
            nameof(GlassOpacity),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                1.0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                null,
                CoerceUnitValue));

    public double GlassOpacity
    {
        get => (double)GetValue(GlassOpacityProperty);
        set => SetValue(GlassOpacityProperty, value);
    }

    public static readonly DependencyProperty TintOpacityProperty =
        DependencyProperty.Register(
            nameof(TintOpacity),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.06,
                FrameworkPropertyMetadataOptions.AffectsRender,
                null,
                CoerceUnitValue));

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    public static readonly DependencyProperty BackgroundOpacityProperty =
        DependencyProperty.Register(
            nameof(BackgroundOpacity),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                null,
                CoerceUnitValue));

    public double BackgroundOpacity
    {
        get => (double)GetValue(BackgroundOpacityProperty);
        set => SetValue(BackgroundOpacityProperty, value);
    }

    public static readonly DependencyProperty SheenOpacityProperty =
        DependencyProperty.Register(
            nameof(SheenOpacity),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.14,
                FrameworkPropertyMetadataOptions.AffectsRender,
                null,
                CoerceUnitValue));

    public double SheenOpacity
    {
        get => (double)GetValue(SheenOpacityProperty);
        set => SetValue(SheenOpacityProperty, value);
    }

    public static readonly DependencyProperty RefractionDepthProperty =
        DependencyProperty.Register(
            nameof(RefractionDepth),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                10.0,
                OnLiquidGlassSettingChanged,
                CoerceRefractionDepth));

    /// <summary>
    /// Width, in device-independent pixels, of the refractive perimeter.
    /// </summary>
    public double RefractionDepth
    {
        get => (double)GetValue(RefractionDepthProperty);
        set => SetValue(RefractionDepthProperty, value);
    }

    public static readonly DependencyProperty RimWidthProperty =
        DependencyProperty.Register(
            nameof(RimWidth),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                12.0,
                OnLiquidGlassSettingChanged,
                CoerceRimWidth));

    /// <summary>
    /// Width, in device-independent pixels, of the visible refractive glass
    /// ring. The ring samples the real backdrop; it is not a painted border.
    /// </summary>
    public double RimWidth
    {
        get => (double)GetValue(RimWidthProperty);
        set => SetValue(RimWidthProperty, value);
    }

    public static readonly DependencyProperty RefractionStrengthProperty =
        DependencyProperty.Register(
            nameof(RefractionStrength),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                100.0,
                OnLiquidGlassSettingChanged,
                CoerceRefractionStrength));

    /// <summary>
    /// Displacement strength. A value of 100 matches the scale used by the
    /// reference web component and produces an intentionally visible lens.
    /// </summary>
    public double RefractionStrength
    {
        get => (double)GetValue(RefractionStrengthProperty);
        set => SetValue(RefractionStrengthProperty, value);
    }

    public static readonly DependencyProperty ChromaticAberrationProperty =
        DependencyProperty.Register(
            nameof(ChromaticAberration),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                2.0,
                OnLiquidGlassSettingChanged,
                CoerceChromaticAberration));

    public double ChromaticAberration
    {
        get => (double)GetValue(ChromaticAberrationProperty);
        set => SetValue(ChromaticAberrationProperty, value);
    }

    public static readonly DependencyProperty SaturationProperty =
        DependencyProperty.Register(
            nameof(Saturation),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                1.5,
                OnLiquidGlassSettingChanged,
                CoerceColorMultiplier));

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public static readonly DependencyProperty BrightnessProperty =
        DependencyProperty.Register(
            nameof(Brightness),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                1.1,
                OnLiquidGlassSettingChanged,
                CoerceColorMultiplier));

    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    public static readonly DependencyProperty EdgeHighlightProperty =
        DependencyProperty.Register(
            nameof(EdgeHighlight),
            typeof(double),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                0.18,
                OnLiquidGlassSettingChanged,
                CoerceUnitValue));

    public double EdgeHighlight
    {
        get => (double)GetValue(EdgeHighlightProperty);
        set => SetValue(EdgeHighlightProperty, value);
    }

    public static readonly DependencyProperty LightDirectionProperty =
        DependencyProperty.Register(
            nameof(LightDirection),
            typeof(Point),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                new Point(-0.72, -0.69),
                OnLiquidGlassSettingChanged));

    public Point LightDirection
    {
        get => (Point)GetValue(LightDirectionProperty);
        set => SetValue(LightDirectionProperty, value);
    }

    public static readonly DependencyProperty ReduceEffectsWhileScrollingProperty =
        DependencyProperty.Register(
            nameof(ReduceEffectsWhileScrolling),
            typeof(bool),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(
                false,
                OnLiquidGlassSettingChanged));

    public bool ReduceEffectsWhileScrolling
    {
        get => (bool)GetValue(ReduceEffectsWhileScrollingProperty);
        set => SetValue(ReduceEffectsWhileScrollingProperty, value);
    }

    private static void OnLiquidGlassSettingChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is AcrylicPanel panel)
        {
            panel.UpdateLiquidGlassEffect();

            if (eventArgs.Property == CornerRadiusProperty ||
                eventArgs.Property == RimWidthProperty)
            {
                panel.UpdateRoundedClip();
            }
        }
    }

    private static object CoerceUnitValue(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 0.0;
    }

    private static object CoerceRefractionDepth(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.5, 64.0) : 10.0;
    }

    private static object CoerceRimWidth(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.5, 64.0) : 12.0;
    }

    private static object CoerceRefractionStrength(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.0, 250.0) : 100.0;
    }

    private static object CoerceChromaticAberration(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.0, 20.0) : 2.0;
    }

    private static object CoerceColorMultiplier(
        DependencyObject dependencyObject,
        object baseValue)
    {
        double value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.0, 3.0) : 1.0;
    }

    #endregion

    #region Command

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(AcrylicPanel),
            new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(AcrylicPanel),
            new PropertyMetadata(null));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    #region IsPressed

    private static readonly DependencyPropertyKey IsPressedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsPressed),
            typeof(bool),
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsPressedProperty =
        IsPressedPropertyKey.DependencyProperty;

    public bool IsPressed =>
        (bool)GetValue(IsPressedProperty);

    #endregion

    public override void OnApplyTemplate()
    {
        if (_backdropLayer is not null)
        {
            _backdropLayer.Effect = null;
        }

        if (_glassRimLayer is not null)
        {
            _glassRimLayer.Effect = null;
        }

        base.OnApplyTemplate();

        _rootGrid = GetTemplateChild("PART_RootGrid") as Grid;
        _effectHost = GetTemplateChild("PART_EffectHost") as Grid;
        _backdropLayer = GetTemplateChild("PART_BackdropLayer") as Border;
        _glassRimHost = GetTemplateChild("PART_GlassRimHost") as Grid;
        _glassRimLayer = GetTemplateChild("PART_GlassRimLayer") as Border;

        UpdateRoundedClip();
        UpdateLiquidGlassEffect();
        QueueBackdropRefresh();
        QueueViewboxUpdate();
    }

    /// <summary>
    /// Invalidates the shared snapshot used by every panel attached to the
    /// supplied background source. Call this after the background image,
    /// theme, gradient or other visual content changes.
    /// </summary>
    public static void InvalidateBackdrop(FrameworkElement backgroundSource)
    {
        ArgumentNullException.ThrowIfNull(backgroundSource);
        backgroundSource.Dispatcher.VerifyAccess();

        if (BackdropCaches.TryGetValue(backgroundSource, out BackdropSourceCache? cache))
        {
            cache.InvalidateAndNotify();
        }
    }

    /// <summary>
    /// Refreshes the shared backdrop used by this panel and all other panels
    /// that reference the same BackgroundSource.
    /// </summary>
    public void RefreshBackdrop()
    {
        _sourceCache?.InvalidateAndNotify();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachToBackgroundSource();
        AttachToScrollViewer();

        QueueBackdropRefresh();
        QueueViewboxUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachFromScrollViewer();
        DetachFromBackgroundSource();

        _scrollIdleTimer?.Stop();
        _isScrolling = false;
        _backdropRefreshQueued = false;
        _viewboxUpdateQueued = false;
    }

    private void OnSizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        UpdateRoundedClip();
        UpdateLiquidGlassEffect();
        QueueViewboxUpdate();
    }

    private void OnIsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            QueueViewboxUpdate();
        }
    }

    private void ChangeBackgroundSource()
    {
        DetachFromBackgroundSource();

        if (IsLoaded)
        {
            AttachToBackgroundSource();
        }

        QueueBackdropRefresh();
    }

    private void AttachToBackgroundSource()
    {
        if (BackgroundSource is null || _sourceCache is not null)
        {
            return;
        }

        _sourceCache = BackdropCaches.GetValue(
            BackgroundSource,
            static source => new BackdropSourceCache(source));

        _sourceCache.Register(this);
    }

    private void DetachFromBackgroundSource()
    {
        _sourceCache?.Unregister(this);
        _sourceCache = null;
    }

    private void AttachToScrollViewer()
    {
        DetachFromScrollViewer();

        _scrollViewer = FindVisualAncestor<ScrollViewer>(this);

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void DetachFromScrollViewer()
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }
    }

    private void OnScrollChanged(
        object sender,
        ScrollChangedEventArgs e)
    {
        QueueViewboxUpdate();

        if (!ReduceEffectsWhileScrolling)
        {
            return;
        }

        _isScrolling = true;
        UpdateLiquidGlassEffect();

        _scrollIdleTimer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(120),
            DispatcherPriority.Background,
            OnScrollIdleTick,
            Dispatcher);

        _scrollIdleTimer.Stop();
        _scrollIdleTimer.Start();
    }

    private void OnScrollIdleTick(object? sender, EventArgs e)
    {
        _scrollIdleTimer?.Stop();
        _isScrolling = false;
        UpdateLiquidGlassEffect();
    }

    private void QueueBackdropRefresh()
    {
        if (_backdropRefreshQueued || !IsLoaded)
        {
            return;
        }

        _backdropRefreshQueued = true;

        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(() =>
            {
                _backdropRefreshQueued = false;
                UpdateBackdropBrush();
            }));
    }

    private void QueueViewboxUpdate()
    {
        if (_viewboxUpdateQueued || !IsLoaded)
        {
            return;
        }

        _viewboxUpdateQueued = true;

        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(() =>
            {
                _viewboxUpdateQueued = false;
                UpdateViewbox();
            }));
    }

    private void UpdateBackdropBrush()
    {
        if (!IsLoaded || BackgroundSource is null)
        {
            ClearBackdropBrush();
            return;
        }

        AttachToBackgroundSource();

        BitmapSource? snapshot = _sourceCache?.GetSnapshot(
            BlurRadius,
            BackdropScale);

        if (snapshot is null)
        {
            ClearBackdropBrush();
            return;
        }

        if (_backdropBrush?.ImageSource != snapshot)
        {
            _backdropBrush = new ImageBrush(snapshot)
            {
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                Stretch = Stretch.Fill,
                TileMode = TileMode.None,
                ViewboxUnits = BrushMappingMode.Absolute,
                ViewportUnits = BrushMappingMode.Absolute
            };

            SetValue(BlurBrushPropertyKey, _backdropBrush);
        }

        UpdateViewbox();
    }

    private void ClearBackdropBrush()
    {
        _backdropBrush = null;
        SetValue(BlurBrushPropertyKey, null);
    }

    private void UpdateViewbox()
    {
        if (_backdropBrush is null ||
            BackgroundSource is null ||
            !IsLoaded ||
            !IsVisible ||
            ActualWidth <= 0 ||
            ActualHeight <= 0)
        {
            return;
        }

        try
        {
            GeneralTransform transform = TransformToVisual(BackgroundSource);
            Point topLeft = transform.Transform(new Point(0, 0));

            _backdropBrush.Viewbox = new Rect(
                topLeft.X,
                topLeft.Y,
                ActualWidth,
                ActualHeight);

            _backdropBrush.Viewport = new Rect(
                0,
                0,
                ActualWidth,
                ActualHeight);

            bool isInsideViewport = IsInsideScrollViewport();

            if (_backdropLayer is not null)
            {
                _backdropLayer.Visibility = isInsideViewport
                    ? Visibility.Visible
                    : Visibility.Hidden;
            }

            if (_glassRimLayer is not null)
            {
                _glassRimLayer.Visibility = isInsideViewport
                    ? Visibility.Visible
                    : Visibility.Hidden;
            }

            if (isInsideViewport)
            {
                UpdateLiquidGlassEffect();
            }
        }
        catch (InvalidOperationException)
        {
            // The elements can temporarily belong to different visual trees
            // while navigation or virtualization is rebuilding the page.
        }
        catch (ArgumentException)
        {
            // TransformToVisual can fail briefly during visual-tree changes.
        }
    }

    private bool IsInsideScrollViewport()
    {
        if (_scrollViewer is null ||
            _scrollViewer.ActualWidth <= 0 ||
            _scrollViewer.ActualHeight <= 0)
        {
            return true;
        }

        try
        {
            GeneralTransform transform = TransformToVisual(_scrollViewer);
            Rect panelBounds = transform.TransformBounds(
                new Rect(0, 0, ActualWidth, ActualHeight));

            Rect viewportBounds = new(
                0,
                0,
                _scrollViewer.ActualWidth,
                _scrollViewer.ActualHeight);

            return panelBounds.IntersectsWith(viewportBounds);
        }
        catch (InvalidOperationException)
        {
            return true;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private void UpdateRoundedClip()
    {
        if (_rootGrid is null || _effectHost is null)
        {
            return;
        }

        double width = ActualWidth;
        double height = ActualHeight;

        if (width <= 0 || height <= 0)
        {
            _rootGrid.Clip = null;
            _effectHost.Clip = null;

            if (_glassRimHost is not null)
            {
                _glassRimHost.Clip = null;
            }

            return;
        }

        Rect bounds = new(0, 0, width, height);
        Geometry geometry = CreateRoundedRectangleGeometry(bounds, CornerRadius);

        _roundedClipGeometry = geometry;

        // Clip the final root composition and the main shader host directly.
        // The rim host receives a ring-shaped clip built from the same rounded
        // geometry, so the visible border is made of real backdrop pixels.
        _rootGrid.Clip = geometry;
        _effectHost.Clip = geometry;

        if (_glassRimHost is not null)
        {
            _glassRimHost.Clip = CreateGlassRimGeometry(
                bounds,
                CornerRadius,
                RimWidth);
        }
    }

    private static Geometry CreateGlassRimGeometry(
        Rect rect,
        CornerRadius cornerRadius,
        double rimWidth)
    {
        Geometry outer = CreateRoundedRectangleGeometry(rect, cornerRadius);

        double normalizedRimWidth = Math.Max(0.5, rimWidth);
        double innerWidth = rect.Width - normalizedRimWidth * 2.0;
        double innerHeight = rect.Height - normalizedRimWidth * 2.0;

        if (innerWidth <= 0 || innerHeight <= 0)
        {
            return outer;
        }

        Rect innerRect = new(
            rect.Left + normalizedRimWidth,
            rect.Top + normalizedRimWidth,
            innerWidth,
            innerHeight);

        CornerRadius innerCornerRadius = new(
            Math.Max(0.0, cornerRadius.TopLeft - normalizedRimWidth),
            Math.Max(0.0, cornerRadius.TopRight - normalizedRimWidth),
            Math.Max(0.0, cornerRadius.BottomRight - normalizedRimWidth),
            Math.Max(0.0, cornerRadius.BottomLeft - normalizedRimWidth));

        Geometry inner = CreateRoundedRectangleGeometry(
            innerRect,
            innerCornerRadius);

        CombinedGeometry rimGeometry = new(
            GeometryCombineMode.Exclude,
            outer,
            inner);

        if (rimGeometry.CanFreeze)
        {
            rimGeometry.Freeze();
        }

        return rimGeometry;
    }

    private static Geometry CreateRoundedRectangleGeometry(
        Rect rect,
        CornerRadius cornerRadius)
    {
        double topLeft = Math.Max(0, cornerRadius.TopLeft);
        double topRight = Math.Max(0, cornerRadius.TopRight);
        double bottomRight = Math.Max(0, cornerRadius.BottomRight);
        double bottomLeft = Math.Max(0, cornerRadius.BottomLeft);

        double scale = 1.0;

        ScaleForPair(ref scale, rect.Width, topLeft + topRight);
        ScaleForPair(ref scale, rect.Width, bottomLeft + bottomRight);
        ScaleForPair(ref scale, rect.Height, topLeft + bottomLeft);
        ScaleForPair(ref scale, rect.Height, topRight + bottomRight);

        if (scale < 1.0)
        {
            topLeft *= scale;
            topRight *= scale;
            bottomRight *= scale;
            bottomLeft *= scale;
        }

        double left = rect.Left;
        double top = rect.Top;
        double right = rect.Right;
        double bottom = rect.Bottom;

        PathFigure figure = new()
        {
            StartPoint = new Point(left + topLeft, top),
            IsClosed = true,
            IsFilled = true
        };

        figure.Segments.Add(new LineSegment(new Point(right - topRight, top), true));
        AddCornerArc(figure, new Point(right, top + topRight), topRight);

        figure.Segments.Add(new LineSegment(new Point(right, bottom - bottomRight), true));
        AddCornerArc(figure, new Point(right - bottomRight, bottom), bottomRight);

        figure.Segments.Add(new LineSegment(new Point(left + bottomLeft, bottom), true));
        AddCornerArc(figure, new Point(left, bottom - bottomLeft), bottomLeft);

        figure.Segments.Add(new LineSegment(new Point(left, top + topLeft), true));
        AddCornerArc(figure, new Point(left + topLeft, top), topLeft);

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);

        if (geometry.CanFreeze)
        {
            geometry.Freeze();
        }

        return geometry;
    }

    private static void ScaleForPair(
        ref double scale,
        double availableLength,
        double radiusSum)
    {
        if (radiusSum > availableLength && radiusSum > 0)
        {
            scale = Math.Min(scale, availableLength / radiusSum);
        }
    }

    private static void AddCornerArc(
        PathFigure figure,
        Point endPoint,
        double radius)
    {
        if (radius <= 0)
        {
            figure.Segments.Add(new LineSegment(endPoint, true));
            return;
        }

        figure.Segments.Add(new ArcSegment(
            endPoint,
            new Size(radius, radius),
            0,
            false,
            SweepDirection.Clockwise,
            true));
    }

    private void UpdateLiquidGlassEffect()
    {
        if (_backdropLayer is null)
        {
            return;
        }

        if (!IsLiquidGlassEnabled || _shaderInitializationFailed)
        {
            _backdropLayer.Effect = null;

            if (_glassRimLayer is not null)
            {
                _glassRimLayer.Effect = null;
            }

            return;
        }

        LiquidGlassEffect mainEffect;
        LiquidGlassEffect? rimEffect = null;

        try
        {
            mainEffect = _liquidGlassEffect ??= new LiquidGlassEffect();
            _backdropLayer.Effect = mainEffect;

            if (_glassRimLayer is not null)
            {
                rimEffect = _glassRimEffect ??= new LiquidGlassEffect();
                _glassRimLayer.Effect = rimEffect;
            }
        }
        catch (Exception exception) when (
            exception is IOException or
            InvalidOperationException or
            ArgumentException or
            TypeInitializationException)
        {
            // A missing/uncompiled .ps resource should degrade to the shared
            // backdrop instead of preventing the whole control from loading.
            _shaderInitializationFailed = true;
            _backdropLayer.Effect = null;

            if (_glassRimLayer is not null)
            {
                _glassRimLayer.Effect = null;
            }

            return;
        }

        double width = Math.Max(1.0, ActualWidth);
        double height = Math.Max(1.0, ActualHeight);
        double maximumRadius = Math.Max(0.5, Math.Min(width, height) * 0.5 - 0.5);
        double radius = Math.Clamp(CornerRadius.TopLeft, 0.5, maximumRadius);

        double scrollingStrengthFactor =
            ReduceEffectsWhileScrolling && _isScrolling ? 0.72 : 1.0;

        double scrollingChromaticFactor =
            ReduceEffectsWhileScrolling && _isScrolling ? 0.78 : 1.0;

        ConfigureLiquidGlassEffect(
            mainEffect,
            width,
            height,
            radius,
            RefractionDepth,
            RefractionStrength * scrollingStrengthFactor,
            ChromaticAberration * scrollingChromaticFactor,
            Saturation,
            Brightness,
            EdgeHighlight);

        if (rimEffect is not null && _glassRimLayer is not null)
        {
            // The rim is made from the same real backdrop, but receives a
            // stronger optical treatment so it behaves like a thick glass edge
            // instead of a painted stroke.
            ConfigureLiquidGlassEffect(
                rimEffect,
                width,
                height,
                radius,
                Math.Max(RefractionDepth, RimWidth),
                RefractionStrength * 1.35 * scrollingStrengthFactor,
                ChromaticAberration * 1.40 * scrollingChromaticFactor,
                Math.Min(3.0, Saturation * 1.05),
                Math.Min(3.0, Brightness * 1.06),
                Math.Min(1.0, EdgeHighlight * 1.55));
        }
    }

    private void ConfigureLiquidGlassEffect(
        LiquidGlassEffect effect,
        double width,
        double height,
        double radius,
        double refractionDepth,
        double refractionStrength,
        double chromaticAberration,
        double saturation,
        double brightness,
        double edgeHighlight)
    {
        effect.InputSize = new Point(width, height);
        effect.CornerRadius = radius;
        effect.RefractionDepth = refractionDepth;
        effect.RefractionStrength = refractionStrength;
        effect.ChromaticAberration = chromaticAberration;
        effect.Saturation = saturation;
        effect.Brightness = brightness;
        effect.EdgeHighlight = edgeHighlight;
        effect.LightDirection = LightDirection;
    }

    protected override void OnMouseLeftButtonDown(
        MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (Command is null)
        {
            return;
        }

        SetValue(IsPressedPropertyKey, true);
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(
        MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        bool wasPressed = IsPressed;
        bool isInside = IsMouseInside(e);

        SetValue(IsPressedPropertyKey, false);

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        if (wasPressed &&
            isInside &&
            Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        if (wasPressed)
        {
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(
        MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        SetValue(IsPressedPropertyKey, false);
    }

    private bool IsMouseInside(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        return position.X >= 0 &&
               position.Y >= 0 &&
               position.X <= ActualWidth &&
               position.Y <= ActualHeight;
    }

    private static T? FindVisualAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        DependencyObject? current = VisualTreeHelper.GetParent(child);

        while (current is not null)
        {
            if (current is T result)
            {
                return result;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private sealed class BackdropSourceCache
    {
        private readonly FrameworkElement _source;
        private readonly List<WeakReference<AcrylicPanel>> _panels = [];
        private readonly Dictionary<SourceSnapshotKey, BitmapSource> _sourceSnapshots = [];
        private readonly Dictionary<SnapshotKey, BitmapSource> _blurredSnapshots = [];

        public BackdropSourceCache(FrameworkElement source)
        {
            _source = source;

            _source.SizeChanged += OnSourceSizeChanged;
        }

        public void Register(AcrylicPanel panel)
        {
            RemoveDeadPanels();

            foreach (WeakReference<AcrylicPanel> reference in _panels)
            {
                if (reference.TryGetTarget(out AcrylicPanel? existing) &&
                    ReferenceEquals(existing, panel))
                {
                    return;
                }
            }

            _panels.Add(new WeakReference<AcrylicPanel>(panel));
        }

        public void Unregister(AcrylicPanel panel)
        {
            for (int index = _panels.Count - 1; index >= 0; index--)
            {
                if (!_panels[index].TryGetTarget(out AcrylicPanel? existing) ||
                    ReferenceEquals(existing, panel))
                {
                    _panels.RemoveAt(index);
                }
            }
        }

        public BitmapSource? GetSnapshot(
            double blurRadius,
            double scale)
        {
            _source.Dispatcher.VerifyAccess();

            if (!_source.IsLoaded ||
                _source.ActualWidth <= 0 ||
                _source.ActualHeight <= 0)
            {
                return null;
            }

            DpiScale dpi = VisualTreeHelper.GetDpi(_source);
            SourceSnapshotKey sourceKey = SourceSnapshotKey.Create(
                _source.ActualWidth,
                _source.ActualHeight,
                dpi,
                scale);

            if (!_sourceSnapshots.TryGetValue(sourceKey, out BitmapSource? sourceSnapshot))
            {
                sourceSnapshot = CaptureSource(sourceKey);

                if (sourceSnapshot is null)
                {
                    return null;
                }

                _sourceSnapshots[sourceKey] = sourceSnapshot;
            }

            if (blurRadius <= 0)
            {
                return sourceSnapshot;
            }

            SnapshotKey key = new(sourceKey, Quantize(blurRadius));

            if (_blurredSnapshots.TryGetValue(key, out BitmapSource? cached))
            {
                return cached;
            }

            try
            {
                BitmapSource blurred = CreateBlurredSnapshot(
                    sourceSnapshot,
                    sourceKey,
                    key.BlurRadius);

                _blurredSnapshots[key] = blurred;
                return blurred;
            }
            catch (InvalidOperationException)
            {
                return sourceSnapshot;
            }
            catch (ArgumentException)
            {
                return sourceSnapshot;
            }
        }

        public void InvalidateAndNotify()
        {
            _source.Dispatcher.VerifyAccess();

            _sourceSnapshots.Clear();
            _blurredSnapshots.Clear();

            for (int index = _panels.Count - 1; index >= 0; index--)
            {
                if (_panels[index].TryGetTarget(out AcrylicPanel? panel))
                {
                    panel.QueueBackdropRefresh();
                }
                else
                {
                    _panels.RemoveAt(index);
                }
            }
        }

        private void OnSourceSizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            InvalidateAndNotify();
        }

        private void OnResizeDebounceTick(
            object? sender,
            EventArgs e)
        {
            InvalidateAndNotify();
        }

        private BitmapSource? CaptureSource(SourceSnapshotKey key)
        {
            try
            {
                var bitmap = new RenderTargetBitmap(
                    key.PixelWidth,
                    key.PixelHeight,
                    key.DpiX,
                    key.DpiY,
                    PixelFormats.Pbgra32);

                bitmap.Render(_source);
                bitmap.Freeze();

                return bitmap;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static BitmapSource CreateBlurredSnapshot(
            BitmapSource source,
            SourceSnapshotKey key,
            double blurRadius)
        {
            var image = new System.Windows.Controls.Image
            {
                Source = source,
                Width = key.LogicalWidth,
                Height = key.LogicalHeight,
                Stretch = Stretch.Fill,
                Effect = new BlurEffect
                {
                    Radius = blurRadius,
                    KernelType = KernelType.Gaussian,
                    RenderingBias = RenderingBias.Performance
                }
            };

            RenderOptions.SetBitmapScalingMode(
                image,
                BitmapScalingMode.Linear);

            image.Measure(new Size(key.LogicalWidth, key.LogicalHeight));
            image.Arrange(new Rect(0, 0, key.LogicalWidth, key.LogicalHeight));
            image.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                key.PixelWidth,
                key.PixelHeight,
                key.DpiX,
                key.DpiY,
                PixelFormats.Pbgra32);

            bitmap.Render(image);
            bitmap.Freeze();

            return bitmap;
        }

        private void RemoveDeadPanels()
        {
            for (int index = _panels.Count - 1; index >= 0; index--)
            {
                if (!_panels[index].TryGetTarget(out _))
                {
                    _panels.RemoveAt(index);
                }
            }
        }

        private static double Quantize(double value) =>
            Math.Round(value * 4.0, MidpointRounding.AwayFromZero) / 4.0;
    }

    private readonly record struct SnapshotKey(
        SourceSnapshotKey Source,
        double BlurRadius);

    private readonly record struct SourceSnapshotKey(
        double LogicalWidth,
        double LogicalHeight,
        int PixelWidth,
        int PixelHeight,
        double DpiX,
        double DpiY)
    {
        private const double MaxSnapshotPixels = 8_000_000.0;
        private const double MaxSnapshotDimension = 8192.0;

        public static SourceSnapshotKey Create(
            double logicalWidth,
            double logicalHeight,
            DpiScale dpi,
            double scale)
        {
            double normalizedWidth = QuantizeDimension(logicalWidth);
            double normalizedHeight = QuantizeDimension(logicalHeight);

            double scaledDpiX = 96.0 * dpi.DpiScaleX * scale;
            double scaledDpiY = 96.0 * dpi.DpiScaleY * scale;

            double pixelWidth = normalizedWidth * scaledDpiX / 96.0;
            double pixelHeight = normalizedHeight * scaledDpiY / 96.0;

            double dimensionReduction = Math.Min(
                1.0,
                Math.Min(
                    MaxSnapshotDimension / Math.Max(1.0, pixelWidth),
                    MaxSnapshotDimension / Math.Max(1.0, pixelHeight)));

            double pixelCount = Math.Max(1.0, pixelWidth * pixelHeight);
            double areaReduction = Math.Min(
                1.0,
                Math.Sqrt(MaxSnapshotPixels / pixelCount));

            double reduction = Math.Min(dimensionReduction, areaReduction);

            scaledDpiX *= reduction;
            scaledDpiY *= reduction;

            int finalPixelWidth = Math.Max(
                1,
                (int)Math.Ceiling(normalizedWidth * scaledDpiX / 96.0));

            int finalPixelHeight = Math.Max(
                1,
                (int)Math.Ceiling(normalizedHeight * scaledDpiY / 96.0));

            return new SourceSnapshotKey(
                normalizedWidth,
                normalizedHeight,
                finalPixelWidth,
                finalPixelHeight,
                scaledDpiX,
                scaledDpiY);
        }

        private static double QuantizeDimension(double value) =>
            Math.Round(value * 2.0, MidpointRounding.AwayFromZero) / 2.0;
    }
}
