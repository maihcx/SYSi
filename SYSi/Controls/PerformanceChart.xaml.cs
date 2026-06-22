// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace SYSi.Controls;

public class PerformanceChart : Control
{
    private const string PartPlotArea = "PART_PlotArea";
    private const string PartGridCanvas = "PART_GridCanvas";
    private const string PartLine = "PART_Line";
    private const string PartFill = "PART_Fill";

    private Grid? _plotArea;
    private Canvas? _gridCanvas;
    private Polyline? _line;
    private Path? _fill;

    // ── Cached rendering objects ────────────────────────────────────────────
    private LinearGradientBrush? _cachedFillBrush;
    private Color _cachedAccentColor;

    private PointCollection? _pointsCache;
    private PolyLineSegment? _polySegCache;
    private PathFigure? _figCache;
    private PathGeometry? _geoCache;

    private Size _lastGridSize;
    private double _lastVerticalOffset;

    private double _accumulatedOffset;

    static PerformanceChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(typeof(PerformanceChart)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _plotArea   = GetTemplateChild(PartPlotArea)   as Grid;
        _gridCanvas = GetTemplateChild(PartGridCanvas) as Canvas;
        _line       = GetTemplateChild(PartLine)       as Polyline;
        _fill       = GetTemplateChild(PartFill)       as Path;

        SizeChanged += (_, _) => Redraw();
        Redraw();
    }

