using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Framework.Core;
using System.Windows.Input;

namespace WebChemistry.Silverlight.Controls
{
    public partial class ScriptElementControl : UserControl
    {
        ScriptElement element;

        public ScriptElementControl()
        {
            InitializeComponent();

            Observable.FromEventPattern(scriptText, "TextChanged")
                .Select(_ => scriptText.Text)
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnDispatcher()
                .Subscribe(e => Validate(e));

            Validate("");

            //Observable.Timer(TimeSpan.FromMilliseconds(125))
            //    .ObserveOnDispatcher()
            //    .Subscribe(_ => scriptText.Focus());
        }

        void Validate(string input)
        {
            if (element == null) return;

            if (string.IsNullOrWhiteSpace(input))
            {
                element.State = ScriptingElementState.Empty;
                return;
            }

            try
            {
                ScriptService.Default.TryCompile(input);
                element.State = ScriptingElementState.Executable;
            }
            catch (Exception e)
            {
                element.ErrorMessage = e.Message;
                element.State = ScriptingElementState.Error;
            }
        }

        void UpdateState()
        {
            if (element.State == ScriptingElementState.Executed)
            {
                scriptText.IsReadOnly = true;
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
            }
        }

        private void scriptText_KeyDown(object sender, KeyEventArgs e)
        {
            if (element == null) return;
            //if (!ctrlPressed && e.Key == Key.Ctrl)
            //{
            //    ctrlPressed = true;
            //    currentHistoryIndex = 0;
            //    enteredQuery = queryString.Text;
            //    selStart = queryString.SelectionStart;
            //    selCount = queryString.SelectionLength;
            //}

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Enter)
                {
                    //if (RunCommand == null) return;
                    //if (isValid && RunCommand.CanExecute(RunCommandParameter)) RunCommand.Execute(RunCommandParameter);
                    element.ExecuteCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void scriptText_Loaded_1(object sender, RoutedEventArgs e)
        {
            scriptText.Focus();
        }
    }
}
