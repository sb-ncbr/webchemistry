using System;
using System.Windows.Data;

namespace WebChemistry.Silverlight.Common.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        #region Methods
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null)
                return value;

            return (bool)value ? Enum.Parse(targetType, (String)parameter, true) : null;
        }
        #endregion Methods
    }
}