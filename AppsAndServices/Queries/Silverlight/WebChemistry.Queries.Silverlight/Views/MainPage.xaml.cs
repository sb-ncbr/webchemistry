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
using WebChemistry.Queries.Silverlight.DataModel;
using WebChemistry.Silverlight.Common.Utils;

namespace WebChemistry.Queries.Silverlight
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            VersionText.Text = "MotiveExplorer " + WebChemistry.Queries.Core.Query.Version;
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

                DropHelper.DoDrop(ServiceLocator.Current.GetInstance<Session>(), files);
            }
        }
    }
}
