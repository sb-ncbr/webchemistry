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
using WebChemistry.Charges.Silverlight.DataModel;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class StructureEntryControl : UserControl
    {
        public StructureEntryControl()
        {
            InitializeComponent();

            try
            {
                LayoutRoot.AllowDrop = true;
            }
            catch
            {
            }
        }

        Brush highlightBrush = new SolidColorBrush { Color = Color.FromArgb(255, 0x11, 0x9E, 0xDA) };

        private void Entry_DragEnter(object sender, DragEventArgs e)
        {
            LayoutRoot.BorderBrush = highlightBrush;
        }

        private void Entry_DragLeave(object sender, DragEventArgs e)
        {
            LayoutRoot.BorderBrush = LayoutRoot.Background;
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

                var s = LayoutRoot.DataContext as StructureWrap;
                files
                    .Where(f => 
                        f.Name.EndsWith("chrg", StringComparison.OrdinalIgnoreCase)
                        || f.Name.EndsWith("mol2", StringComparison.OrdinalIgnoreCase)
                        || f.Name.EndsWith("pqr", StringComparison.OrdinalIgnoreCase))
                    .ForEach(f => s.AddReferenceCharges(f));
            }

            e.Handled = true;
            LayoutRoot.BorderBrush = LayoutRoot.Background;
        }
    }
}
