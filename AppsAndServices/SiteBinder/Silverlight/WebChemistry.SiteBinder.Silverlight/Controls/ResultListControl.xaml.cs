using System.Windows;
using System.Windows.Controls;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.SiteBinder.Silverlight.Controls
{
    public partial class ResultListControl : UserControl
    {
        public ResultListControl()
        {
            InitializeComponent();
        }

        private void ToggleGroupsClick(object sender, RoutedEventArgs e)
        {
            DataGridUtils.ToggleGroups(dataView);
        }
    }
}
