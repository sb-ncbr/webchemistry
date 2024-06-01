using System.Windows.Data;
using System;

namespace WebChemistry.SiteBinder.Silverlight.Converters
{
    public class SigmaGroupConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string g = value.ToString();

          //  int nS = ((CollectionViewGroup)parameter).ItemCount;

          //  string cS = nS == 1 ? "1 motive" : nS.ToString() + " motives";

            string cS = "";

            if (g == "0") return "Difference from RMSD < σ " + cS;
            else if (g == "1") return "Difference from RMSD < 2σ " + cS;
            else if (g == "2") return "Difference from RMSD < 3σ " + cS;
            else return "Difference from RMSD > 3σ " + cS;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class MatchedCountConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString() + " atoms";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
