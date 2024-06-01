using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Silverlight.Common;
using WebChemistry.Charges.Core;
using System.Threading.Tasks;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class ResultViewModel : ObservableObject
    {
        [Ninject.Inject]
        public Session Session { get; set; }

        private ICommand _setCurrentResultCommand;
        public ICommand SetCurrentResultCommand
        {
            get
            {
                _setCurrentResultCommand = _setCurrentResultCommand ?? new RelayCommand<StructureCharges>(r => CurrentResult = r);
                return _setCurrentResultCommand;
            }
        }

        private ICommand _copyGroupChargesCommand;
        public ICommand CopyGroupChargesCommand
        {
            get
            {
                _copyGroupChargesCommand = _copyGroupChargesCommand ?? new RelayCommand<CollectionViewGroup>(g => CopyGroupCharges(g, false));
                return _copyGroupChargesCommand;
            }
        }

        private ICommand _copyGroupDetailsCommand;
        public ICommand CopyGroupDetailsCommand
        {
            get
            {
                _copyGroupDetailsCommand = _copyGroupDetailsCommand ?? new RelayCommand<CollectionViewGroup>(g => CopyGroupCharges(g, true));
                return _copyGroupDetailsCommand;
            }
        }

        private ICommand _exportAllCommand;
        public ICommand ExportAllCommand
        {
            get
            {
                _exportAllCommand = _exportAllCommand ?? new RelayCommand(() => ExportAll());
                return _exportAllCommand;
            }
        }

        private StructureCharges _currentResult;
        public StructureCharges CurrentResult
        {
            get
            {
                return _currentResult;
            }

            set
            {
                if (_currentResult == value) return;

                _currentResult = value;
                NotifyPropertyChanged("CurrentResult");
            }
        }

        public ObservableCollection<StructureCharges> Results { get; private set; }

        private PagedCollectionView _resultsView;
        public PagedCollectionView ResultsView
        {
            get
            {
                return _resultsView;
            }

            set
            {
                if (_resultsView == value) return;

                _resultsView = value;
                NotifyPropertyChanged("ResultsView");
            }
        }

        public void Update()
        {
            Results = new ObservableCollection<StructureCharges>(Session.Structures.SelectMany(s => s.Charges).ToList());
            this.ResultsView = new PagedCollectionView(Results);
            this.ResultsView.SortDescriptions.Add(new SortDescription("Structure.Structure.Id", ListSortDirection.Ascending));
            this.ResultsView.SortDescriptions.Add(new SortDescription("Result.State", ListSortDirection.Ascending));
            this.ResultsView.SortDescriptions.Add(new SortDescription("Result.Parameters.MethodId", ListSortDirection.Ascending));

            this.ResultsView.GroupDescriptions.Add(new PropertyGroupDescription("Structure.Structure.Id") { StringComparison = StringComparison.Ordinal });
        }

        static ListExporter GetCsv(string separator, IList<StructureCharges> charges, bool includeDetails)
        {
            var pivot = charges[0];         
            var structure = pivot.Structure.Structure;

            var exporter = structure.Atoms.GetExporter(separator)
                .AddExportableColumn(a => a.Id, ColumnType.Number, "AtomId");

            var detailsPivot = charges.FirstOrDefault(c => c.Result.Parameters.Method != Core.ChargeComputationMethod.Reference);
            if (detailsPivot != null && includeDetails)
            {
                var detailsResult = detailsPivot.Result;
                exporter.AddExportableColumn(a => a.ElementSymbol, ColumnType.String, "Element")
                    .AddExportableColumn(a =>
                    {
                        int mult;
                        if (detailsResult.Multiplicities.TryGetValue(a, out mult)) return mult.ToString();
                        return "-";
                    }, ColumnType.Number, "Type");
            }

            foreach (StructureCharges c in charges)
            {
                if (includeDetails && c.Result.Parameters.Method != Core.ChargeComputationMethod.Reference) c.AddDetailColumns(exporter);
                else c.AddChargeColumn(exporter);
            }

            return exporter;
        }

        static ListExporter GetPartitionCsv(string separator, string partition, IList<StructureCharges> charges)
        {
            var pivot = charges[0];
            //var structure = pivot.Structure.Structure;

            var groups = pivot.PartitionCharges[partition].Partition.Groups;

            var exporter = pivot.PartitionCharges[partition].Partition.Groups.GetExporter(separator)
                .AddExportableColumn(r => r.Id, ColumnType.Number, "Id")
                .AddExportableColumn(r => r.Residues, ColumnType.String, "ResidueSignature")
                .AddExportableColumn(r => r.ResidueCount, ColumnType.Number, "ResidueCount")
                .AddExportableColumn(r => r.ResidueIdentifiers, ColumnType.String, "Residues")
                .AddExportableColumn(r => r.Atoms, ColumnType.String, "AtomSignature")
                .AddExportableColumn(r => r.Data.Count, ColumnType.Number, "AtomCount")
                .AddExportableColumn(r => r.AtomIdentifiers, ColumnType.String, "Atoms");
                     
            foreach (StructureCharges c in charges)
            {
                c.AddParitionChargeColumn(partition, exporter);
            }

            return exporter;
        }

        void CopyGroupCharges(CollectionViewGroup group, bool includeDetails)
        {
            try
            {
                var pivot = group.Items[0] as StructureCharges;
                var exporter = GetCsv("\t", pivot.Structure.Charges, includeDetails);
                Clipboard.SetText(exporter.ToCsvString());
                LogService.Default.Message("Data copied to clipboard.");
            }
            catch (Exception e)
            {
                LogService.Default.Error("Clipboard Copy", e.Message);
            }

        }

        async void ExportAll()
        {
            if (ResultsView == null || ResultsView.Count == 0)
            {
                LogService.Default.Info("No data to export.");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip"
            };

            if (sfd.ShowDialog() == true)
            {
                var cs = ComputationService.Default;
                var progress = cs.Start();
                progress.Update(statusText: "Exporting...", currentProgress: 0, maxProgress: Session.Structures.Count, canCancel: false, isIndeterminate: false);
                try
                {
                    using (var f = sfd.OpenFile())
                    {
                        await ZipUtils.CreateZip(f, context => TaskEx.Run(() =>
                        {
                            for (int i = 0; i < Session.Structures.Count; i++)
                            {
                                var s = Session.Structures[i];
                                var folder = s.Structure.Id + "\\";

                                var chargeSets = s.Charges;//.Concat(s.ReferenceCharges).ToArray();

                                var csv = GetCsv(",", chargeSets, true);
                                context.BeginEntry(folder + "detailed_charges.csv");
                                csv.WriteCsvString(context.TextWriter);
                                context.TextWriter.Flush();
                                context.EndEntry();

                                csv = GetCsv(",", chargeSets, false);
                                context.BeginEntry(folder + "charges.csv");
                                csv.WriteCsvString(context.TextWriter);
                                context.TextWriter.Flush();
                                context.EndEntry();

                                foreach (var p in Session.ComputedPartitions)
                                {
                                    csv = GetPartitionCsv(",", p, chargeSets);
                                    context.BeginEntry(string.Format("{0}partition_{1}.csv", folder, p));
                                    csv.WriteCsvString(context.TextWriter);
                                    context.TextWriter.Flush();
                                    context.EndEntry();                                    
                                }
                                
                                foreach (var c in chargeSets)
                                {
                                    foreach (var p in c.PartitionCharges)
                                    {
                                        context.BeginEntry(string.Format("{0}{1}_partition_{2}.mol2", folder, c.Name, p.Key));
                                        {
                                            var partition = s.Partitions.First(t => t.Name.EqualOrdinalIgnoreCase(p.Key));
                                            var pCharges = p.Value.PartitionCharges;
                                            context.TextWriter.WriteLine("# To determine groups corresponding to individual atoms, use 'partition_{0}.csv'.", p.Key);
                                            context.TextWriter.WriteLine("# <i>-th atom (zero based) corresponds to row with id <i>.");
                                            partition.InducedStructure.WriteMol2(context.TextWriter, remark: string.Format("{0}_partition_{1}.mol2", c.Name, p.Key), chargeSelector: a =>
                                            {
                                                double value;
                                                if (pCharges.TryGetValue(partition.Groups[a.Id], out value)) return value;
                                                return 0.0;
                                            });
                                        }
                                        context.TextWriter.Flush();
                                        context.EndEntry();

                                        foreach (var clusterStats in p.Value.ClusterStats)
                                        {
                                            csv = PartitionClustering.GetClusterStatsExporter(",", clusterStats.Stats);
                                            context.BeginEntry(string.Format("{0}{1}_partition_{2}_aggregate_{3}.csv", folder, c.Name, p.Key, clusterStats.Clustering.Name));
                                            csv.WriteCsvString(context.TextWriter);
                                            context.TextWriter.Flush();
                                            context.EndEntry();
                                        }
                                    }

                                    context.BeginEntry(folder + c.Name + ".mol2");
                                    var charges = c.Result.Charges;
                                    s.Structure.WriteMol2(context.TextWriter, remark: c.Name, chargeSelector: a =>
                                    {
                                        ChargeValue value;
                                        if (charges.TryGetValue(a, out value)) return value.Charge;
                                        return 0.0;
                                    });
                                    context.TextWriter.Flush();
                                    context.EndEntry();

                                    context.BeginEntry(folder + c.Name + AtomPropertiesEx.DefaultExtension);
                                    var props = s.Structure.GetAtomProperties(c.Result.Parameters.Id);
                                    props.Write(context.TextWriter);
                                    context.TextWriter.Flush();
                                    context.EndEntry();
                                }
                                
                                progress.UpdateProgress(i);
                            }
                        }));
                    }

                    LogService.Default.Message("Data exported.");
                }
                catch (Exception e)
                {
                    LogService.Default.Error("Export", e.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }

        public ResultViewModel(Session session)
        {
            this.Results = new ObservableCollection<StructureCharges>();
            this.ResultsView = new PagedCollectionView(Results);
        }
    }
}
