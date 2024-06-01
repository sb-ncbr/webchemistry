using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Charges.Silverlight.ViewModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Charges.Silverlight.Views
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            this.VersionText.Text = "Charges " + EemSolver.Version;

            try
            {
                var mainvm = ServiceLocator.Current.GetInstance<MainViewModel>();

                mainvm.ObservePropertyChanged(this, (s, vm, arg) =>
                {
                    if (arg == "Mode") s.UpdateMode(vm.Mode);
                });
            }
            catch
            {
            }

            LayoutRoot.Loaded += (s, a) =>
            {
                try
                {
                    LayoutRoot.AllowDrop = true;
                }
                catch
                {
                }
            };

            LayoutRoot.Drop += new DragEventHandler(LayoutRoot_Drop);
        }


        void LayoutRoot_Drop(object sender, DragEventArgs e)
        {
            if (e.Data != null)
            {
                FileInfo[] files = null;

                try
                {
                    files = e.Data.GetData(DataFormats.FileDrop) as FileInfo[];
                }
                catch
                {
                }

                if (files == null) return;

                var session = ServiceLocator.Current.GetInstance<Session>();

                if (files.Length == 1 && files[0].Extension.Equals(session.WorkspaceExtension, StringComparison.OrdinalIgnoreCase))
                {
                    DropHelper.DoDrop(session, files);
                }
                else
                {
                    DropHelper.DoDrop(session, files, extras =>
                    {
                        var references = extras.Where(f => f.Name.EndsWith("chrg", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (references.Length > 0) session.LoadReferenceCharges(references);
                        var xmls = extras.Where(f => f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (xmls.Length > 0) session.LoadSets(xmls);
                    });
                }
            }
        }


        void UpdateMode(AppMode mode)
        {
            switch (mode)
            {
                case AppMode.Input:
                    tabs.SelectedItem = inputTab;
                    break;
                case AppMode.EditSet:
                    break;
                case AppMode.Result:
                    tabs.SelectedItem = resultTab;
                    break;
                case AppMode.Analyze:
                    tabs.SelectedItem = analyzeTab;
                    break;
                case AppMode.Visualization:
                    tabs.SelectedItem = visualizeTab;
                    break;
                default:
                    break;
            }
        }

        private void tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = ServiceLocator.Current.GetInstance<MainViewModel>();
            var tabs = sender as TabControl;
            switch ((tabs.SelectedItem as TabItem).Name)
            {
                case "inputTab":
                    vm.Mode = AppMode.Input;
                    break;
                case "resultTab":
                    vm.Mode = AppMode.Result;
                    break;
                case "analyzeTab":
                    vm.Mode = AppMode.Analyze;
                    break;
                case "visualizeTab":
                    vm.Mode = AppMode.Visualization;
                    break;
                default:
                    break;
            }
        }
    }
}
