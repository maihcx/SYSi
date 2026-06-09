namespace SYSi.Helpers
{
    public class BoolToBadgeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = value is true;
            string param = parameter?.ToString() ?? "background";

            return param switch
            {
                "foreground" => Application.Current.Resources[
                    isTrue ? "SystemFillColorSuccessBrush"
                           : "SystemFillColorCautionBrush"],

                _ => Application.Current.Resources[
                    isTrue ? "SystemFillColorSuccessBackgroundBrush"
                           : "SystemFillColorCautionBackgroundBrush"],
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
