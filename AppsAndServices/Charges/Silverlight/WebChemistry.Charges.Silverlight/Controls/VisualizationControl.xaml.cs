using System.Windows;
using System.Windows.Controls;
using WebChemistry.Charges.Silverlight.Visuals;
using WebChemistry.Silverlight.Common;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class VisualizationControl : UserControl
    {
        public VisualizationControl()
        {
            InitializeComponent();

            viewportWrap.MouseLeave += (_, __) =>
            {
                if (viewport.Visual is ChargeStructureVisual3D)
                {
                    (viewport.Visual as ChargeStructureVisual3D).SuppressTooltip = true;
                }
            };

            viewportWrap.MouseEnter += (_, __) =>
            {
                if (viewport.Visual is ChargeStructureVisual3D)
                {
                    (viewport.Visual as ChargeStructureVisual3D).SuppressTooltip = false;
                }
            };
        }

        private void SaveImage(object sender, RoutedEventArgs e)
        {
            viewport.RenderToPng();
        }
    }
}
