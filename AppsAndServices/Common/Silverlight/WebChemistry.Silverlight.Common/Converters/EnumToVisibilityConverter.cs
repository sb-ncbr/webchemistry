using System;
using System.Windows;
using System.Windows.Data;

namespace WebChemistry.Silverlight.Common.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        #region Methods
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            return ((Visibility)value) == Visibility.Visible ? Enum.Parse(targetType, (String)parameter, true) : null;
        }
        #endregion Methods
    }
}