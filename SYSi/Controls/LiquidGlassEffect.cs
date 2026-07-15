using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SYSi.Controls;

/// <summary>
/// GPU pixel-shader effect that bends a backdrop around the perimeter of a
/// rounded rectangle and samples the red, green and blue channels at slightly
/// different offsets.
/// </summary>
public sealed class LiquidGlassEffect : ShaderEffect
{
    private static readonly PixelShader SharedPixelShader = CreatePixelShader();

    public LiquidGlassEffect()
    {
        PixelShader = SharedPixelShader;

        // The shader only samples toward the center, so large padding is not
        // required. A small amount prevents edge clipping during transforms.
        PaddingLeft = 2;
        PaddingTop = 2;
        PaddingRight = 2;
        PaddingBottom = 2;

        UpdateShaderValue(InputProperty);
        UpdateShaderValue(InputSizeProperty);
        UpdateShaderValue(CornerRadiusProperty);
        UpdateShaderValue(RefractionDepthProperty);
        UpdateShaderValue(RefractionStrengthProperty);
        UpdateShaderValue(ChromaticAberrationProperty);
        UpdateShaderValue(SaturationProperty);
        UpdateShaderValue(BrightnessProperty);
        UpdateShaderValue(EdgeHighlightProperty);
        UpdateShaderValue(LightDirectionProperty);
    }

    private static PixelShader CreatePixelShader()
    {
        string assemblyName =
            typeof(LiquidGlassEffect).Assembly.GetName().Name ?? "SYSi";

        var shader = new PixelShader
        {
            UriSource = new Uri(
                $"pack://application:,,,/{assemblyName};component/Controls/Shaders/LiquidGlass.ps",
                UriKind.Absolute)
        };

        if (shader.CanFreeze)
        {
            shader.Freeze();
        }

        return shader;
    }

    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty(
            nameof(Input),
            typeof(LiquidGlassEffect),
            0);

    public Brush? Input
    {
        get => (Brush?)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public static readonly DependencyProperty InputSizeProperty =
        DependencyProperty.Register(
            nameof(InputSize),
            typeof(Point),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                new Point(1, 1),
                PixelShaderConstantCallback(0)));

    public Point InputSize
    {
        get => (Point)GetValue(InputSizeProperty);
        set => SetValue(InputSizeProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                0.0,
                PixelShaderConstantCallback(1)));

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty RefractionDepthProperty =
        DependencyProperty.Register(
            nameof(RefractionDepth),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                10.0,
                PixelShaderConstantCallback(2)));

    public double RefractionDepth
    {
        get => (double)GetValue(RefractionDepthProperty);
        set => SetValue(RefractionDepthProperty, value);
    }

    public static readonly DependencyProperty RefractionStrengthProperty =
        DependencyProperty.Register(
            nameof(RefractionStrength),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                100.0,
                PixelShaderConstantCallback(3)));

    public double RefractionStrength
    {
        get => (double)GetValue(RefractionStrengthProperty);
        set => SetValue(RefractionStrengthProperty, value);
    }

    public static readonly DependencyProperty ChromaticAberrationProperty =
        DependencyProperty.Register(
            nameof(ChromaticAberration),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                2.0,
                PixelShaderConstantCallback(4)));

    public double ChromaticAberration
    {
        get => (double)GetValue(ChromaticAberrationProperty);
        set => SetValue(ChromaticAberrationProperty, value);
    }

    public static readonly DependencyProperty SaturationProperty =
        DependencyProperty.Register(
            nameof(Saturation),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                1.5,
                PixelShaderConstantCallback(5)));

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public static readonly DependencyProperty BrightnessProperty =
        DependencyProperty.Register(
            nameof(Brightness),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                1.1,
                PixelShaderConstantCallback(6)));

    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    public static readonly DependencyProperty EdgeHighlightProperty =
        DependencyProperty.Register(
            nameof(EdgeHighlight),
            typeof(double),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                0.18,
                PixelShaderConstantCallback(7)));

    public double EdgeHighlight
    {
        get => (double)GetValue(EdgeHighlightProperty);
        set => SetValue(EdgeHighlightProperty, value);
    }

    public static readonly DependencyProperty LightDirectionProperty =
        DependencyProperty.Register(
            nameof(LightDirection),
            typeof(Point),
            typeof(LiquidGlassEffect),
            new UIPropertyMetadata(
                new Point(-0.72, -0.69),
                PixelShaderConstantCallback(8)));

    public Point LightDirection
    {
        get => (Point)GetValue(LightDirectionProperty);
        set => SetValue(LightDirectionProperty, value);
    }
}
