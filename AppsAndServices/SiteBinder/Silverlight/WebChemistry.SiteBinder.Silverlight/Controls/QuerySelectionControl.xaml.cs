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
    public partial class QuerySelectionControl : UserControl
    {
        public QuerySelectionControl()
        {
            InitializeComponent();
        }

        private void addButton_Click_1(object sender, RoutedEventArgs e)
        {
            queryInput.Focus();
        }
    }
}
