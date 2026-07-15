namespace SYSi.Helpers
{
    public sealed class BrushAlphaColorConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is not SolidColorBrush brush)
            {
                return Colors.Transparent;
            }

            double opacity = 1.0;

            if (parameter is string text &&
                double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed))
            {
                opacity = Math.Clamp(parsed, 0.0, 1.0);
            }

            Color source = brush.Color;

            double sourceAlpha =
                source.A / 255.0 *
                brush.Opacity;

            byte alpha = (byte)Math.Round(
                255.0 *
                sourceAlpha *
                opacity);

            return Color.FromArgb(
                alpha,
                source.R,
                source.G,
                source.B);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
