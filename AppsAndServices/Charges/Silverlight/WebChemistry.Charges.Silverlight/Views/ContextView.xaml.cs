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
using WebChemistry.Framework.Core;

namespace WebChemistry.Charges.Silverlight.Views
{
    public partial class ContextView : UserControl
    {
        public ContextView()
        {
            InitializeComponent();

            try
            {
                var mainvm = ServiceLocator.Current.GetInstance<MainViewModel>();
                var analyzevm = ServiceLocator.Current.GetInstance<AnalyzeViewModel>();

                mainvm.ObservePropertyChanged(this, (s, vm, arg) =>
                {
                    if (arg == "Mode") s.UpdateMode();
                });

                analyzevm.ObservePropertyChanged(this, (s, vm, arg) =>
                {
                    if (arg == "Mode") s.UpdateMode();
                });
            }
            catch
            {
            }
        }

        void UpdateMode()
        {
            controls.Children.ForEach(c => c.Visibility = System.Windows.Visibility.Collapsed);
            var mode = ServiceLocator.Current.GetInstance<MainViewModel>().Mode;
            switch (mode)
            {
                case AppMode.Input:
                    setsControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                case AppMode.Result:
                    resultControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                case AppMode.EditSet:
                    editorControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                case AppMode.Analyze:
                    {
                        var vm = ServiceLocator.Current.GetInstance<AnalyzeViewModel>();
                        if (vm.Mode == AnalyzeMode.Correlate) correlationControl.Visibility = System.Windows.Visibility.Visible;
                        else aggregateControl.Visibility = System.Windows.Visibility.Visible;
                        break;
                    }
                case AppMode.Visualization:
                    visualizationControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                default:
                    break;
            }
        }
    }
}
