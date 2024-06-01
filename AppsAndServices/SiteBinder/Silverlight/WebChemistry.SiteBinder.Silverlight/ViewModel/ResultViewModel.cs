using System;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WebChemistry.Framework.Core;
using WebChemistry.SiteBinder.Core;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Practices.ServiceLocation;
using System.Threading.Tasks;
using System.Windows.Controls;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml.Linq;
using WebChemistry.Silverlight.Common.Services;
using System.IO;
using System.Text;
using WebChemistry.Silverlight.Common;

namespace WebChemistry.SiteBinder.Silverlight.ViewModel
{
    public class ResultViewModel : ViewModelBase
    {
        private ICommand _selectGroupCommand;
        public ICommand SelectGroupCommand
        {
            get
            {
                _selectGroupCommand = _selectGroupCommand ?? new RelayCommand<System.Windows.Data.CollectionViewGroup>(g => Session.Select(g.Items.Select(x => (x as Result.Entry).Structure)));
                return _selectGroupCommand;
            }
        }

        private ICommand _unselectGroupCommand;
        public ICommand UnselectGroupCommand
        {
            get
            {
                _unselectGroupCommand = _unselectGroupCommand ?? new RelayCommand<System.Windows.Data.CollectionViewGroup>(g => Session.Unselect(g.Items.Select(x => (x as Result.Entry).Structure)));
                return _unselectGroupCommand;
            }
        }

        private ICommand _copyToClipboardCommand;
        public ICommand CopyToClipboardCommand
        {
            get
            {
                _copyToClipboardCommand = _copyToClipboardCommand ?? new RelayCommand<string>(what => CopyToClipboard(what));
                return _copyToClipboardCommand;
            }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                _exportCommand = _exportCommand ?? new RelayCommand<string>(what => Export(what));
                return _exportCommand;
            }
        }

        private ICommand _selectAllStructuresCommand;
        public ICommand SelectAllStructuresCommand
        {
            get
            {
                _selectAllStructuresCommand = _selectAllStructuresCommand ?? new RelayCommand(() =>
                {
                    if (Result != null)
                    {
                        Session.Unselect(Session.SelectedStructures);
                        Session.Select(Result.Structures.Select(e => e.Structure));
                    }
                });
                return _selectAllStructuresCommand;
            }
        }
        
        private Color? _selectedColor;
        public Color? SelectedColor
        {
            get
            {
                return _selectedColor;
            }

            set
            {
                if (value == _selectedColor) return;

                _selectedColor = value;
                RaisePropertyChanged("SelectedColor");
            }
        }

        string[] resultGroupingTypes = new string[] { "Group by Sigma Group", "Group by Cluster Index", "Group by Matched Atom Count", "Group by Descriptor",            
            "Sort by Name", "Sort by Descriptor (asc)", "Sort by Descriptor (desc)" };
        public string[] ResultGroupingTypes { get { return resultGroupingTypes; } }

