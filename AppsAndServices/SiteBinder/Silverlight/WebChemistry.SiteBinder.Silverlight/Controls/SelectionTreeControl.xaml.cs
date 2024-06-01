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
using System.Collections;
using WebChemistry.SiteBinder.Silverlight.DataModel;

namespace WebChemistry.SiteBinder.Silverlight.Controls
{
    public partial class SelectionTreeControl : UserControl
    {
        public SelectionTreeControl()
        {
            InitializeComponent();

            treeView.DataContextChanged += new DependencyPropertyChangedEventHandler(treeView_DataContextChanged);
        }

        void treeView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (treeView.DataContext is SingleSelectionInfo)
            {
                if ((treeView.DataContext as SingleSelectionInfo).Groups.Length < 5) treeView.ExpandAll();
            }
            else if (treeView.DataContext is MultipleSelectionInfo)
            {
                if ((treeView.DataContext as MultipleSelectionInfo).Groups.Length < 5) treeView.ExpandAll();
            }
        }

        bool expanded = false;
        private void ExpandClick(object sender, RoutedEventArgs e)
        {
            if (expanded) treeView.CollapseAll();
            else treeView.ExpandAll();

            expanded = !expanded;
        }
    }
}
