using Microsoft.Practices.ServiceLocation;
using System.Windows.Controls;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;

namespace WebChemistry.Charges.Silverlight.Views
{
    public partial class InputView : UserControl
    {
        public InputView()
        {
            InitializeComponent();
        }

        private void activeSetsDragArea_DragEnter_1(object sender, Microsoft.Windows.DragEventArgs e)
        {
            var data = e.Data.GetData(typeof(ItemDragEventArgs)) as ItemDragEventArgs;
            if (data != null)
            {
                if (data.DragSource is DataGrid)
                {
                    ActiveSetsDragBorder.BorderThickness = new System.Windows.Thickness(2);
                }
            }
        }

        private void activeSetsDragArea_Drop_1(object sender, Microsoft.Windows.DragEventArgs e)
        {
            //DragEventArgs e;
            var data = e.Data.GetData(typeof(ItemDragEventArgs)) as ItemDragEventArgs;
            if (data != null)
            {
                if (data.DragSource is DataGrid)
                {
                    var selection = data.Data as System.Collections.ObjectModel.SelectionCollection;

                    var session = ServiceLocator.Current.GetInstance<Session>();
                    if (selection.Count == 1)
                    {
                        var set = selection[0].Item as EemParameterSet;
                        if (set != null)
                        {
                            if (set.IsSelected) session.AddActiveSets();
                            else session.AddActiveSet(set);
                        }
                    }
                    else
                    {

                        foreach (System.Collections.ObjectModel.Selection sel in selection)
                        {
                            var set = sel.Item as EemParameterSet;
                            if (set != null) session.AddActiveSet(set);
                        }
                    }

                    e.Handled = true;
                }                
            }

            ActiveSetsDragBorder.BorderThickness = new System.Windows.Thickness(0);
        }

        private void activeSetsDragArea_DragLeave_1(object sender, Microsoft.Windows.DragEventArgs e)
        {
            ActiveSetsDragBorder.BorderThickness = new System.Windows.Thickness(0);
        }

        private void activeSetsDragArea_ItemDragStarting_1(object sender, ItemDragEventArgs e)
        {
            e.Cancel = true;
            e.Handled = true;
        }
    }
}