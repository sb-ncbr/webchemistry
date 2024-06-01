using GalaSoft.MvvmLight.Command;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.SiteBinder.Core;
using WebChemistry.SiteBinder.Silverlight.ViewModel;
using WebChemistry.SiteBinder.Silverlight.Visuals;

namespace WebChemistry.SiteBinder.Silverlight.DataModel
{
    public class Session : SessionBase<StructureWrap>
    {
        private MatchMethod _matchMethod;
        public MatchMethod MatchMethod
        {
            get
            {
                return _matchMethod;
            }

            set
            {
                if (_matchMethod == value) return;

                _matchMethod = value;
                NotifyPropertyChanged("MatchMethod");
            }
        }

        private PivotType _pivotType;
        public PivotType PivotType
        {
            get
            {
                return _pivotType;
            }

            set
            {
                if (_pivotType == value) return;

                _pivotType = value;
                NotifyPropertyChanged("PivotType");
            }
        }

        private bool _ignoreHydrogens;
        public bool IgnoreHydrogens
        {
            get
            {
                return _ignoreHydrogens;
            }

            set
            {
                if (_ignoreHydrogens == value) return;

                Structures.ForEach(s => s.MarkDirty());
                _ignoreHydrogens = value;
                NotifyPropertyChanged("IgnoreHydrogens");
            }
        }

        private bool _findPairwiseMatrix;
        public bool FindPairwiseMatrix
        {
            get
            {
                return _findPairwiseMatrix;
            }

            set
            {
                if (_findPairwiseMatrix == value) return;

                _findPairwiseMatrix = value;
                NotifyPropertyChanged("FindPairwiseMatrix");
            }
        }

        private bool _useSpecificPivot;
        public bool UseSpecificPivot
        {
            get
            {
                return _useSpecificPivot;
            }

            set
            {
                if (_useSpecificPivot == value) return;

                if (value) PivotType = Core.PivotType.SpecificStructure;
                _useSpecificPivot = value;
                NotifyPropertyChanged("UseSpecificPivot");
            }
        }

        private string _specificPivotText;
        public string SpecificPivotText
        {
            get
            {
                return _specificPivotText;
            }

            set
            {
                if (_specificPivotText == value) return;

                _specificPivotText = value;
                NotifyPropertyChanged("SpecificPivotText");
            }
        }

        private StructureWrap _specificPivot;
        public StructureWrap SpecificPivot
        {
            get
            {
                return _specificPivot;
            }

            set
            {
                if (_specificPivot == value) return;

                if (UseSpecificPivot) PivotType = Core.PivotType.SpecificStructure;
                _specificPivot = value;
                NotifyPropertyChanged("SpecificPivot");
            }
        }


        string[] structureFilterTypes = new string[] { "Show All", "Show Selected", "Show Unselected" };
        public string[] StructureFilterTypes { get { return structureFilterTypes; } }

        private int _structureFilterIndex;
        public int StructureFilterIndex
        {
            get
            {
                return _structureFilterIndex;
            }

            set
            {
                if (_structureFilterIndex == value) return;

                _structureFilterIndex = value;
                UpdateStructuresFilter();
                NotifyPropertyChanged("StructureFilterIndex");
            }
        }

        public void UpdateStructuresFilter()
        {
            using (StructuresView.DeferRefresh())
            {
                this.StructuresView.Filter = null;
                switch (StructureFilterIndex)
                {
                    case 1:
                        this.StructuresView.Filter = new Predicate<object>(o => (o as StructureWrap).IsSelected);
                        break;
                    case 2:
                        this.StructuresView.Filter = new Predicate<object>(o => !(o as StructureWrap).IsSelected);
                        break;
                    default:
                        break;
                }
            }
        }

        string[] structureListGroupingTypes = new string[] 
        { 
            "Group by Residues",  // 0
            "Group by Atom Count", // 1
            "Group by Descriptor",  // 2
            "Group by Selection",  // 3
            "Sort by Name", // 4
            "Sort by Descriptor (asc)", // 5
            "Sort by Descriptor (desc)" // 6
        }; 
        public string[] StructureListGroupingTypes { get { return structureListGroupingTypes; } }
        bool IsDescriptorGrouping()
        {
            return GroupingTypeIndex == 2 || GroupingTypeIndex == 5 || GroupingTypeIndex == 6;
        }