        void UpdateGrouping()
        {
            if (this.ResultView == null) return;

            using (ResultView.DeferRefresh())
            {
                this.ResultView.GroupDescriptions.Clear();
                this.ResultView.SortDescriptions.Clear();
                switch (GroupingTypeIndex)
                {
                    case 0:
                        this.ResultView.SortDescriptions.Add(new SortDescription("SigmaGroup", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Rmsd", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        this.ResultView.GroupDescriptions.Add(new PropertyGroupDescription("SigmaGroupString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 1:
                        this.ResultView.SortDescriptions.Add(new SortDescription("ClusterGroup", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Rmsd", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        this.ResultView.GroupDescriptions.Add(new PropertyGroupDescription("ClusterGroupString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 2:
                        this.ResultView.SortDescriptions.Add(new SortDescription("MatchedCount", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Rmsd", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        this.ResultView.GroupDescriptions.Add(new PropertyGroupDescription("MatchedCountString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 3:
                        this.ResultView.SortDescriptions.Add(new SortDescription("Structure.CurrentDescriptorValue", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Rmsd", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        this.ResultView.GroupDescriptions.Add(new PropertyGroupDescription("Structure.CurrentDescriptorValue") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 4:
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        break;
                    case 5:
                        this.ResultView.SortDescriptions.Add(new SortDescription("Structure.CurrentDescriptorValue", ListSortDirection.Ascending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        break;
                    case 6:
                        this.ResultView.SortDescriptions.Add(new SortDescription("Structure.CurrentDescriptorValue", ListSortDirection.Descending));
                        this.ResultView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
                        break;
                    default:
                        break;
                }
            }
        }

        public void CurrentDescriptorUpdated()
        {
            if (GroupingTypeIndex == 4 || GroupingTypeIndex == 5)
            {
                UpdateGrouping();
            }
        }

        private int _groupingTypeIndex = 0;
        public int GroupingTypeIndex
        {
            get
            {
                return _groupingTypeIndex;
            }

            set
            {
                if (_groupingTypeIndex == value) return;

                _groupingTypeIndex = value;
                UpdateGrouping();
                RaisePropertyChanged("GroupingTypeIndex");
            }
        }

        private IValueConverter _groupingConverter;
        public IValueConverter GroupingConverter
        {
            get
            {
                return _groupingConverter;
            }

            set
            {
                if (_groupingConverter == value) return;

                _groupingConverter = value;
                RaisePropertyChanged("GroupingConverter");
            }
        }

        private Result _result;
        public Result Result
        {
            get
            {
                return _result;
            }

            private set
            {
                if (_result == value) return;

                _result = value;
                RaisePropertyChanged("Result");
            }
        }

        private PagedCollectionView _resultView;
        public PagedCollectionView ResultView
        {
            get
            {
                return _resultView;
            }

            private set
            {
                if (_resultView == value) return;

                _resultView = value;
                RaisePropertyChanged("ResultView");
            }
        }
        
        public Session Session { get; private set; }
        LogService log;

        public void ClearResult()
        {
            this.Result = null;
            this.ResultView = null;
        }

        public void RemoveEntries(HashSet<StructureWrap> toRemove)
        {
            if (Result == null) return;

            Result.Structures = new System.Collections.ObjectModel.ObservableCollection<Result.Entry>(Result.Structures.Where(e => !toRemove.Contains(e.Structure)));
            this.ResultView = new PagedCollectionView(this.Result.Structures);            
            UpdateGrouping();
        }

        public void UpdateResult(MultipleMatching<IAtom> result, TimeSpan timing)
        {
            this.Result = Result.Create(result, timing, Session);
            this.ResultView = new PagedCollectionView(this.Result.Structures);
            UpdateGrouping();
        }

        void CopyToClipboard(string what)
        {
            if (Result == null)
            {
                log.Info("Nothing to copy.");
                return;
            }

            try
            {

                switch (what.ToLower())
                {
                    case "list":
                        Clipboard.SetText(Result.ToCsvListString('\t'));
                        log.Message("CSV list copied to the clipboard.");
                        break;
                    case "matrix":
                        if (Result.Matching.PairwiseMatrix == null) log.Info("The pairwise matrix is not available.");
                        else
                        {                            
                            var s = Result.ToCsvMatrixString('\t');
                            Clipboard.SetText(s);
                            log.Message("CSV pairwise matrix copied to the clipboard.");
                        }
                        break;
                    ////case "xmlpairing":
                    ////    var e = Result.ExportPairing(Session);
                    ////    Clipboard.SetText(e.ToString());
                    ////    log.AddEntry("XML Pairing copied to the clipboard.");
                    ////    break;
                    case "csvpairingid":
                        {
                            var ret = Result.ExportPairingAsCsv(Session, '\t').Item1;
                            Clipboard.SetText(ret);
                            log.Message("CSV Pairing (Identifiers) copied to the clipboard.");
                            break;
                        }
                    case "csvpairingname":
                        {
                            var ret = Result.ExportPairingAsCsv(Session, '\t').Item2;
                            Clipboard.SetText(ret);
                            log.Message("CSV Pairing (Atom Names) copied to the clipboard.");
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                log.Error("Clipboard Copy", e.Message);
            }
        }


        static UTF8Encoding encoding = new UTF8Encoding();
        static void WriteEntryFromString(ZipOutputStream stream, string entryName, string data)
        {
            var entry = new ZipEntry(entryName);
            stream.PutNextEntry(entry);

            StreamWriter w = new StreamWriter(stream);
            w.Write(data);
            w.Flush();

            stream.CloseEntry();
        }

        async void Export(bool structures)
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
                    using (var stream = sfd.OpenFile())
                    using (var zip = new ZipOutputStream(stream))
                    {
                        await TaskEx.Run(() =>
                            {
                                WriteEntryFromString(zip, "list.csv", Result.ToCsvListString(','));
                                if (Result.Matching.PairwiseMatrix != null) WriteEntryFromString(zip, "pairwisematrix.csv", Result.ToCsvMatrixString(','));
                                //var data = Result.ExportPairing(Session);
                                //WriteEntryFromString(zip, "pairing.xml", data.ToString());
                                var pairing = Result.ExportPairingAsCsv(Session, ',');
                                WriteEntryFromString(zip, "pairing_ids.csv", Result.ExportPairingAsCsv(Session, ',').Item1);
                                WriteEntryFromString(zip, "pairing_names.csv", Result.ExportPairingAsCsv(Session, ',').Item2);

                                if (structures)
                                {
                                    ZipUtils.WriteStructuresToZipStream(zip, "superimposed", Result.Structures.Select(e => e.Structure), s => s.Structure.ToPdbString());
                                    Result.ExportPairedStructures(Session, zip);
                                    if (Result.AverageStructure != null)
                                    {
                                        WriteEntryFromString(zip, "average.pdb", Result.AverageStructure.ToPdbString());
                                    }
                                }
                            });
                    }
                    log.Message("Export successful.");
                }
                catch (Exception e)
                {
                    log.Error("Export", e.Message);
                }
                finally
                {
                    cs.End();
                }
            }
        }

        void Export(string what)
        {
            if (Result == null)
            {
                log.Info("Nothing to export.");
                return;
            }

            try
            {
                switch (what.ToLower())
                {
                    case "info":
                        Export(false);
                        break;
                    case "structures":
                        Export(true);
                        break;
                    case "both":
                        log.Message("TODO");
                        break;
                }
            }
            catch (Exception e)
            {
                log.Error("Export", e.Message);
            }
        }
        
        public ResultViewModel(Session Session, LogService log)
        {
            this.Session = Session;
            this.log = log;
        }
    }
}
