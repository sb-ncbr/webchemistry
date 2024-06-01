using GalaSoft.MvvmLight.Command;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class CorrelationViewModel : ObservableObject
    {
        [Ninject.Inject]
        public Session Session { get; set; }

        [Ninject.Inject]
        public AnalyzeViewModel AnalyzeViewModel { get; set; }

        private ICommand _correlateCommand;
        public ICommand CorrelateCommand
        {
            get
            {
                _correlateCommand = _correlateCommand ?? new RelayCommand(() => Correlate());
                return _correlateCommand;
            }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                _exportCommand = _exportCommand ?? new RelayCommand(() => ExportAll());
                return _exportCommand;
            }
        }
        
        private ICommand _copyToClipboardCommand;
        public ICommand CopyToClipboardCommand
        {
            get
            {
                _copyToClipboardCommand = _copyToClipboardCommand ?? new RelayCommand(() => CopyToClipboard());
                return _copyToClipboardCommand;
            }
        }

        private bool _selectionOnly;
        public bool SelectionOnly
        {
            get
            {
                return _selectionOnly;
            }

            set
            {
                if (_selectionOnly == value) return;

                _selectionOnly = value;
                NotifyPropertyChanged("SelectionOnly");
            }
        }
        
        private Correlation _currentCorrelation;
        public Correlation CurrentCorrelation
        {
            get
            {
                return _currentCorrelation;
            }

            set
            {
                if (_currentCorrelation == value) return;

                _currentCorrelation = value;
                NotifyPropertyChanged("CurrentCorrelation");
            }
        }

        private PagedCollectionView _correlationsView;
        public PagedCollectionView CorrelationsView
        {
            get
            {
                return _correlationsView;
            }

            set
            {
                if (_correlationsView == value) return;

                _correlationsView = value;
                NotifyPropertyChanged("CorrelationsView");
            }
        }

        private int _orderTypeIndex = 0;
        public int OrderTypeIndex
        {
            get
            {
                return _orderTypeIndex;
            }

            set
            {
                if (_orderTypeIndex == value) return;

                _orderTypeIndex = value;
                UpdateView();
                NotifyPropertyChanged("OrderTypeIndex");
            }
        }
        
        public void SyncView()
        {
            var CurrentStructure = AnalyzeViewModel.CurrentStructure;
            var CurrentPartition = AnalyzeViewModel.CurrentPartition;

            if (CurrentStructure == null 
                || CurrentPartition == null)
            {
                CorrelationsView = null;
                CurrentCorrelation = null;
                return;
            }
            
            var source = CurrentStructure.Correlations[CurrentPartition];

            CorrelationsView = new PagedCollectionView(source);
            UpdateView();
            if (CurrentCorrelation != null)
            {
                var corr = source.FirstOrDefault(c =>
                    c.DependentName == CurrentCorrelation.DependentName
                    && c.IndependentName == CurrentCorrelation.IndependentName);

                CurrentCorrelation = corr;
            }
            else if (CorrelationsView.Count > 0)
            {
                CurrentCorrelation = CorrelationsView[0] as Correlation;
            }
        }
        
        string[] orderTypes = new string[] { "Sort by Pearson Correlation", "Sort by Spearman Correlation", "Sort by RMSD", "Sort by Absolute Charge Difference" };
        public string[] OrderTypes { get { return orderTypes; } }

        public void UpdateView()
        {
            if (CorrelationsView == null) return;
            using (CorrelationsView.DeferRefresh())
            {
                this.CorrelationsView.SortDescriptions.Clear();
                this.CorrelationsView.GroupDescriptions.Clear();
                this.CorrelationsView.SortDescriptions.Add(new SortDescription("IndependentName", ListSortDirection.Ascending));
                this.CorrelationsView.SortDescriptions.Add(new SortDescription("DependentName", ListSortDirection.Ascending));
                this.CorrelationsView.SortDescriptions.Add(new SortDescription("DependentMethodId", ListSortDirection.Ascending));
                this.CorrelationsView.GroupDescriptions.Add(new PropertyGroupDescription("IndependentName") { StringComparison = StringComparison.Ordinal });
                switch (OrderTypeIndex)
                {
                    case 0:
                        this.CorrelationsView.SortDescriptions.Add(new SortDescription("PearsonCoefficient", ListSortDirection.Descending));
                        break;
                    case 1:
                        this.CorrelationsView.SortDescriptions.Add(new SortDescription("SpearmanCoefficient", ListSortDirection.Descending));
                        break;
                    case 2:
                        this.CorrelationsView.SortDescriptions.Add(new SortDescription("Rmsd", ListSortDirection.Ascending));
                        break;
                    case 3:
                        this.CorrelationsView.SortDescriptions.Add(new SortDescription("AbsoluteDifferenceSum", ListSortDirection.Ascending));
                        break;
                    default:
                        break;
                }
            }
        }

        ListExporter GetExporter(StructureWrap structure, string separator, string partition)
        {
            var source = structure.Correlations[partition];

            var xs = source
                .OrderBy(c => c.IndependentName)
                .ThenBy(c => c.DependentName)
                .ThenBy(c => c.DependentMethodId)
                .ToList();

            return xs.GetExporter(separator)
                .AddExportableColumn(x => x.IndependentName, ColumnType.String, "Pivot")
                .AddExportableColumn(x => x.DependentName, ColumnType.String, "Other")
                .AddExportableColumn(x => x.FormattedPearsonCoefficient, ColumnType.Number, "Pearson")
                .AddExportableColumn(x => x.FormattedSpearmanCoefficient, ColumnType.Number, "Spearman")
                .AddExportableColumn(x => x.FormattedRmsd, ColumnType.Number, "RMSD")
                .AddExportableColumn(x => x.FormattedAbsoluteDifferenceSum, ColumnType.Number, "DiffSum")
                .AddExportableColumn(x => x.DataPoints.Count, ColumnType.Number, "DataPoints");
        }

        void CopyToClipboard()
        {
            try
            {
                var CurrentStructure = AnalyzeViewModel.CurrentStructure;
                var CurrentPartition = AnalyzeViewModel.CurrentPartition;

                if (CurrentStructure == null || CurrentStructure.Correlations[CurrentPartition].Length == 0 || CorrelationsView == null)
                {
                    LogService.Default.Info("Nothing to export.");
                    return;
                }
                
                var csv = GetExporter(CurrentStructure, "\t", CurrentPartition).ToCsvString();

                Clipboard.SetText(csv);
                LogService.Default.Message("Data copied to clipboard.");
            }
            catch (Exception e)
            {
                LogService.Default.Error("Clipboard Copy", e.Message);
            }
        }

        async void ExportAll()
        {
            if (Session.Structures.Count == 0)
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
                progress.Update(statusText: "Exporting...", canCancel: false, isIndeterminate: true);
                try
                {
                    using (var f = sfd.OpenFile())
                    {
                        await ZipUtils.CreateZip(f, context => TaskEx.Run(() =>
                        {
                            foreach (var s in Session.Structures)
                            {
                                var folder = s.Structure.Id + "\\";

                                foreach (var p in Session.ComputedPartitions)
                                {
                                    var exporter = GetExporter(s, ",", p);
                                    context.BeginEntry(string.Format("{0}correlations_{1}.csv", folder, p));
                                    exporter.WriteCsvString(context.TextWriter);
                                    context.TextWriter.Flush();
                                    context.EndEntry();
                                }
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

        void Correlate()
        {
            Session.Correlate();
        }
    }
}
