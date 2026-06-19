namespace SYSi.Controls
{
    public class PerformanceChart : Control
    {
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register(nameof(Values), typeof(IReadOnlyList<double>),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register(nameof(CurrentValue), typeof(double),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register(nameof(LineColor), typeof(Color),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(Color.FromRgb(0, 120, 212),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(nameof(LineThickness), typeof(double),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HorizontalGridLinesProperty =
            DependencyProperty.Register(nameof(HorizontalGridLines), typeof(int),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(9, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty VerticalGridLinesProperty =
            DependencyProperty.Register(nameof(VerticalGridLines), typeof(int),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(5, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridLineColorProperty =
            DependencyProperty.Register(nameof(GridLineColor), typeof(Color),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(
                    Color.FromRgb(204, 204, 204),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register(nameof(ShowGridLines), typeof(bool),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowAxisLabelsProperty =
            DependencyProperty.Register(nameof(ShowAxisLabels), typeof(bool),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata("%", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxCapacityLabelProperty =
            DependencyProperty.Register(nameof(MaxCapacityLabel), typeof(string),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata("100%", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TimeWindowSecondsProperty =
            DependencyProperty.Register(nameof(TimeWindowSeconds), typeof(int),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(60, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ChartBackgroundProperty =
            DependencyProperty.Register(nameof(ChartBackground), typeof(Brush),
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(
                    new SolidColorBrush(Colors.White),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IReadOnlyList<double> Values
        {
            get => (IReadOnlyList<double>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }
        public double CurrentValue
        {
            get => (double)GetValue(CurrentValueProperty);
            set => SetValue(CurrentValueProperty, value);
        }
        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }
        public Color LineColor
        {
            get => (Color)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }
        public double LineThickness
        {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }
        public int HorizontalGridLines
        {
            get => (int)GetValue(HorizontalGridLinesProperty);
            set => SetValue(HorizontalGridLinesProperty, value);
        }
        public int VerticalGridLines
        {
            get => (int)GetValue(VerticalGridLinesProperty);
            set => SetValue(VerticalGridLinesProperty, value);
        }
        public Color GridLineColor
        {
            get => (Color)GetValue(GridLineColorProperty);
            set => SetValue(GridLineColorProperty, value);
        }
        public bool ShowGridLines
        {
            get => (bool)GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }
        public bool ShowAxisLabels
        {
            get => (bool)GetValue(ShowAxisLabelsProperty);
            set => SetValue(ShowAxisLabelsProperty, value);
        }
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }
        public string MaxCapacityLabel
        {
            get => (string)GetValue(MaxCapacityLabelProperty);
            set => SetValue(MaxCapacityLabelProperty, value);
        }
        public int TimeWindowSeconds
        {
            get => (int)GetValue(TimeWindowSecondsProperty);
            set => SetValue(TimeWindowSecondsProperty, value);
        }
        public Brush ChartBackground
        {
            get => (Brush)GetValue(ChartBackgroundProperty);
            set => SetValue(ChartBackgroundProperty, value);
        }

        static PerformanceChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PerformanceChart),
                new FrameworkPropertyMetadata(typeof(PerformanceChart)));
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 10 || h < 10)
            {
                return;
            }

            double leftPad = ShowAxisLabels ? 0 : 0;
            double rightPad = 0;
            double topPad = ShowAxisLabels ? 16 : 0;
            double bottomPad = ShowAxisLabels ? 14 : 0;

            double cX = leftPad;
            double cY = topPad;
            double cW = w - leftPad - rightPad;
            double cH = h - topPad - bottomPad;
            var chartRect = new Rect(cX, cY, cW, cH);

            if (ShowAxisLabels)
            {
                DrawAxisLabels(dc, chartRect, w, h);
            }

            dc.PushClip(new RectangleGeometry(chartRect));

            DrawChartBackground(dc, chartRect);
            if (ShowGridLines)
            {
                DrawGrid(dc, chartRect);
            }

            DrawSeries(dc, chartRect);

            dc.Pop();

            DrawChartBorder(dc, chartRect);
        }

        private void DrawChartBackground(DrawingContext dc, Rect rect)
        {
            dc.DrawRectangle(ChartBackground ?? Brushes.White, null, rect);
        }

        private void DrawChartBorder(DrawingContext dc, Rect rect)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(180, 180, 180)), 1.0);
            pen.Freeze();
            dc.DrawRectangle(null, pen, rect);
        }

        private void DrawGrid(DrawingContext dc, Rect rect)
        {
            var gridBrush = new SolidColorBrush(GridLineColor);
            gridBrush.Freeze();
            var pen = new Pen(gridBrush, 1.0);
            pen.Freeze();

            int hLines = Math.Max(HorizontalGridLines, 1);
            for (int i = 1; i <= hLines; i++)
            {
                double y = Math.Round(rect.Y + rect.Height * i / (hLines + 1)) + 0.5;
                dc.DrawLine(pen, new Point(rect.X, y), new Point(rect.Right, y));
            }

            int vLines = Math.Max(VerticalGridLines, 1);
            for (int i = 1; i <= vLines; i++)
            {
                double x = Math.Round(rect.X + rect.Width * i / (vLines + 1)) + 0.5;
                dc.DrawLine(pen, new Point(x, rect.Y), new Point(x, rect.Bottom));
            }
        }

        private void DrawSeries(DrawingContext dc, Rect rect)
        {
            var values = Values;
            if (values == null || values.Count < 2)
            {
                return;
            }

            double maxVal = MaxValue > 0 ? MaxValue : 100;
            int count = values.Count;
            double xStep = rect.Width / (count - 1);

            var lineFigure = new PathFigure();
            var fillFigure = new PathFigure { StartPoint = new Point(rect.X, rect.Bottom) };

            for (int i = 0; i < count; i++)
            {
                double xPos = rect.X + i * xStep;
                double norm = Math.Clamp(values[i] / maxVal, 0.0, 1.0);
                double yPos = rect.Bottom - norm * rect.Height;
                var pt = new Point(xPos, yPos);

                if (i == 0)
                {
                    lineFigure.StartPoint = pt;
                    fillFigure.Segments.Add(new LineSegment(pt, false));
                }
                else
                {
                    lineFigure.Segments.Add(new LineSegment(pt, true));
                    fillFigure.Segments.Add(new LineSegment(pt, true));
                }
            }

            fillFigure.Segments.Add(new LineSegment(new Point(rect.Right, rect.Bottom), false));
            fillFigure.IsClosed = true;

            var accent = LineColor;
            var fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint   = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(160, accent.R, accent.G, accent.B), 0.0),
                    new GradientStop(Color.FromArgb(60,  accent.R, accent.G, accent.B), 0.5),
                    new GradientStop(Color.FromArgb(10,  accent.R, accent.G, accent.B), 1.0),
                }
            };
            fill.Freeze();

            var fillGeom = new PathGeometry(new[] { fillFigure });
            fillGeom.Freeze();
            dc.DrawGeometry(fill, null, fillGeom);

            var linePen = new Pen(new SolidColorBrush(accent), LineThickness)
            {
                LineJoin     = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round,
            };
            linePen.Freeze();

            var lineGeom = new PathGeometry(new[] { lineFigure });
            lineGeom.Freeze();
            dc.DrawGeometry(null, linePen, lineGeom);
        }

        private void DrawAxisLabels(DrawingContext dc, Rect chartRect, double totalW, double totalH)
        {
            double maxVal = MaxValue > 0 ? MaxValue : 100;
            string maxLbl = MaxCapacityLabel ?? $"{(int)maxVal}{Unit}";

            var brush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
            brush.Freeze();

            var accentBrush = new SolidColorBrush(LineColor);
            accentBrush.Freeze();

            double fs = 10.0;

            string topLeft = string.IsNullOrEmpty(Title)
                ? $"{CurrentValue:0.#}{Unit}"
                : Title;
            DrawText(dc, topLeft, brush, fs, chartRect.X + 2, chartRect.Y - 15, false);

            var ftMax = MakeFormattedText(maxLbl, brush, fs, false);
            dc.DrawText(ftMax, new Point(chartRect.Right - ftMax.Width - 2, chartRect.Y - 15));

            DrawText(dc, $"0{Unit}", brush, fs,
                chartRect.X + 2, chartRect.Bottom + 1, false);

            var accentFt = MakeFormattedText(
                $"{CurrentValue:0.#}{Unit}", accentBrush, 11.5, true);
            dc.DrawText(accentFt, new Point(chartRect.X + 4, chartRect.Y + 3));
        }

        private void DrawText(DrawingContext dc, string text, Brush brush,
            double size, double x, double y, bool bold)
        {
            dc.DrawText(MakeFormattedText(text, brush, size, bold), new Point(x, y));
        }

        private FormattedText MakeFormattedText(string text, Brush brush, double size, bool bold)
        {
            return new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    bold ? FontWeights.SemiBold : FontWeights.Normal,
                    FontStretches.Normal),
                size,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }
    }
}