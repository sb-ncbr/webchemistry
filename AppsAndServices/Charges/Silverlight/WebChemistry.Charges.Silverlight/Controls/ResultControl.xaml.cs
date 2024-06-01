using System.Windows;
using System.Windows.Controls;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class ResultControl : UserControl
    {
        public ResultControl()
        {
            InitializeComponent();
        }


        private void ToggleGroupsClick(object sender, RoutedEventArgs e)
        {
            DataGridUtils.ToggleGroups(setsGrid);            
        }

    }
}
