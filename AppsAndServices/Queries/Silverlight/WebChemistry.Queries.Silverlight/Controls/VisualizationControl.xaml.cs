using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ImageTools;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Queries.Silverlight.DataModel;
using WebChemistry.Queries.Silverlight.Visuals;
using WebChemistry.Silverlight.Common;

namespace WebChemistry.Queries.Silverlight.Controls
{
    public partial class VisualizationControl : UserControl
    {
        public VisualizationControl()
        {
            InitializeComponent();

            LayoutRoot.MouseEnter += (_, __) =>
            {
                if (viewport.Visual is MotiveVisual3DWrap)
                {
                    (viewport.Visual as MotiveVisual3DWrap).SuppressTooltip = false;
                }
            };

            LayoutRoot.MouseLeave += (_, __) =>
            {
                if (viewport.Visual is MotiveVisual3DWrap)
                {
                    (viewport.Visual as MotiveVisual3DWrap).SuppressTooltip = true;
                }
            };

            LayoutRoot.MouseEnter += (_, __) =>
            {
                if (viewport.Visual is MotiveVisual3DWrap)
                {
                    (viewport.Visual as MotiveVisual3DWrap).SuppressTooltip = false;
                }
            };
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            viewport.RenderToPng();
        }
    }
}
