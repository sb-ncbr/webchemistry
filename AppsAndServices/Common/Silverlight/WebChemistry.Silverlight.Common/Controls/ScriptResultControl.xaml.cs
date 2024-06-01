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
using WebChemistry.Queries.Core;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Silverlight.Common;
using System.Threading.Tasks;
using System.IO;
using WebChemistry.Silverlight.Common.Utils;
using System.Collections;
using System.Text;

namespace WebChemistry.Silverlight.Controls
{
    public partial class ScriptResultControl : UserControl
    {
        ScriptElement element;

        public ScriptResultControl()
        {
            InitializeComponent();
        }

        void UpdateState()
        {
            switch (element.State)
            {
                case ScriptingElementState.Empty:
                case ScriptingElementState.Error:
                case ScriptingElementState.Executable:
                    this.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case ScriptingElementState.Executed:
                    this.Visibility = System.Windows.Visibility.Visible;
                    var result = element.Result;
                    if (!string.IsNullOrWhiteSpace(element.StdOut))
                    {
                        resultText.Text = element.StdOut;
                    }
                    else
                    {
                        resultText.Text = "";
                    }

                    if (result == null && string.IsNullOrWhiteSpace(element.StdOut)) resultText.Text += "<Action>";
                    else if (result != null)
                    {
                        if (result is IEnumerable<Motive>)
                        {
                            resultText.Text += string.Format("<{0} motive(s)>", (result as IEnumerable<Motive>).Count());
                        }
                        else if (result is Motive)
                        {
                            var atoms = string.Join(", ", (result as Motive).Atoms.OrderBy(a => a.Id).Select(a => a.PdbName() + " " + a.Id));
                            resultText.Text += string.Format("<{0}>", atoms);
                        }
                        else if (result is IEnumerable<QueriesScripting.ContextResult>)
                        {
                            var data = (result as IEnumerable<QueriesScripting.ContextResult>).AsList();
                            if (data.Count == 0) resultText.Text += "No data.";
                            var columnType = data[0].Result is double || data[0].Result is int || data[0].Result is long || data[0].Result is float ? ColumnType.Number : ColumnType.String;
                            resultText.Text += data.GetExporter()
                                .AddExportableColumn(x => x.StructureId, ColumnType.String, "Id")
                                .AddExportableColumn(x => x.Result == null ? "" : x.Result.ToString(), columnType, "Value")
                                .ToCsvString();
                        }
                        else if (result is IEnumerable && !(result is string))
                        {
                            var builder = new StringBuilder();
                            foreach (var r in (result as IEnumerable)) builder.AppendLine(r.ToString());
                            if (builder.Length == 0) resultText.Text = "Empty collection.";
                            else resultText.Text += builder.ToString();
                        }
                        else resultText.Text += result.ToString();
                    }

                    resultText.Select(resultText.Text.Length - 1, 0);
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

        private void LayoutRoot_MouseEnter_1(object sender, MouseEventArgs e)
        {
            exportPanel.Visibility = System.Windows.Visibility.Visible;
            hoverBackgroud.Visibility = System.Windows.Visibility.Visible;

            exportButton.Visibility = CanExport() ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            sep.Visibility = CanExport() ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void LayoutRoot_MouseLeave_1(object sender, MouseEventArgs e)
        {
            exportPanel.Visibility = System.Windows.Visibility.Collapsed;
            hoverBackgroud.Visibility = System.Windows.Visibility.Collapsed;
        }

        bool CanExport()
        {
            return element.Result is IEnumerable<Motive> || element.Result is Motive;
        }

        private void copyButton_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(resultText.Text);
        }

        async void ExportMotives()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip"
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Exporting...");
                try
                {
                    var motives = element.Result as IEnumerable<Motive>;
                    using (var stream = sfd.OpenFile())
                    {
                        await ZipUtils.CreateZip(stream, zip => TaskEx.Run(() =>
                        {
                            int index = 0;
                            foreach (Motive m in motives)
                            {
                                var s = m.ToStructure((index++).ToString(), true, true);
                                zip.BeginEntry(s.Id + ".pdb");
                                s.WritePdb(zip.TextWriter);
                                zip.EndEntry();
                            }
                        }));
                    }
                }
                finally
                {
                    cs.End();
                }
            }
        }

        void ExportMotive()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "PDB files (*.pdb)|*.pdb"
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Exporting...");
                try
                {
                    var motive = element.Result as Motive;
                    using (var stream = sfd.OpenFile())
                    using (var writer = new StreamWriter(stream))
                    {
                        var s = motive.ToStructure("0", true, true);
                        s.WritePdb(writer);
                    }
                }
                finally
                {
                    cs.End();
                }
            }
        }

        private void exportButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (element.Result is IEnumerable<Motive>) ExportMotives();
            else if (element.Result is Motive) ExportMotive();
        }
    }
}