        bool IsSelectionGrouping()
        {
            return GroupingTypeIndex == 3;
        }

        public override void UpdateStructuresView()
        {
            using (StructuresView.DeferRefresh())
            {
                this.StructuresView.SortDescriptions.Clear();
                this.StructuresView.GroupDescriptions.Clear();                
                switch (GroupingTypeIndex)
                {
                    case 0:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("ResidueCount", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("ResidueString", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        this.StructuresView.GroupDescriptions.Add(new PropertyGroupDescription("ResidueString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 1:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("AtomCount", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        this.StructuresView.GroupDescriptions.Add(new PropertyGroupDescription("AtomCountString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 2:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("CurrentDescriptorValue", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        this.StructuresView.GroupDescriptions.Add(new PropertyGroupDescription("CurrentDescriptorValue") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 3:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("ResidueCount", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("ResidueString", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        this.StructuresView.GroupDescriptions.Add(new PropertyGroupDescription("SelectionString") { StringComparison = StringComparison.OrdinalIgnoreCase });
                        break;
                    case 4:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        break;
                    case 5:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("CurrentDescriptorValue", ListSortDirection.Ascending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        break;
                    case 6:
                        this.StructuresView.SortDescriptions.Add(new SortDescription("CurrentDescriptorValue", ListSortDirection.Descending));
                        this.StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
                        break;
                    default:
                        break;
                }
            }
        }
            
        public override void CurrentDescriptorUpdated()
        {
            if (IsDescriptorGrouping())
            {
                UpdateStructuresView();
            }

            ServiceLocator.Current.GetInstance<ResultViewModel>().CurrentDescriptorUpdated();
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
                UpdateStructuresView();
                NotifyPropertyChanged("GroupingTypeIndex");
            }
        }

        string[] selectionGroupingTypes = new string[] { "by Residue", "by Atom Name", "by Atom Type" };
        public string[] SelectionGroupingTypes { get { return selectionGroupingTypes; } }

        private int _selectionTypeIndex;
        public int SelectionTypeIndex
        {
            get
            {
                return _selectionTypeIndex;
            }

            set
            {
                if (_selectionTypeIndex == value) return;

                _selectionTypeIndex = value;
                UpdateSelectionInfo();
                NotifyPropertyChanged("SelectionTypeIndex");
            }
        }

        private SelectionInfo _selectionInfo = SelectionInfo.Empty;
        public SelectionInfo SelectionInfo
        {
            get
            {
                return _selectionInfo;
            }

            set
            {
                if (_selectionInfo == value) return;

                _selectionInfo = value;
                NotifyPropertyChanged("SelectionInfo");
            }
        }

        static string[] clusterCountList = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };

        public string[] ClusterCountList { get { return clusterCountList; } }


        private int _clusterCountIndex;
        public int ClusterCountIndex
        {
            get
            {
                return _clusterCountIndex;
            }

            set
            {
                if (_clusterCountIndex == value) return;

                _clusterCountIndex = value;
                NotifyPropertyChanged("ClusterCountIndex");
            }
        }

        void UpdateSelectionInfo()
        {
            SelectionInfo = SelectionInfo.Create(SelectedStructures, 
                this,
                SelectionTypeIndex == 0 ? 
                    SelectionInfoType.ResidueName 
                    : SelectionTypeIndex == 1 ? SelectionInfoType.AtomName : SelectionInfoType.AtomType);
        }

        public MultiMotiveVisual3D Visual { get; private set; }


        public QuerySelectionViewModel QuerySelection { get; private set; }
        public StructureSelectionViewModel StructureSelection { get; private set; }

        HashSet<StructureWrap> selectedStructuresSet = new HashSet<StructureWrap>();
        
        //public ObservableCollection<StructureWrap> SelectedStructures { get; private set; }

        private List<StructureWrap> _selectedStructures;
        public List<StructureWrap> SelectedStructures
        {
            get
            {
                return _selectedStructures;
            }

            set
            {
                if (_selectedStructures == value) return;

                _selectedStructures = value;
                NotifyPropertyChanged("SelectedStructures");
            }
        }


        private ICommand _superimposeCommand;
        public ICommand SuperimposeCommand
        {
            get
            {
                _superimposeCommand = _superimposeCommand ?? new RelayCommand(() => Superimpose());
                return _superimposeCommand;
            }
        }

        bool CheckCanSuperimpose()
        {
            if (SelectedStructures.Count < 2)
            {
                Log.Info("Cannot superimpose: Select at least 2 structures.");
                return false;
            }

            if (SelectedStructures[0].SelectedCount == 0)
            {
                Log.Info("Cannot superimpose: Select some atoms.");
                return false;
            }

            if (MatchMethod == Core.MatchMethod.Combinatorial)
            {
                if (SelectedStructures.Select(s => s.SelectionString).Distinct().Count() != 1)
                {
                    Log.Info("Cannot superimpose: The combinatorial method requires that you select the same atoms on every structure. Using None/All above the selection tree functions might help.");
                    return false;
                }
            }
            else
            {
                var notConnected = SelectedStructures.Where(s => !s.MatchGraph.IsConnected()).ToArray();

                if (notConnected.Length > 0)
                {
                    Log.Info("Cannot superimpose: {0} structures ({1}) are not connected. Select more atoms manually, or use the Expand (+) or Connect(@) functions.", 
                        notConnected.Length,
                        string.Join(", ", notConnected.Select(s => s.Structure.Id)));
                    return false;
                }
            }

            return true;
        }

        public async void Superimpose()
        {
            var cs = ComputationService.Default;

            if (!CheckCanSuperimpose()) return;

            var progress = cs.Start();

            try
            {
                progress.UpdateStatus("Initializing...");
                await TaskEx.Delay(TimeSpan.FromSeconds(0.05));
                //SelectedStructures.ForEach(s => s.Structure.ToCentroidCoordinates());

                if (UseSpecificPivot && SelectedStructures.Count > 0)
                {
                    if (!StructureMap.ContainsKey(SpecificPivotText))
                    {
                        Log.Error("Specific Pivot", "There is no structure called '{0}'. Using '{1}' as the pivot.", SpecificPivotText, SelectedStructures[0].Structure.Id);
                    }
                }

                var result = await TaskEx.Run(() =>
                    {
                        var graphs = SelectedStructures.Select(s => s.MatchGraph).ToArray();
                        var pivotIndex = UseSpecificPivot ? SelectedStructures.IndexOf(SpecificPivot) : -1;
                        var res = MultipleMatching<IAtom>.Find(graphs, pivotType: PivotType, pairMethod: MatchMethod,
                                    pivotIndex: pivotIndex, progress: progress, pairwiseMatrix: FindPairwiseMatrix, kClusters: ClusterCountIndex + 1);
                        var transforms = res.GetTransformation(SelectedStructures.Select(s => s.Structure));
                        return new { Matchings = res, Transforms = transforms };
                    });

                progress.UpdateStatus("Applying transformations (this might take up to several seconds)...");
                progress.UpdateCanCancel(false);
                progress.UpdateIsIndeterminate(true);
                await TaskEx.Delay(TimeSpan.FromSeconds(0.05));

                //StructureMap[result.Matchings.Pivot.Token].Structure.ToCentroidCoordinates();
                SelectedStructures.Where(s => result.Matchings.Statistics.MatchedCounts[s.Structure.Id] > 0).ForEach(s => result.Transforms[s.Structure.Id].Apply(s.Structure));
                var resultVM = ServiceLocator.Current.GetInstance<ResultViewModel>();
                resultVM.UpdateResult(result.Matchings, cs.Elapsed);

                Log.Message("{0} structures superimposed in {1}. RMSD = {2}, σ = {3}.", SelectedStructures.Count, ComputationService.GetElapasedTimeString(cs.Elapsed),
                    result.Matchings.Statistics.AverageRmsd.ToStringInvariant("0.00"), result.Matchings.Statistics.Sigma.ToStringInvariant("0.00"));

                ServiceLocator.Current.GetInstance<MainPage>().GoToResultTab();
                Visual.Viewport.Render();
            }
            catch (ComputationCancelledException)
            {
                Log.Info("Aborted.");
            }
            catch (Exception e)
            {
                Log.Error("Superimposing", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        public void SelectTopLevelAtoms(string name, List<string> bottomNames, SelectionInfoType type, bool value)
        {
            SelectedStructures.ForEach(s => s.SelectTopLevelAtoms(name, bottomNames, value, type));
            UpdateSelectionGrouping();
        }

        public void SelectBottomLevelAtoms(string topName, string bottomName, SelectionInfoType type, bool value)
        {
            SelectedStructures.ForEach(s => s.SelectBottomLevelAtoms(topName, bottomName, value, type));
            UpdateSelectionGrouping();
        }

        public void SelectAllAtomsOnSelectedStructures(bool selected, bool useSelectionInfo)
        {
            if (useSelectionInfo)
            {
                if (selected == false)
                {
                    SelectedStructures.ForEach(s => s.SelectMany(a => true, false));
                    if (SelectionInfo is SingleSelectionInfo)
                    {
                        (SelectionInfo as SingleSelectionInfo).Groups.ForEach(g => g.IsSelected = false);
                    }
                    else if (SelectionInfo is MultipleSelectionInfo)
                    {
                        (SelectionInfo as MultipleSelectionInfo).Groups.ForEach(g => g.IsSelected = false);
                    }
                }
                else
                {
                    if (SelectionInfo is SingleSelectionInfo)
                    {
                        (SelectionInfo as SingleSelectionInfo).Groups.ForEach(g => g.IsSelected = selected);
                    }
                    else if (SelectionInfo is MultipleSelectionInfo)
                    {
                        (SelectionInfo as MultipleSelectionInfo).Groups.ForEach(g => g.IsSelected = selected);
                    }
                }

                SelectionInfo.Update();
            }
            else
            {
                SelectedStructures.ForEach(s => s.SelectMany(a => true, selected));
                SelectionInfo.Update();
            }

            UpdateSelectionGrouping();
        }

        public void UpdateSelectionGrouping()
        {
            if (IsSelectionGrouping())
            {
                UpdateStructuresView();
            }
        }

        public void ExpandAtomSelectionOnSelectedStructures()
        {
            foreach (var s in SelectedStructures)
            {
                s.SelectMany(s.Structure.Atoms
                    .Where(a => a.IsSelected)
                    .ToArray()
                    .SelectMany(a => s.Structure.Bonds[a].Select(b => b.B))
                    .ToArray(), true);
            }

            SelectionInfo.Update();
        }

        public void InvertAtomSelection()
        {
            foreach (var s in SelectedStructures)
            {
                var sel = s.Structure.Atoms.Where(a => a.IsSelected).ToHashSet();
                s.SelectAtoms(s.Structure.Atoms, false);
                s.SelectMany(a => !sel.Contains(a), true);
            }

            SelectionInfo.Update();
        }

        public async void MakeConnected()
        {
            var cs = ServiceLocator.Current.GetInstance<ComputationService>();

            var progress = cs.Start();

            try
            {
                progress.UpdateStatus("Computing...");
                progress.UpdateProgress(0, SelectedStructures.Count);

                var atoms = await TaskEx.Run(() => SelectedStructures.Select((s, i) =>
                    {
                        progress.ThrowIfCancellationRequested();
                        progress.UpdateProgress(i);
                        return ConnectorHelper.GetConnectedSelection(s.Structure);
                    }).ToList());

                progress.UpdateCanCancel(false);
                progress.UpdateStatus("Done.");
                progress.UpdateLength(SelectedStructures.Count);

                var notConnectable = new List<StructureWrap>();

                for (int i = 0; i < atoms.Count; i++)
                {
                    var sel = atoms[i];
                    if (sel == null)
                    {
                        notConnectable.Add(SelectedStructures[i]);
                        continue;
                    }
                    SelectedStructures[i].SelectMany(a => sel.Contains(a), true);
                }

                if (notConnectable.Count > 0)
                {
                    Unselect(notConnectable);
                    Log.Info("Structures {0} could not be connected (possible reason is bad conformation of the structure, i.e. O atom being too far to create a bond with C) and were automatically unselected.", string.Join(", ", notConnectable.Select(s => "'" + s.Structure.Id + "'")));
                }

                if (notConnectable.Count > 0)
                {
                    Log.Message("{0} structures were connected and {1} structures were unselected.", SelectedStructures.Count, notConnectable.Count);
                }
                else
                {
                    Log.Message("{0} structures were connected.", SelectedStructures.Count);
                }
            }
            catch (ComputationCancelledException)
            {
                Log.Aborted();
            }
            catch (Exception e)
            {
                Log.Error("Make Connected", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        public void Select(IEnumerable<StructureWrap> structures)
        {
            ignoreStructurePropertyChanged = true;

            var news = structures.Where(s => !selectedStructuresSet.Contains(s)).ToArray();

            if (news.Length > 0)
            {
                news.ForEach(s =>
                    {
                        s.Structure.IsSelected = true;
                        //SelectedStructures.Add(s);
                        selectedStructuresSet.Add(s);
                    });

                SelectedStructures = SelectedStructures.Concat(news).ToList();
                if (SelectedStructures.Count > 0 && SpecificPivot == null) SpecificPivot = SelectedStructures[0];
                else if (SelectedStructures.Count == 0) SpecificPivot = null;
                else
                {
                    var oldPivot = SpecificPivot;
                    SpecificPivot = null;
                    SpecificPivot = oldPivot;
                }
                
                Visual.Add(news);
                UpdateSelectionInfo();
                UpdateStructuresFilter();
            }

            ignoreStructurePropertyChanged = false;
        }
        
        public void Unselect(IEnumerable<StructureWrap> structures)
        {
            ignoreStructurePropertyChanged = true;

            var toRemove = structures.Where(s => selectedStructuresSet.Contains(s)).ToArray();

            if (toRemove.Length > 0)
            {   
                toRemove.ForEach(s =>
                {
                    s.Structure.IsSelected = false;
                    //SelectedStructures.Remove(s);
                    selectedStructuresSet.Remove(s);
                });

                SelectedStructures = SelectedStructures.Where(s => selectedStructuresSet.Contains(s)).ToList();
                if (!selectedStructuresSet.Contains(SpecificPivot)) SpecificPivot = SelectedStructures.Count > 0 ? SelectedStructures[0] : null;
                else
                {
                    var oldPivot = SpecificPivot;
                    SpecificPivot = null;
                    SpecificPivot = oldPivot;
                }

                Visual.Remove(toRemove, SelectedStructures);

                UpdateSelectionInfo();
                UpdateStructuresFilter();
            }

            ignoreStructurePropertyChanged = false;
        }

        protected override void OnRemove(StructureWrap structure)
        {
            var rvm = ServiceLocator.Current.GetInstance<ResultViewModel>();
            structure.Structure.PropertyChanged -= StructurePropertyChanged;
            structure.ClearVisual();
            rvm.RemoveEntries(new HashSet<StructureWrap> { structure });
        }

        protected override void OnRemoveMany(IList<StructureWrap> structures)
        {
            var rvm = ServiceLocator.Current.GetInstance<ResultViewModel>();

            StructuresView = null;
            var toRemove = structures.ToHashSet();

            structures.ForEach(s =>
            {
                s.Structure.PropertyChanged -= StructurePropertyChanged;
                s.ClearVisual();
            });

            rvm.RemoveEntries(toRemove);
        }

        public void RemoveSelected()
        {
            var sel = SelectedStructures.ToArray();
            Unselect(SelectedStructures);
            RemoveMany(sel);
        }

        protected override void OnClear(IList<StructureWrap> structures)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            Select(Structures);
            RemoveSelected();
        }

        bool ignoreStructurePropertyChanged = false;

        void StructurePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ignoreStructurePropertyChanged) return;

            if (e.PropertyName.Equals("IsSelected", StringComparison.Ordinal))
            {
                var s = sender as IStructure;
                if (s.IsSelected) Select(StructureMap[s.Id].ToSingletonArray());
                else Unselect(StructureMap[s.Id].ToSingletonArray());
            }
        }

        public string SetUseRingHeuristic(bool value)
        {
            WebChemistry.SiteBinder.Core.MatchGraph.UseRingHeuristic = value;
            if (value) return "Ring heuristic will be used.";
            return "Ring heuristic will not be used.";
        }

        protected override void OnLoad(List<StructureWrap> newStructures)
        {
            foreach (var s in newStructures)
            {
                s.Structure.SelectAllAtoms();
                s.Structure.PropertyChanged += StructurePropertyChanged;
                s.UpdateSession(this);
                s.Color = Pallete.GetRandomColor();
            }

            Select(newStructures);
        }
        
        protected override void LoadStateFromXml(XElement root)
        {
            ImportSelection(root.Element("Selection"));
            //should be loaded last (coz of pivot)
            ImportSettings(root.Element("Settings"));
        }

        XElement ExportSelection()
        {
            var ret = new XElement("Selection");
            foreach (var s in Structures)
            {
                ret.Add(new XElement("Structure",
                    new XAttribute("Id", s.Structure.Id),
                    new XAttribute("Color", new ElementColor(s.Color.R, s.Color.G, s.Color.B)),
                    new XAttribute("IsSelected", s.Structure.IsSelected ? 1 : 0),
                    new XAttribute("AtomSelection", string.Join(" ", s.Structure.Atoms.Where(a => a.IsSelected).Select(a => a.Id)))));
            }

            return ret;
        }
                
        void ImportSelection(XElement element)
        {
            Unselect(Structures);

            var toSelect = new List<StructureWrap>();
            var split = " ".ToCharArray();

            foreach (var s in element.Elements("Structure"))
            {
                var id = s.Attribute("Id").Value;
                
                StructureWrap w;
                if (StructureMap.TryGetValue(id, out w))
                {
                    w.Structure.SelectAllAtoms(false);

                    if (s.Attribute("IsSelected").Value == "1") toSelect.Add(w);

                    var atomSelection = s.Attribute("AtomSelection").Value.Split(split, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToHashSet();
                    w.SelectMany(a => atomSelection.Contains(a.Id), true);

                    var c = ElementColor.Parse(s.Attribute("Color").Value);
                    w.Color = System.Windows.Media.Color.FromArgb(255, c.R, c.G, c.B);
                }
            }

            Select(toSelect);
        }

        XElement ExportSettings()
        {
            //ultipleMatching<IAtom>.Find(graphs, pivotType: PivotType, pairMethod: MatchMethod,
            //                        pivotIndex: pivotIndex, progress: progress, pairwiseMatrix: FindPairwiseMatrix, kClusters: ClusterCountIndex + 1);

            return new XElement("Settings",
                new XAttribute("PivotType", PivotType),
                new XAttribute("MatchMethod", MatchMethod),
                new XAttribute("UseSpecificPivot", UseSpecificPivot),
                new XAttribute("PivotId", SpecificPivot.Structure.Id),
                new XAttribute("FindPairwiseMatrix", FindPairwiseMatrix),
                new XAttribute("IgnoreHydrogens", IgnoreHydrogens),
                new XAttribute("ClusterCountIndex", ClusterCountIndex));
        }

        void ImportSettings(XElement element)
        {
            this.PivotType = (PivotType)Enum.Parse(typeof(PivotType), element.Attribute("PivotType").Value, false);
            this.MatchMethod = (MatchMethod)Enum.Parse(typeof(MatchMethod), element.Attribute("MatchMethod").Value, false);
            this.UseSpecificPivot = bool.Parse(element.Attribute("UseSpecificPivot").Value);
            this.SpecificPivot = StructureMap[element.Attribute("PivotId").Value];
            this.FindPairwiseMatrix = bool.Parse(element.Attribute("FindPairwiseMatrix").Value);
            this.ClusterCountIndex = int.Parse(element.Attribute("ClusterCountIndex").Value);
            var ignoreH = element.Attribute("IgnoreHydrogens");
            if (ignoreH != null) this.IgnoreHydrogens = bool.Parse(ignoreH.Value);
            else IgnoreHydrogens = true;
        }

        protected override XElement SaveStateToXml()
        {
            return new XElement(new XElement("Session", ExportSettings(), ExportSelection()));
        }

        protected override void ExportToZip(ZipOutputStream zip, ComputationProgress progress)
        {
            throw new NotImplementedException();
        }

        public Session()
            : base("SiteBinder", ".sws")
        {            
            //this.SelectedStructures = new ObservableCollection<StructureWrap>();

            this.QuerySelection = new QuerySelectionViewModel(this);
            this.StructureSelection = new StructureSelectionViewModel(this);

            this.SelectedStructures = new List<StructureWrap>();

            //this.PivotListView = new PagedCollectionView(this.SelectedStructures);
            //this.PivotListView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));

            this.IgnoreHydrogens = true;
            this.PivotType = Core.PivotType.Average;
            this.MatchMethod = Core.MatchMethod.Subgraph;

            this.Visual = new MultiMotiveVisual3D();
        }
    }
}
