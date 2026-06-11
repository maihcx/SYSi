namespace SYSi.Helpers
{
    public class WidthToColumnsConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values.Length < 3)
            {
                return 1;
            }

            if (values[0] is not double width || width <= 0)
            {
                return 1;
            }

            double minCardWidth = double.Parse(values[1].ToString()!, CultureInfo.InvariantCulture);
            int maxColumns = int.Parse(values[2].ToString()!);

            var columns = (int)Math.Floor(width / minCardWidth);

            return Math.Clamp(columns, 1, maxColumns);
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
