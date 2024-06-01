using System.Windows;
using System.Windows.Controls;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class ParameterSetsControl : UserControl
    {
        public ParameterSetsControl()
        {
            InitializeComponent();
        }

        private void ToggleGroupsClick(object sender, RoutedEventArgs e)
        {
            DataGridUtils.ToggleGroups(setsGrid);
        }

        private void DataGridDragDropTarget_ItemDroppedOnTarget_1(object sender, ItemDragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