    #region Dependency Properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit),
            typeof(string), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty MaxCapacityLabelProperty =
        DependencyProperty.Register(nameof(MaxCapacityLabel),
            typeof(string), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string MaxCapacityLabel
    {
        get => (string)GetValue(MaxCapacityLabelProperty);
        set => SetValue(MaxCapacityLabelProperty, value);
    }

    public static readonly DependencyProperty CurrentValueProperty =
        DependencyProperty.Register(nameof(CurrentValue),
            typeof(double), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(0.0));

    public double CurrentValue
    {
        get => (double)GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    }

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values),
            typeof(IReadOnlyList<double>), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null, OnDataChanged));

    public IReadOnlyList<double> Values
    {
        get => (IReadOnlyList<double>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue),
            typeof(double), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(100.0, OnDataChanged));

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(nameof(LineBrush),
            typeof(Brush), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromRgb(0, 120, 212)), OnVisualChanged));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty LineThicknessProperty =
        DependencyProperty.Register(nameof(LineThickness),
            typeof(double), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(1.0, OnVisualChanged));

    public double LineThickness
    {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public static readonly DependencyProperty HorizontalGridLinesProperty =
        DependencyProperty.Register(nameof(HorizontalGridLines),
            typeof(int), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(9, OnVisualChanged));

    public int HorizontalGridLines
    {
        get => (int)GetValue(HorizontalGridLinesProperty);
        set => SetValue(HorizontalGridLinesProperty, value);
    }

    public static readonly DependencyProperty VerticalGridLinesProperty =
        DependencyProperty.Register(nameof(VerticalGridLines),
            typeof(int), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(5, OnVisualChanged));

    public int VerticalGridLines
    {
        get => (int)GetValue(VerticalGridLinesProperty);
        set => SetValue(VerticalGridLinesProperty, value);
    }

    public static readonly DependencyProperty GridLineBrushProperty =
        DependencyProperty.Register(nameof(GridLineBrush),
            typeof(Brush), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromRgb(204, 204, 204)), OnVisualChanged));

    public Brush GridLineBrush
    {
        get => (Brush)GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public static readonly DependencyProperty ShowGridLinesProperty =
        DependencyProperty.Register(nameof(ShowGridLines),
            typeof(bool), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(true, OnVisualChanged));

    public bool ShowGridLines
    {
        get => (bool)GetValue(ShowGridLinesProperty);
        set => SetValue(ShowGridLinesProperty, value);
    }

    public static readonly DependencyProperty FooterLabelProperty =
        DependencyProperty.Register(nameof(FooterLabel),
            typeof(string), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string FooterLabel
    {
        get => (string)GetValue(FooterLabelProperty);
        set => SetValue(FooterLabelProperty, value);
    }

    public static readonly DependencyProperty AxisLabelBrushProperty =
        DependencyProperty.Register(nameof(AxisLabelBrush),
            typeof(Brush), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public Brush AxisLabelBrush
    {
        get => (Brush)GetValue(AxisLabelBrushProperty);
        set => SetValue(AxisLabelBrushProperty, value);
    }

    public static readonly DependencyProperty BorderBrushExProperty =
        DependencyProperty.Register(nameof(BorderBrushEx),
            typeof(Brush), typeof(PerformanceChart),
            new PropertyMetadata(null));

    public Brush BorderBrushEx
    {
        get => (Brush)GetValue(BorderBrushExProperty);
        set => SetValue(BorderBrushExProperty, value);
    }

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(nameof(FillBrush),
            typeof(Brush), typeof(PerformanceChart),
            new PropertyMetadata(null));

    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public static readonly DependencyProperty CapacityProperty =
        DependencyProperty.Register(nameof(Capacity),
            typeof(int), typeof(PerformanceChart),
            new FrameworkPropertyMetadata(60));

    public int Capacity
    {
        get => (int)GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    #endregion

    // ── Callbacks ───────────────────────────────────────────────────────────

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PerformanceChart c)
        {
            return;
        }

        if (c._plotArea is not { } plotArea)
        {
            return;
        }

        if (c._line is not { } line)
        {
            return;
        }

        if (c._fill is not { } fill)
        {
            return;
        }

        if (c._gridCanvas is not { } gridCanvas)
        {
            return;
        }

        double w = plotArea.ActualWidth;
        double h = plotArea.ActualHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var values = c.Values;
        if (values == null || values.Count < 2)
        {
            return;
        }

        int capacity = Math.Max(values.Count, c.Capacity);
        int vLines = Math.Max(1, c.VerticalGridLines);
        double pixelPerTick = w / (capacity - 1);
        double period = w / (vLines + 1);

        c._accumulatedOffset = (c._accumulatedOffset + pixelPerTick) % period;

        c.DrawVerticalGridLines(gridCanvas, w, h, c._accumulatedOffset, vLines, period);
        c.DrawSeries(line, fill, values, w, h, capacity);
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PerformanceChart c)
        {
            c._cachedFillBrush = null;
            c._lastGridSize    = default;
            c._pointsCache     = null;
            c.Redraw();
        }
    }

    // ── Draw pipeline ────────────────────────────────────────────────────────

    private void Redraw()
    {
        if (_plotArea is not { } plotArea   ||
            _line is not { } line       ||
            _fill is not { } fill       ||
            _gridCanvas is not { } gridCanvas)
        {
            return;
        }

        double w = plotArea.ActualWidth;
        double h = plotArea.ActualHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        int vLines = Math.Max(1, VerticalGridLines);
        double period = w / (vLines + 1);

        DrawGrid(gridCanvas, w, h, vLines, period);

        var values = Values;
        if (values != null && values.Count >= 2)
        {
            int capacity = Math.Max(values.Count, Capacity);
            DrawSeries(line, fill, values, w, h, capacity);
        }
    }

    private void DrawGrid(Canvas gridCanvas, double w, double h, int vLines, double period)
    {
        if (!ShowGridLines)
        {
            gridCanvas.Children.Clear();
            _lastGridSize = default;
            return;
        }

        int hLines = Math.Max(1, HorizontalGridLines);
        var size = new Size(w, h);
        bool sizeChanged = _lastGridSize != size;

        if (sizeChanged)
        {
            _lastGridSize = size;
            gridCanvas.Children.Clear();

            for (int i = 1; i <= hLines; i++)
            {
                gridCanvas.Children.Add(new Line
                {
                    X1 = 0,
                    X2 = w,
                    Y1 = h * i / (hLines + 1),
                    Y2 = h * i / (hLines + 1),
                    Stroke          = GridLineBrush,
                    StrokeThickness = 1
                });
            }

            for (int i = 0; i <= vLines; i++)
            {
                gridCanvas.Children.Add(new Line
                {
                    Y1 = 0,
                    Y2 = h,
                    Stroke          = GridLineBrush,
                    StrokeThickness = 1
                });
            }
        }

        DrawVerticalGridLines(gridCanvas, w, h, _lastVerticalOffset, vLines, period, hLines);
    }

    private void DrawVerticalGridLines(
        Canvas gridCanvas, double w, double h,
        double offset, int vLines, double period,
        int hLines = -1)
    {
        if (!ShowGridLines)
        {
            return;
        }

        _lastVerticalOffset = offset;

        if (hLines < 0)
        {
            hLines = Math.Max(1, HorizontalGridLines);
        }

        double phase = period - (offset % period);
        if (phase <= 0)
        {
            phase += period;
        }

        int startIndex = hLines;

        for (int i = 0; i <= vLines; i++)
        {
            int childIndex = startIndex + i;
            if (childIndex >= gridCanvas.Children.Count)
            {
                break;
            }

            if (gridCanvas.Children[childIndex] is Line vLine)
            {
                double x = phase + i * period;
                vLine.X1 = x;
                vLine.X2 = x;
            }
        }
    }

    private void DrawSeries(
        Polyline line, Path fill,
        IReadOnlyList<double> values,
        double w, double h, int capacity)
    {
        int count = values.Count;
        double max = MaxValue <= 0 ? 100 : MaxValue;
        double step = w / (capacity - 1);
        double startX = w - (count - 1) * step;

        if (_pointsCache == null || _pointsCache.Count != count)
        {
            _pointsCache = new PointCollection(count);
            for (int i = 0; i < count; i++)
            {
                _pointsCache.Add(default);
            }

            _polySegCache = null;
        }

        for (int i = 0; i < count; i++)
        {
            _pointsCache[i] = new Point(
                startX + i * step,
                h - Math.Clamp(values[i] / max, 0, 1) * h);
        }

        line.Points = _pointsCache;

        if (_polySegCache == null || _figCache == null || _geoCache == null)
        {
            _polySegCache = new PolyLineSegment(_pointsCache, isStroked: true);
            _figCache     = new PathFigure
            {
                StartPoint = new Point(startX, h),
                IsClosed   = true,
                Segments   =
                {
                    new LineSegment(_pointsCache[0], isStroked: true),
                    _polySegCache,
                    new LineSegment(new Point(w, h), isStroked: true),
                }
            };
            _geoCache = new PathGeometry([_figCache]);
        }
        else
        {
            _figCache.StartPoint                              = new Point(startX, h);
            ((LineSegment)_figCache.Segments[0]).Point       = _pointsCache[0];
            _polySegCache.Points                             = _pointsCache;
            ((LineSegment)_figCache.Segments[2]).Point       = new Point(w, h);
        }

        fill.Data = _geoCache;

        if (FillBrush != null)
        {
            fill.Fill        = FillBrush;
            _cachedFillBrush = null;
        }
        else
        {
            var accent = (LineBrush as SolidColorBrush)?.Color ?? Colors.DodgerBlue;

            if (_cachedFillBrush == null || _cachedAccentColor != accent)
            {
                _cachedAccentColor = accent;
                _cachedFillBrush   = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint   = new Point(0, 1),
                    GradientStops =
                    [
                        new GradientStop(Color.FromArgb(160, accent.R, accent.G, accent.B), 0),
                        new GradientStop(Color.FromArgb( 20, accent.R, accent.G, accent.B), 1),
                    ]
                };
            }

            fill.Fill = _cachedFillBrush;
        }
    }
}