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
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Framework.Core;

namespace WebChemistry.Silverlight.Controls
{
    public partial class ScriptStateControl : UserControl
    {
        ScriptElement element;

        public ScriptStateControl()
        {
            InitializeComponent();
        }

        void UpdateState()
        {
            switch (element.State)
            {
                case ScriptingElementState.Empty:
                    stateText.Text = "Enter something.";
                    break;
                case ScriptingElementState.Error:
                    stateText.Text = element.ErrorMessage ?? "";
                    break;
                case ScriptingElementState.Executable:
                    stateText.Text = "[Ctrl+Enter] to execute.";
                    break;
                case ScriptingElementState.Executed:
                    this.Visibility = System.Windows.Visibility.Collapsed;
                    break;
            }
        }

        private void LayoutRoot_DataContextChanged_1(object sender, DependencyPropertyChangedEventArgs e)
        {
            element = this.DataContext as ScriptElement;

            if (element != null)
            {
                element.ObservePropertyChanged(this, (l, s, n) =>
                {
                    if (n.EqualOrdinal("State")) l.UpdateState();
                });
                UpdateState();
            }
        }
    }
}
