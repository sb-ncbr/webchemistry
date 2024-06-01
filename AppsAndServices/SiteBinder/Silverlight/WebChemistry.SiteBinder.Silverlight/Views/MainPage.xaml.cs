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
using WebChemistry.Framework.Core;
using WebChemistry.SiteBinder.Core;
using WebChemistry.SiteBinder.Silverlight.Visuals;
using System.Threading.Tasks;
using WebChemistry.SiteBinder.Silverlight.ViewModel;
using System.IO;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.SiteBinder.Silverlight
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            VersionText.Text = "SiteBinder " + WebChemistry.SiteBinder.Core.SiteBinderVersion.Version;

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
                    GoToInputTab();
                }
                else
                {
                    DropHelper.DoDrop(session, files);
                }
            }
        }

        public void GoToResultTab()
        {
            tabs.SelectedItem = resultTab;
        }

        public void GoToInputTab()
        {
            tabs.SelectedItem = inputTab;
        }

        private void LayoutRoot_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.I) tabs.SelectedItem = inputTab;
            //else if (e.Key == Key.R) tabs.SelectedItem = resultTab;
        }
    }
}
