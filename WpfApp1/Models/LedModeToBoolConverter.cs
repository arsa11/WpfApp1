using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Models
{
    // true  -> Hold
    // false -> Toggle
    public class LedModeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LedMode mode)
            {
                return mode == LedMode.Hold;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isOn = value is bool b && b;
            return isOn ? LedMode.Hold : LedMode.Toggle;
        }
    }
}
