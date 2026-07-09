using System.Windows.Input;

namespace SYSi.Controls;

public class AcrylicPanel : ContentControl
{
    static AcrylicPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AcrylicPanel),
            new FrameworkPropertyMetadata(typeof(AcrylicPanel)));
    }

    public static readonly DependencyProperty BackgroundSourceProperty =
        DependencyProperty.Register(nameof(BackgroundSource), typeof(Visual), typeof(AcrylicPanel),
            new PropertyMetadata(null, OnBackgroundSourceChanged));

    public Visual? BackgroundSource
    {
        get => (Visual?)GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    public static readonly DependencyProperty BlurRadiusProperty =
        DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(AcrylicPanel),
            new PropertyMetadata(24.0));

    public double BlurRadius
    {
        get => (double)GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public static readonly DependencyProperty TintBrushProperty =
        DependencyProperty.Register(nameof(TintBrush), typeof(Brush), typeof(AcrylicPanel),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255))));

    public Brush TintBrush
    {
        get => (Brush)GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }

    private static readonly DependencyPropertyKey BlurBrushPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(BlurBrush), typeof(Brush), typeof(AcrylicPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BlurBrushProperty = BlurBrushPropertyKey.DependencyProperty;

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(AcrylicPanel),
            new PropertyMetadata(new CornerRadius(0)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

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

    private static readonly DependencyPropertyKey IsPressedPropertyKey =
    DependencyProperty.RegisterReadOnly(
        nameof(IsPressed),
        typeof(bool),
        typeof(AcrylicPanel),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsPressedProperty =
        IsPressedPropertyKey.DependencyProperty;

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
    }

    public Brush? BlurBrush => (Brush?)GetValue(BlurBrushProperty);

    private VisualBrush? _visualBrush;

    private Grid? _rootGrid;

    private RectangleGeometry? _clipGeometry;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootGrid = GetTemplateChild("PART_RootGrid") as Grid;

        if (_rootGrid != null)
        {
            _clipGeometry = new RectangleGeometry();
            _rootGrid.Clip = _clipGeometry;
        }

        Loaded += (_, _) => UpdateVisualBrush();
        SizeChanged += (_, _) =>
        {
            UpdateClip();
            UpdateViewbox();
        };

        LayoutUpdated += OnLayoutUpdated;
        Unloaded += (_, _) => LayoutUpdated -= OnLayoutUpdated;

        UpdateClip();
    }

    private void UpdateClip()
    {
        if (_clipGeometry == null)
        {
            return;
        }

        _clipGeometry.Rect = new Rect(0, 0, ActualWidth, ActualHeight);

        double radius = CornerRadius.TopLeft;

        _clipGeometry.RadiusX = radius;
        _clipGeometry.RadiusY = radius;
    }

    private static void OnBackgroundSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AcrylicPanel)d).UpdateVisualBrush();

    private void UpdateVisualBrush()
    {
        if (BackgroundSource is null)
        {
            SetValue(BlurBrushPropertyKey, null);
            _visualBrush = null;
            return;
        }

        _visualBrush = new VisualBrush(BackgroundSource)
        {
            Stretch = Stretch.None,
            ViewboxUnits = BrushMappingMode.Absolute,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };

        SetValue(BlurBrushPropertyKey, _visualBrush);
        UpdateViewbox();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateViewbox();

    private void UpdateViewbox()
    {
        if (_visualBrush is null || BackgroundSource is null || !IsLoaded)
        {
            return;
        }

        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        GeneralTransform transform;
        try
        {
            transform = TransformToVisual(BackgroundSource);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Point topLeft = transform.Transform(new Point(0, 0));
        _visualBrush.Viewbox = new Rect(topLeft, new Size(ActualWidth, ActualHeight));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
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

        e.Handled = true;
    }

    private bool IsMouseInside(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        return position.X >= 0 &&
               position.Y >= 0 &&
               position.X <= ActualWidth &&
               position.Y <= ActualHeight;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        SetValue(IsPressedPropertyKey, true);

        CaptureMouse();

        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        SetValue(IsPressedPropertyKey, false);
    }
}
