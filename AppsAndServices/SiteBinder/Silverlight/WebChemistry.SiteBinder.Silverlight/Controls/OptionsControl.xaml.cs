using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WebChemistry.SiteBinder.Silverlight.Controls
{
    public partial class OptionsControl : UserControl
    {
        public OptionsControl()
        {
            InitializeComponent();

            int gotoCounter = 0;
            pivotField.TextFilter = (search, value) =>
            {
                if (value.Length > 0)
                {
                    if ((gotoCounter < 10) &&
                        (value.ToUpper().StartsWith(search.ToUpper())))
                    {
                        gotoCounter++;
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            };

            pivotField.TextChanged += (s, a) => gotoCounter = 0;
        }
    }
}
