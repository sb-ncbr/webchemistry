using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Silverlight.Controls
{
    public partial class ScriptControl : UserControl
    {
        public ScriptControl()
        {
            InitializeComponent();

            ScriptService.Default.Elements
                .Subscribe(async e =>
                    {
                        scriptElements.Items.Add(e);
                        await TaskEx.Yield();
                        view.ScrollToBottom();
                    });

            ScriptService.Default.CreateNewElement();
        }

        private void NewButton_Click_1(object sender, RoutedEventArgs e)
        {
            ScriptService.Default.CreateNewElement();
        }

        private void ClearButton_Click_1(object sender, RoutedEventArgs e)
        {
            scriptElements.Items.Clear();
            ScriptService.Default.ResetScope();
            ScriptService.Default.CreateNewElement();
        }
    }
}
