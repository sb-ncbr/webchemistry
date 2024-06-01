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
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Charges.Silverlight.ViewModel;
using WebChemistry.Framework.Core;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class SetEditorControl : UserControl
    {
        public SetEditorControl()
        {
            InitializeComponent();
        }
        
        private void ParameterSetXml_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var start = ParameterSetXml.SelectionStart;
            int line = 1;
            int lastNewLine = 0;
            var text = ParameterSetXml.Text;

            char newLine = text.Contains('\n') ? '\n' : '\r';

            for (int i = 0; i < start; i++)
            {
                if (text[i] == newLine)
                {
                    line++;
                    lastNewLine = i;
                }
            }

            LineNumber.Text = string.Format("Line: {0}, Position: {1}", line, start - lastNewLine);
        }
    }
}
