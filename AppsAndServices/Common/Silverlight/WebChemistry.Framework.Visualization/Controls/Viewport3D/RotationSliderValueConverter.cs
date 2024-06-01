using System.Windows.Data;

namespace WebChemistry.Framework.Controls
{
    /// <summary>
    /// Ensures the value stays between -180 and 180 degrees.
    /// </summary>
    public class RotationSliderValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double a = (double)value;

            if (a < -180) while (a < -180) a += 360;
            if (a > 180) while (a > 180) a -= 360;

            return a;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}