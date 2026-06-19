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
    private System.Windows.Shapes.Path? _fill;

    static PerformanceChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(typeof(PerformanceChart)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _plotArea = GetTemplateChild(PartPlotArea) as Grid;
        _gridCanvas = GetTemplateChild(PartGridCanvas) as Canvas;
        _line = GetTemplateChild(PartLine) as Polyline;
        _fill = GetTemplateChild(PartFill) as System.Windows.Shapes.Path;

        SizeChanged += (_, _) => Redraw();

        Redraw();
    }

    #region Dependency Properties
    public static readonly DependencyProperty TitleProperty =
    DependencyProperty.Register(nameof(Title),
        typeof(string),
        typeof(PerformanceChart),
        new FrameworkPropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit),
            typeof(string),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty MaxCapacityLabelProperty =
        DependencyProperty.Register(nameof(MaxCapacityLabel),
            typeof(string),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string MaxCapacityLabel
    {
        get => (string)GetValue(MaxCapacityLabelProperty);
        set => SetValue(MaxCapacityLabelProperty, value);
    }

    public static readonly DependencyProperty CurrentValueProperty =
        DependencyProperty.Register(nameof(CurrentValue),
            typeof(double),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(0.0));

    public double CurrentValue
    {
        get => (double)GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    }

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values),
            typeof(IReadOnlyList<double>),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null, OnVisualChanged));

    public IReadOnlyList<double> Values
    {
        get => (IReadOnlyList<double>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue),
            typeof(double),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(100.0, OnVisualChanged));

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(nameof(LineBrush),
            typeof(Brush),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromRgb(0, 120, 212)), OnVisualChanged));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty LineThicknessProperty =
        DependencyProperty.Register(nameof(LineThickness),
            typeof(double),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(1.0, OnVisualChanged));

    public double LineThickness
    {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public static readonly DependencyProperty HorizontalGridLinesProperty =
        DependencyProperty.Register(nameof(HorizontalGridLines),
            typeof(int),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(9, OnVisualChanged));

    public int HorizontalGridLines
    {
        get => (int)GetValue(HorizontalGridLinesProperty);
        set => SetValue(HorizontalGridLinesProperty, value);
    }

    public static readonly DependencyProperty VerticalGridLinesProperty =
        DependencyProperty.Register(nameof(VerticalGridLines),
            typeof(int),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(5, OnVisualChanged));

    public int VerticalGridLines
    {
        get => (int)GetValue(VerticalGridLinesProperty);
        set => SetValue(VerticalGridLinesProperty, value);
    }

    public static readonly DependencyProperty GridLineBrushProperty =
        DependencyProperty.Register(nameof(GridLineBrush),
            typeof(Brush),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromRgb(204, 204, 204)), OnVisualChanged));

    public Brush GridLineBrush
    {
        get => (Brush)GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public static readonly DependencyProperty ShowGridLinesProperty =
        DependencyProperty.Register(nameof(ShowGridLines),
            typeof(bool),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(true, OnVisualChanged));

    public bool ShowGridLines
    {
        get => (bool)GetValue(ShowGridLinesProperty);
        set => SetValue(ShowGridLinesProperty, value);
    }

    public static readonly DependencyProperty FooterLabelProperty =
        DependencyProperty.Register(
            nameof(FooterLabel),
            typeof(string),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public string FooterLabel
    {
        get => (string)GetValue(FooterLabelProperty);
        set => SetValue(FooterLabelProperty, value);
    }

    public static readonly DependencyProperty AxisLabelBrushProperty =
        DependencyProperty.Register(
            nameof(AxisLabelBrush),
            typeof(Brush),
            typeof(PerformanceChart),
            new FrameworkPropertyMetadata(null));

    public Brush AxisLabelBrush
    {
        get => (Brush)GetValue(AxisLabelBrushProperty);
        set => SetValue(AxisLabelBrushProperty, value);
    }

    public static readonly DependencyProperty BorderBrushExProperty =
        DependencyProperty.Register(nameof(BorderBrushEx), typeof(Brush),
            typeof(PerformanceChart), new PropertyMetadata(null));

    public Brush BorderBrushEx
    {
        get => (Brush)GetValue(BorderBrushExProperty);
        set => SetValue(BorderBrushExProperty, value);
    }

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(nameof(FillBrush), typeof(Brush),
            typeof(PerformanceChart), new PropertyMetadata(null));

    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }
    #endregion

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PerformanceChart c)
        {
            c.Redraw();
        }
    }

    private void Redraw()
    {
        if (_plotArea == null || _line == null || _fill == null || _gridCanvas == null)
        {
            return;
        }

        double w = _plotArea.ActualWidth;
        double h = _plotArea.ActualHeight;

        if (w <= 0 || h <= 0)
        {
            return;
        }

        DrawGrid(w, h);
        DrawSeries(w, h);
    }

    private void DrawGrid(double w, double h)
    {
        _gridCanvas!.Children.Clear();

        if (!ShowGridLines)
        {
            return;
        }

        for (int i = 1; i <= Math.Max(1, HorizontalGridLines); i++)
        {
            double y = h * i / (HorizontalGridLines + 1);

            _gridCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = w,
                Y1 = y,
                Y2 = y,
                Stroke = GridLineBrush,
                StrokeThickness = 1
            });
        }

        for (int i = 1; i <= Math.Max(1, VerticalGridLines); i++)
        {
            double x = w * i / (VerticalGridLines + 1);

            _gridCanvas.Children.Add(new Line
            {
                Y1 = 0,
                Y2 = h,
                X1 = x,
                X2 = x,
                Stroke = GridLineBrush,
                StrokeThickness = 1
            });
        }
    }

    private void DrawSeries(double w, double h)
    {
        var values = Values;
        if (values == null || values.Count < 2)
        {
            return;
        }

        var points = new PointCollection(values.Count);

        double step = w / (values.Count - 1);
        double max = MaxValue <= 0 ? 100 : MaxValue;

        for (int i = 0; i < values.Count; i++)
        {
            double x = i * step;
            double y = h - Math.Clamp(values[i] / max, 0, 1) * h;
            points.Add(new Point(x, y));
        }

        _line!.Points = points;

        var fig = new PathFigure { StartPoint = new Point(0, h), IsClosed = true };
        fig.Segments.Add(new LineSegment(points[0], true));

        for (int i = 1; i < points.Count; i++)
        {
            fig.Segments.Add(new LineSegment(points[i], true));
        }

        fig.Segments.Add(new LineSegment(new Point(w, h), true));

        _fill?.Data = new PathGeometry(new[] { fig });

        var accent = (LineBrush as SolidColorBrush)?.Color ?? Colors.DodgerBlue;

        if (FillBrush != null)
        {
            _fill?.Fill = FillBrush;
        }
        else
        {
            _fill?.Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(160, accent.R, accent.G, accent.B), 0),
                    new GradientStop(Color.FromArgb(20, accent.R, accent.G, accent.B), 1),
                }
            };
        }
    }
}
