using System.Windows.Controls;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Silverlight.Controls
{
    public partial class ProgressControl : UserControl
    {
        public ProgressControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = ComputationService.Default;
        }
    }
}
