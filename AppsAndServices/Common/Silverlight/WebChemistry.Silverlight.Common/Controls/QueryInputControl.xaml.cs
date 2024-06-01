using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Queries.Core;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Queries.Core.MetaQueries;
using WebChemistry.Framework.TypeSystem;

namespace WebChemistry.Silverlight.Controls
{
    public enum QueryInputType
    {
        Any,
        MotiveSeq,
        Bool,
        Value
    }

    public partial class QueryInputControl : UserControl
    {
        //public IEnumerable<string> QueryHistory
        //{
        //    get { return (IEnumerable<string>)GetValue(QueryHistoryProperty); }
        //    set { SetValue(QueryHistoryProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for QueryHistory.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty QueryHistoryProperty =
        //    DependencyProperty.Register("QueryHistory", typeof(IEnumerable<string>), typeof(QueryInputControl), new PropertyMetadata(null, QueryHistoryChanged));
        

        //static void QueryHistoryChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        //{
        //    (sender as QueryInputControl).queryHistory.ItemsSource = (sender as QueryInputControl).QueryHistory;
        //}

        string emptyString = "Enter something.";

        public string QueryString
        {
            get { return (string)GetValue(QueryStringProperty); }
            set { SetValue(QueryStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for QueryString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueryStringProperty =
            DependencyProperty.Register("QueryString", typeof(string), typeof(QueryInputControl), new PropertyMetadata(""));

        public QueryInputType Type
        {
            get { return (QueryInputType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Type.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(QueryInputType), typeof(QueryInputControl), new PropertyMetadata(QueryInputType.Any, InputTypeChanged));

        static void InputTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as QueryInputControl).UpdateEmptyString();
        }

        void UpdateEmptyString()
        {
            switch (Type)
            {
                case QueryInputType.MotiveSeq: emptyString = "Enter Pattern sequence."; break;
                case QueryInputType.Bool: emptyString = "Enter boolean."; break;
                case QueryInputType.Value: emptyString = "Enter value (string/number/bool)."; break;
                case QueryInputType.Any: emptyString = "Enter something."; break;
            }

            if (string.IsNullOrWhiteSpace(queryString.Text))
            {
                statusText.Text = emptyString;
            }
        }

        public ICommand RunCommand
        {
            get { return (ICommand)GetValue(RunCommandProperty); }
            set { SetValue(RunCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RunCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RunCommandProperty =
            DependencyProperty.Register("RunCommand", typeof(ICommand), typeof(QueryInputControl), new PropertyMetadata(null));



        public object RunCommandParameter
        {
            get { return (object)GetValue(RunCommandParameterProperty); }
            set { SetValue(RunCommandParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RunCommandParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RunCommandParameterProperty =
            DependencyProperty.Register("RunCommandParameter", typeof(object), typeof(QueryInputControl), new PropertyMetadata(null));

        bool isValid = false;

        public QueryInputControl()
        {
            InitializeComponent();

            LayoutRoot.DataContext = this;
            queryHistory.ItemsSource = QueryService.Default.QueryHistory;

            Observable.FromEventPattern(queryString, "TextChanged")
                .Select(_ => queryString.Text)
                .DistinctUntilChanged()
                .Subscribe(query => Validate(query));
        }

        void Validate(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                statusText.Text = emptyString;
                hitEnterText.Visibility = System.Windows.Visibility.Collapsed;
                isValid = false;
                return;
            }

            try
            {
                TypeExpression desiredType = null;
                switch (Type)
                {
                    case QueryInputType.MotiveSeq: desiredType = BasicTypes.PatternSeq; break;
                    case QueryInputType.Bool: desiredType = BasicTypes.Bool; break;
                    case QueryInputType.Value: desiredType = BasicTypes.Value; break;
                }

                var metaquery = ScriptService.Default.GetMetaQuery(query, desiredType);
               
                metaquery.Compile();
                statusText.Text = "";
                hitEnterText.Visibility = System.Windows.Visibility.Visible;
                isValid = true;
            }
            catch (Exception e)
            {
                statusText.Text = e.Message;
                hitEnterText.Visibility = System.Windows.Visibility.Collapsed;
                isValid = false;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (queryHistory.SelectedValue != null)
            {
                queryString.Text = queryHistory.SelectedValue as string;
                queryHistory.SelectedValue = null;
            }
        }

        private void LayoutRoot_GotFocus_1(object sender, RoutedEventArgs e)
        {
            queryString.Focus();
        }

        int currentHistoryIndex = 0, selStart = 0, selCount = 0;
        bool ctrlPressed = false;
        string enteredQuery = "";

        public string GetHistoryEntry()
        {
            if (currentHistoryIndex == 0) return enteredQuery;

            var hist = QueryService.Default.QueryHistory;
            var count = hist.Count;

            if (count == 0 || currentHistoryIndex > count)
            {
                currentHistoryIndex = 0;
                return enteredQuery;
            }

            var i = (currentHistoryIndex - 1) % count;
            if (i < 0) i += count;
            currentHistoryIndex = i + 1;
            return hist[i];
        }

        private void queryString_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (!ctrlPressed && e.Key == Key.Ctrl)            
            {
                ctrlPressed = true;
                currentHistoryIndex = 0;
                enteredQuery = queryString.Text;
                selStart = queryString.SelectionStart;
                selCount = queryString.SelectionLength;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {   
                if (e.Key == Key.Enter)
                {
                    if (RunCommand == null) return;
                    if (isValid && RunCommand.CanExecute(RunCommandParameter)) RunCommand.Execute(RunCommandParameter);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Up)
                {
                    currentHistoryIndex--;
                    queryString.Text = GetHistoryEntry();
                    queryString.Select(selStart, selCount);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Down)
                {
                    currentHistoryIndex++;
                    queryString.Text = GetHistoryEntry();
                    queryString.Select(selStart, selCount);
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                if (RunCommand == null) return;
                if (isValid && RunCommand.CanExecute(RunCommandParameter)) RunCommand.Execute(RunCommandParameter);
                e.Handled = true;
            }
            //else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.H)
            //{
            //    queryHistory.IsDropDownOpen = true;
            //    queryHistory.Focus();
            //}
        }

        private void queryHistory_KeyDown_1(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    queryHistory.IsDropDownOpen = false;
            //    queryString.Focus();
            //}
        }

        private void queryString_KeyUp_1(object sender, KeyEventArgs e)
        {
            ctrlPressed = false;
        }

        private void LayoutRoot_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            //statusText.Width = LayoutRoot.ActualWidth - 30;
            statusText.MaxWidth = LayoutRoot.ActualWidth - 30;
        }
    }
}
