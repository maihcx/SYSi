using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Helpers
{
    public class MvToVoltConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ushort mv && mv > 0 ? (mv / 1000.0).ToString("F2", CultureInfo.InvariantCulture) : "N/A";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
