namespace SYSi.Helpers
{
    public class WidthToColumnsConverter : IValueConverter
    {
        public double MinCardWidth { get; set; } = 190;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double width || width <= 0)
                return 1;

            var columns = (int)Math.Floor(width / MinCardWidth);

            return Math.Clamp(columns, 1, 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
