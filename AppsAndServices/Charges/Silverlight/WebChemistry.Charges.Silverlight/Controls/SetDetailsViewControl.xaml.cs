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
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.Controls
{
    public partial class SetDetailsViewControl : UserControl
    {
        public SetDetailsViewControl()
        {
            InitializeComponent();
        }

        private void DataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            var set = LayoutRoot.DataContext as EemParameterSet;
            if (set != null)
            {
                ShowSet(set);
                return;
            }

            var result = LayoutRoot.DataContext as StructureCharges;
            if (result != null)
            {
                ShowResult(result);
                return;
            }


            if (set == null)
            {
                Paragraph paragraph = new Paragraph() { FontSize = 12 };
                setText.Blocks.Clear();
                paragraph.Inlines.Add(new Run { Text = "Nothing selected." });
                setText.Blocks.Add(paragraph);
                return;
            }
        }

        void ShowSet(EemParameterSet set)
        {
            Paragraph paragraph = new Paragraph() { FontSize = 12 };
            setText.Blocks.Clear();
            
            Action<string> addCaption = val =>
            {
                paragraph.Inlines.Add(new Run { Text = val, FontWeight = FontWeights.Bold });
                paragraph.Inlines.Add(new LineBreak());
            };

            Action<string> addLine = val =>
            {
                paragraph.Inlines.Add(new Run { Text = val });
                paragraph.Inlines.Add(new LineBreak());
            };

            addCaption("Name");
            addLine(set.Name);

            foreach (var p in set.Properties)
            {
                addCaption(p.Item1);
                addLine(p.Item2);
            }

            string nf = "0.00000000";

            paragraph.Inlines.Add(new LineBreak());
            addCaption("Parameter Groups");

            var courier = new FontFamily("Courier New");

            foreach (var paramGroup in set.ParameterGroups)
            {
                paragraph.Inlines.Add(new Run
                {
                    Text = string.Format("Priority = {0}", paramGroup.Priority),
                    FontWeight = FontWeights.Bold,
                    FontFamily = courier
                });
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new Run
                {
                    Text = string.Format("Target   = {0}", paramGroup.TargetQueryString),
                    FontWeight = FontWeights.Bold,
                    FontFamily = courier
                });
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new Run
                {
                    Text = string.Format("Kappa    = {0}", paramGroup.Kappa.ToStringInvariant(nf)),
                    FontWeight = FontWeights.Bold,
                    FontFamily = courier
                });
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new Run { Text = "Element BondType " + "A".PadRight(12) + " B", FontWeight = FontWeights.Bold, FontFamily = courier });
                paragraph.Inlines.Add(new LineBreak());

                paramGroup.Parameters
                        .OrderBy(group => group.Key)
                        .ForEach(g =>
                        {
                            var ordered = g.Value;
                            paragraph.Inlines.Add(new Run { Text = g.Key.ToString().PadRight(8), FontWeight = FontWeights.Bold, FontFamily = courier });
                            paragraph.Inlines.Add(new Run
                            {
                                Text = ordered[0].Multiplicity.ToString().PadRight(8) + " "
                                    + ordered[0].A.ToStringInvariant(nf).PadRight(12) + " "
                                    + ordered[0].B.ToStringInvariant(nf).PadRight(12),
                                FontFamily = courier
                            });
                            paragraph.Inlines.Add(new LineBreak());

                            ordered.Skip(1).ForEach(p =>
                            {
                                paragraph.Inlines.Add(new Run
                                {
                                    Text = "".PadRight(8) + p.Multiplicity.ToString().PadRight(8) + " " +
                                        p.A.ToStringInvariant(nf).PadRight(12) + " " +
                                        p.B.ToStringInvariant(nf).PadRight(12),
                                    FontFamily = courier
                                });
                                paragraph.Inlines.Add(new LineBreak());
                            });

                        });

                paragraph.Inlines.Add(new LineBreak());
            }
            
            setText.Blocks.Add(paragraph);

            TextPointer pstart = setText.ContentStart;
            setText.Selection.Select(pstart, pstart);
        }

        static Brush WarningBrush = new SolidColorBrush { Color = Color.FromArgb(0xFF, 0xFF, 0x85, 0x40) };
        static Brush ErrorBrush = new SolidColorBrush { Color = Color.FromArgb(0xFF, 0xED, 0x00, 0x2F) };

        void ShowResult(StructureCharges charges)
        {
            Paragraph paragraph = new Paragraph() { FontSize = 12 };
            setText.Blocks.Clear();

            Action<string> addCaption = val =>
            {
                paragraph.Inlines.Add(new Run { Text = val, FontWeight = FontWeights.Bold });
                paragraph.Inlines.Add(new LineBreak());
            };

            Action<string> addLine = val =>
            {
                paragraph.Inlines.Add(new Run { Text = val });
                paragraph.Inlines.Add(new LineBreak());
            };

            Action<string, object> addEntry = (c, t) =>
            {
                addCaption(c);
                addLine(t.ToString());
            };

            addEntry("Structure", charges.Structure.Structure.Id);
            addEntry("Name", charges.Name);

            var parameters = charges.Result.Parameters;
            switch (parameters.Method)
            {
                case ChargeComputationMethod.Eem:
                    addEntry("Method", "EEM");
                    break;
                case ChargeComputationMethod.EemCutoff:
                    addEntry("Method", "EEM Cutoff " + parameters.CutoffRadius.ToStringInvariant("0") + " ang");
                    break;
                case ChargeComputationMethod.Reference:
                    addEntry("Method", "Reference");
                    break;
            }

            addEntry("Options", (parameters.IgnoreWaters ? "No Waters" : "With Waters") + ", " + (parameters.SelectionOnly ? "Selected Atoms" : "All Atoms"));

            //addEntry("Computed Total Charge", charges.Result.ComputedTotalCharge.ToStringInvariant("0.000"));

            paragraph.Inlines.Add(new Run { Text = "Total Charge", FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Input = " + charges.Result.Parameters.TotalCharge });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Computed = " + charges.Result.ComputedTotalCharge.ToStringInvariant("0.000") });
            paragraph.Inlines.Add(new LineBreak());

            addEntry("Result Size", charges.Result.Charges.Count);

            switch (charges.Result.State)
            {
                case ChargeResultState.Ok:
                    addEntry("State", "All is well");
                    break;
                case ChargeResultState.Warning:
                    addEntry("State", "Warning");
                    //addEntry("Warning Message", charges.Result.Message);
                    paragraph.Inlines.Add(new Run { Text = "Warning Message", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run { Text = charges.Result.Message, Foreground = WarningBrush });
                    paragraph.Inlines.Add(new LineBreak());
                    break;
                case ChargeResultState.Error:
                    addEntry("State", "Error");
                    //addEntry("Error Message", charges.Result.Message);

                    paragraph.Inlines.Add(new Run { Text = "Error Message", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run { Text = charges.Result.Message, Foreground = ErrorBrush });
                    paragraph.Inlines.Add(new LineBreak());
                    break;
            }

            addEntry("Date Created", charges.Result.TimeCreatedUtc.ToLocalTime().ToString());

            addEntry("Computation Time", ComputationService.GetElapasedTimeString(charges.Result.Timing));

            string nf = "0.00000000";

            if (parameters.Method != ChargeComputationMethod.Reference)
            {

                paragraph.Inlines.Add(new LineBreak());
                addCaption("Parameters");

                var courier = new FontFamily("Courier New");

                foreach (var paramGroup in parameters.Set.ParameterGroups)
                {
                    paragraph.Inlines.Add(new Run
                    {
                        Text = string.Format("Priority = {0}", paramGroup.Priority),
                        FontWeight = FontWeights.Bold,
                        FontFamily = courier
                    });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run
                    {
                        Text = string.Format("Target   = {0}", paramGroup.TargetQueryString),
                        FontWeight = FontWeights.Bold,
                        FontFamily = courier
                    });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run
                    {
                        Text = string.Format("Kappa    = {0}", paramGroup.Kappa.ToStringInvariant(nf)),
                        FontWeight = FontWeights.Bold,
                        FontFamily = courier
                    });
                    paragraph.Inlines.Add(new LineBreak());
                    paragraph.Inlines.Add(new Run { Text = "Element BondType " + "A".PadRight(12) + " B", FontWeight = FontWeights.Bold, FontFamily = courier });
                    paragraph.Inlines.Add(new LineBreak());

                    paramGroup.Parameters
                            .OrderBy(group => group.Key)
                            .ForEach(g =>
                            {
                                var ordered = g.Value;
                                paragraph.Inlines.Add(new Run { Text = g.Key.ToString().PadRight(8), FontWeight = FontWeights.Bold, FontFamily = courier });
                                paragraph.Inlines.Add(new Run
                                {
                                    Text = ordered[0].Multiplicity.ToString().PadRight(8) + " "
                                        + ordered[0].A.ToStringInvariant(nf).PadRight(12) + " "
                                        + ordered[0].B.ToStringInvariant(nf).PadRight(12),
                                    FontFamily = courier
                                });
                                paragraph.Inlines.Add(new LineBreak());

                                ordered.Skip(1).ForEach(p =>
                                {
                                    paragraph.Inlines.Add(new Run
                                    {
                                        Text = "".PadRight(8) + p.Multiplicity.ToString().PadRight(8) + " " +
                                            p.A.ToStringInvariant(nf).PadRight(12) + " " +
                                            p.B.ToStringInvariant(nf).PadRight(12),
                                        FontFamily = courier
                                    });
                                    paragraph.Inlines.Add(new LineBreak());
                                });

                            });

                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            setText.Blocks.Add(paragraph);

            TextPointer pstart = setText.ContentStart;
            setText.Selection.Select(pstart, pstart);
        }
    }
}
