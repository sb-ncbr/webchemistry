namespace WebChemistry.Silverlight.Common.Utils
{
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;

    public static class DataGridUtils
    {
        static bool IsAnyGroupExpanded(DataGrid grid)
        {
            foreach (var control in grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>())
            {
                var tog = control.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault();

                if (tog != null)
                {
                    var context = control.DataContext as CollectionViewGroup;
                    string nm = context.Name.ToString();
                    bool? chk = tog.IsChecked;
                    if (chk != null && (bool)chk)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void ToggleGroups(DataGrid grid)
        {
            try
            {
                var view = grid.ItemsSource as PagedCollectionView;
                if (view != null)
                {
                    if (IsAnyGroupExpanded(grid)) foreach (CollectionViewGroup g in view.Groups) grid.CollapseRowGroup(g, true);
                    else foreach (CollectionViewGroup g in view.Groups) grid.ExpandRowGroup(g, true);
                }
            }
            catch
            {
            }
        }
    }
}
