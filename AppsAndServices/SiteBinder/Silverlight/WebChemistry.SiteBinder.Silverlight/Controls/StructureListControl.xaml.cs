using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.SiteBinder.Silverlight.Controls
{
    public partial class StructureListControl : UserControl
    {
        public StructureListControl()
        {
            InitializeComponent();

            int gotoCounter = 0;
            gotoField.TextFilter = (search, value) =>
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

            gotoField.TextChanged += (s, a) =>
                {
                    gotoCounter = 0;
                };

            gotoField.SelectionChanged += (s, a) =>
                {
                    try
                    {
                        var view = dataView.ItemsSource as PagedCollectionView;
                        if (view != null && view.Count > 0)
                        {
                            dataView.ScrollIntoView(view[view.Count - 1], dataView.Columns[1]);
                            dataView.ScrollIntoView(gotoField.SelectedItem, dataView.Columns[1]);
                        }
                    }
                    catch
                    {
                    }
                };
        }

        private void ToggleGroupsClick(object sender, RoutedEventArgs e)
        {
            DataGridUtils.ToggleGroups(dataView);
        }
    }
}
