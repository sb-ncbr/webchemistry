using Microsoft.Practices.ServiceLocation;
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
using WebChemistry.Charges.Silverlight.ViewModel;

namespace WebChemistry.Charges.Silverlight.Views
{
    public partial class AnalyzeView : UserControl
    {
        public AnalyzeView()
        {
            InitializeComponent();
        }

        private void anylysisTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = ServiceLocator.Current.GetInstance<AnalyzeViewModel>();
            var tabs = sender as TabControl;
            switch ((tabs.SelectedItem as TabItem).Name)
            {
                case "correlateTab":
                    vm.Mode = AnalyzeMode.Correlate;
                    break;
                case "aggregateTab":
                    vm.Mode = AnalyzeMode.Aggregate;
                    break;
                default:
                    break;
            }
        }
    }
}
