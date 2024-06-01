using System;
using System.Windows;
using System.Windows.Data;

namespace WebChemistry.Silverlight.Common.Converters
{

    public class UIVisibilityConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool reverse = false;
            
            if (parameter is string) reverse = ((string)parameter).ToLower() == "negate";

            bool visibility;

            if (value is int) visibility = (int)value != 0;
            else if (value is bool) visibility = (bool)value;
            else if (value is Visibility) visibility = ((Visibility)value) == Visibility.Visible;
            else visibility = value != null;

            if (reverse)
                return visibility ? Visibility.Collapsed : Visibility.Visible;

            return visibility ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
