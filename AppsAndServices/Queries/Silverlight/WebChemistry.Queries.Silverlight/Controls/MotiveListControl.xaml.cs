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
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Queries.Silverlight.Controls
{
    public partial class MotiveListControl : UserControl
    {
        public MotiveListControl()
        {
            InitializeComponent();
        }

        private void ToggleGroupsClick(object sender, RoutedEventArgs e)
        {
            DataGridUtils.ToggleGroups(dataView);
        }
    }
}
