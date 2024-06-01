namespace WebChemistry.Charges.Silverlight.DataModel
{
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Practices.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Resources;
    using System.Xml;
    using System.Xml.Linq;
    using WebChemistry.Charges.Core;
    using WebChemistry.Charges.Silverlight.ViewModel;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.TypeSystem;
    using WebChemistry.Queries.Core;
    using WebChemistry.Queries.Core.MetaQueries;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Silverlight.Common.DataModel;
    using WebChemistry.Silverlight.Common.Services;

    public class Session : SessionBase<StructureWrap>
    {
        public ParameterSetManager SetManager { get; private set; }
        public PagedCollectionView ParameterSetsView { get; private set; }

        public ObservableCollection<ActiveSet> ActiveSets { get; private set; }
        public PagedCollectionView ActiveSetsView { get; private set; }

        private ICommand _addActiveSetsCommand;
        public ICommand AddActiveSetsCommand
        {
            get
            {
                _addActiveSetsCommand = _addActiveSetsCommand ?? new RelayCommand(() => AddActiveSets());
                return _addActiveSetsCommand;
            }
        }

        private ICommand _clearActiveSetsCommand;
        public ICommand ClearActiveSetsCommand
        {
            get
            {
                _clearActiveSetsCommand = _clearActiveSetsCommand ?? new RelayCommand(() => ActiveSets.Clear());
                return _clearActiveSetsCommand;
            }
        }

        private ICommand _clearStructuresCommand;
        public ICommand ClearStructuresCommand
        {
            get
            {
                _clearStructuresCommand = _clearStructuresCommand ?? new RelayCommand(() => RemoveMany(Structures));
                return _clearStructuresCommand;
            }
        }

        private ICommand _computeCommand;
        public ICommand ComputeCommand
        {
            get
            {
                _computeCommand = _computeCommand ?? new RelayCommand(() => Compute());
                return _computeCommand;
            }
        }

        public GlobalSelectionViewModel GlobalSelection { get; private set; }
               
        private EemParameterSet _currentSet;
        public EemParameterSet CurrentSet
        {
            get
            {
                return _currentSet;
            }

            set
            {
                if (_currentSet == value) return;

                _currentSet = value;
                NotifyPropertyChanged("CurrentSet");
            }
        }

        private ChargeComputationMethod _method = ChargeComputationMethod.EemCutoff;
        public ChargeComputationMethod Method
        {
            get
            {
                return _method;
            }

            set
            {
                if (_method == value) return;

                _method = value;
                NotifyPropertyChanged("Method");
            }
        }

        private double _cutoffRadius = 7.0;
        public double CutoffRadius
        {
            get
            {
                return _cutoffRadius;
            }

            set
            {
                if (_cutoffRadius == value) return;

                _cutoffRadius = value;
                NotifyPropertyChanged("CutoffRadius");
            }
        }

        private bool _ignoreWaters;
        public bool IgnoreWaters
        {
            get
            {
                return _ignoreWaters;
            }

            set
            {
                if (_ignoreWaters == value) return;

                _ignoreWaters = value;
                NotifyPropertyChanged("IgnoreWaters");
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

        private bool _correctCutoffTotalCharge;
        public bool CorrectCutoffTotalCharge
        {
            get
            {
                return _correctCutoffTotalCharge;
            }

            set
            {
                if (_correctCutoffTotalCharge == value) return;

                _correctCutoffTotalCharge = value;
                NotifyPropertyChanged("CorrectCutoffTotalCharge");
            }
        }

        public IList<PartitionDescriptor> PartitionDescriptors { get; private set; }

        private IList<string> _computedPartitions;
        public IList<string> ComputedPartitions
        {
            get
            {
                return _computedPartitions;
            }

            set
            {
                if (_computedPartitions == value) return;

                _computedPartitions = value;
                NotifyPropertyChanged("ComputedPartitions");
            }
        }

        /// <summary>
        /// Adds a partition descriptor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="topLevel"></param>
        /// <param name="bottomLevel"></param>
        /// <returns></returns>
        public string AddPartitionDescriptor(string name, QueryBuilderElement topLevel, QueryBuilderElement bottomLevel)
        {
            try
            {
                if (PartitionDescriptors.Any(d => d.Name.EqualOrdinalIgnoreCase(name)))
                {
                    return string.Format("A descriptor called '{0}' already exists.", name);
                }

                var top = topLevel.ToMetaQuery();
                var bottom = bottomLevel.ToMetaQuery();

                if (!TypeExpression.Unify(BasicTypes.PatternSeq, top.Type).Success) throw new Exception(string.Format("Wrong topLevel type: got {0}, expected {1}", top.Type, BasicTypes.PatternSeq.ToString()));
                if (!TypeExpression.Unify(BasicTypes.PatternSeq, bottom.Type).Success) throw new Exception(string.Format("Wrong bottomLevel type: got {0}, expected {1}", bottom.Type, BasicTypes.PatternSeq.ToString()));

                var p = new PartitionDescriptor { Name = name, TopLevel = top.Compile() as QueryMotive, BottomLevel = bottom.Compile() as QueryMotive };
                PartitionDescriptors.Add(p);

                // TODO: async
                Structures.ForEach(s => s.AddPartition(p));

                return string.Format("The descriptor '{0}' was successfully added. This change will take effect in the next computation.", name);
            }
            catch (Exception e)
            {
                return string.Format("The descriptor '{0}' could not be added: {1}.", name, e.Message);
            }                       
        }

        /// <summary>
        /// Removes a part. descriptor.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string RemovePartitionDescriptor(string name)
        {
            try
            {
                var d = PartitionDescriptors.FirstOrDefault(x => x.Name.EqualOrdinalIgnoreCase(name));
                if (d == null)
                {
                    return string.Format("A descriptor called '{0}' does not exist.", name);
                }

                PartitionDescriptors.Remove(d);
                Structures.ForEach(s => s.RemovePartition(d));
                return string.Format("The descriptor '{0}' was successfully removed. This change will take effect in the next computation.");
            }
            catch (Exception e)
            {
                return string.Format("The descriptor '{0}' could not be removed: {1}.", e.Message);
            }    
        }

        /// <summary>
        /// List the descriptors.
        /// </summary>
        /// <returns></returns>
        public string ListPartitionDescriptors()
        {
            return string.Join(", ", PartitionDescriptors.Select(p => p.Name));
        }


        protected override void ExportToZip(ICSharpCode.SharpZipLib.Zip.ZipOutputStream zip, ComputationProgress progress)
        {
            throw new System.NotImplementedException();
        }

        protected override void LoadStateFromXml(XElement root)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnClear(IList<StructureWrap> structures)
        {
            
        }

        protected override void OnLoad(List<StructureWrap> newStructures)
        {
            
        }

        protected override void OnRemove(StructureWrap structure)
        {
            var results = ServiceLocator.Current.GetInstance<ResultViewModel>();
            results.Results.Where(r => r.Structure == structure).ToArray()
                .ForEach(r => results.Results.Remove(r));
        }

        protected override void OnRemoveMany(IList<StructureWrap> structures)
        {
            var results = ServiceLocator.Current.GetInstance<ResultViewModel>();

            foreach (var s in structures)
            {
                results.Results.Where(r => r.Structure == s).ToArray()
                    .ForEach(r => results.Results.Remove(r));
            }
        }

        protected override XElement SaveStateToXml()
        {
            return null;   
        }

        public override void UpdateStructuresView()
        {
            using (StructuresView.DeferRefresh())
            {
                StructuresView.SortDescriptions.Clear();
                StructuresView.SortDescriptions.Add(new SortDescription("Structure.Id", ListSortDirection.Ascending));
            }
        }

        void ReadDefaultSets()
        {
            try
            {
                StreamResourceInfo sri = Application.GetResourceStream(new Uri("DefaultSets.xml", UriKind.Relative));

                using (XmlReader xr = XmlReader.Create(sri.Stream))
                {
                    xr.Read();
                    var root = XElement.Load(xr);
                    SetManager.Update(root);
                }
            }
            catch (Exception e)
            {
                Log.Error("Parameter Sets", "Error reader default parameter sets: {0}", e.Message);
            }
        }

        public void AddActiveSet(EemParameterSet set)
        {
            var activeSet = new ActiveSet(this, set, Method, CutoffRadius, IgnoreWaters, SelectionOnly, CorrectCutoffTotalCharge);

            if (!ActiveSets.Contains(activeSet))
            {
                ActiveSets.Add(activeSet);
            }
        }

        public void AddActiveSets()
        {
            foreach (var set in SetManager.Sets.Where(s => s.IsSelected))
            {
                var activeSet = new ActiveSet(this, set, Method, CutoffRadius, IgnoreWaters, SelectionOnly, CorrectCutoffTotalCharge);

                if (!ActiveSets.Contains(activeSet))
                {
                    ActiveSets.Add(activeSet);
                }
            }
        }

        public void LoadReferenceCharges(FileInfo[] references)
        {
            HashSet<FileInfo> loaded = new HashSet<FileInfo>();
            foreach (var s in Structures)
            {
                foreach (var rc in references)
                {
                    if (loaded.Contains(rc)) continue;
                    if (rc.Name.StartsWith(s.Structure.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        loaded.Add(rc);
                        s.AddReferenceCharges(rc);
                    }
                }
            }
        }

        public void LoadSets(FileInfo[] xmls)
        {
            HashSet<string> sets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in xmls)
            {
                using (var s = f.OpenRead())
                {
                    try
                    {
                        var xml = XElement.Load(s);
                        var updated = SetManager.Update(xml);
                        sets.UnionWith(updated);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Parameter Sets", "'{0}', {1}", f.Name, e.Message);
                    }
                }
            }
            Log.Message("Loaded/updated set(s): {0}.", string.Join(", ", sets));
        }

        async void Compute()
        {
            var cs = ComputationService.Default;

            //if (ActiveSets.Count == 0 && Structures.Count == 0)
            //{
            //    Log.Info("Select at least one active set and load at least one structure.");
            //    return;
            //}

            //if (ActiveSets.Count == 0)
            //{
            //    Log.Info("Select at least one active set.");
            //    return;
            //}

            if (Structures.Count == 0)
            {
                Log.Info("Load at least one structure.");
                return;
            }

            var rvm = ServiceLocator.Current.GetInstance<ResultViewModel>();
            var avm = ServiceLocator.Current.GetInstance<AnalyzeViewModel>();
            var cvm = ServiceLocator.Current.GetInstance<CorrelationViewModel>();

            try
            {

                var progress = cs.Start();

                int count = 0;
                int ok = 0;
                int errors = 0;
                int warnings = 0;

                rvm.CurrentResult = null;
                avm.CurrentStructure = null;
                Structures.ForEach(s => s.ResetResults());

                int maxProgress = Structures.Count * ActiveSets.Count;
                var correlateSelectionOnly = cvm.SelectionOnly;

                foreach (var s in Structures)
                {
                    foreach (var set in ActiveSets)
                    {
                        progress.Update(statusText: string.Format("[{0}/{1}] Computing '{2}: {3}'", ++count, maxProgress, s.Structure.Id, set.Id), canCancel: set.Method != ChargeComputationMethod.Eem);
                        await set.ComputeCharges(s, progress);
                    }

                    await TaskEx.Run(() =>
                        {
                            foreach (var r in s.ReferenceCharges)
                            {
                                r.RecalcPartitionCharges();
                            }
                        });

                    progress.Update(isIndeterminate: true);
                    await s.ComputeCorrelations(correlateSelectionOnly, progress);

                    foreach (var set in s.Results)
                    {
                        switch (set.Result.State)
                        {
                            case ChargeResultState.Ok: ok++; break;
                            case ChargeResultState.Error: errors++; break;
                            case ChargeResultState.Warning: warnings++; break;
                        }
                    }
                }
                
                var elapsed = cs.Elapsed;

                Log.Message("Computation and correlation of {0} sets [{1} ok, {2} warning(s), {3} error(s)] done in {4}.", count, ok, warnings, errors, ComputationService.GetElapasedTimeString(elapsed));

                if (count == 0 && Structures.Any(s => s.ReferenceCharges.Count > 1))
                {
                    Log.Message("Reference charges have been correlated.");
                }

                var vm = ServiceLocator.Current.GetInstance<MainViewModel>();
                vm.Mode = AppMode.Result;

                ComputedPartitions = PartitionDescriptors.Select(p => p.Name).OrderBy(n => n).ToArray();

                QueryExecutionContext.ResetCache();
            }
            catch (ComputationCancelledException)
            {
                Log.Aborted();
            }
            catch (Exception e)
            {
                Log.Error("Compute Charges", e.Message);
            }
            finally
            {
                rvm.Update();
                cs.End();
            }
        }

        public async void Correlate()
        {
            var cs = ComputationService.Default;

            var avm = ServiceLocator.Current.GetInstance<AnalyzeViewModel>();
            var cvm = ServiceLocator.Current.GetInstance<CorrelationViewModel>();

            try
            {
                var progress = cs.Start();

                int count = 0;

                var oldCurrent = avm.CurrentStructure;
                var oldPartition = avm.CurrentPartition;

                avm.CurrentStructure = null;
                Structures.ForEach(s => s.ResetCorrelations());

                int maxProgress = Structures.Count * ActiveSets.Count;
                var correlateSelectionOnly = cvm.SelectionOnly;

                progress.Update(statusText: "Correlating...", currentProgress: 0, maxProgress: Structures.Count, isIndeterminate: false);

                foreach (var s in Structures)
                {
                    progress.UpdateProgress(count++);
                    await s.ComputeCorrelations(correlateSelectionOnly, progress);
                }

                var elapsed = cs.Elapsed;
                Log.Message("Correlation done in {0}.", ComputationService.GetElapasedTimeString(elapsed));
                avm.CurrentStructure = oldCurrent;
                avm.CurrentPartition = oldPartition;
            }
            catch (ComputationCancelledException)
            {
                Log.Aborted();
            }
            catch (Exception e)
            {
                Log.Error("Correlate", e.Message);
            }
            finally
            {
                cs.End();
            }
        }

        public Session()
            : base("Charges", ".cws")
        {
            GlobalSelection = new GlobalSelectionViewModel(this);

            SetManager = new ParameterSetManager();
            ParameterSetsView = new PagedCollectionView(SetManager.Sets);
            ParameterSetsView.SortDescriptions.Add(new SortDescription("Target", ListSortDirection.Ascending));
            ParameterSetsView.SortDescriptions.Add(new SortDescription("BasisSet", ListSortDirection.Ascending));
            ParameterSetsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            ParameterSetsView.GroupDescriptions.Add(new PropertyGroupDescription("Target") { StringComparison = StringComparison.InvariantCultureIgnoreCase });
            ParameterSetsView.GroupDescriptions.Add(new PropertyGroupDescription("BasisSet") { StringComparison = StringComparison.InvariantCultureIgnoreCase });


            ActiveSets = new ObservableCollection<ActiveSet>();
            ActiveSetsView = new PagedCollectionView(ActiveSets);
            ActiveSetsView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            PartitionDescriptors = new List<PartitionDescriptor>();
            PartitionDescriptors.Add(new PartitionDescriptor
            {
                Name = "Atoms",
                TopLevel = MetaQuery.CreateSymbol("Atoms").ApplyTo().Compile() as QueryMotive,
                BottomLevel = MetaQuery.CreateSymbol("Atoms").ApplyTo().Compile() as QueryMotive
            });
            PartitionDescriptors.Add(new PartitionDescriptor
            {
                Name = "Residues",
                TopLevel = MetaQuery.CreateSymbol("Residues").ApplyTo().Compile() as QueryMotive,
                BottomLevel = MetaQuery.CreateSymbol("Residues").ApplyTo().Compile() as QueryMotive
            });
            PartitionDescriptors.Add(new PartitionDescriptor
            {
                Name = "AtomTypes",
                TopLevel = MetaQuery.CreateSymbol("GroupedAtoms").ApplyTo().Compile() as QueryMotive,
                BottomLevel = MetaQuery.CreateSymbol("GroupedAtoms").ApplyTo().Compile() as QueryMotive
            });
            ComputedPartitions = new string[0];


            ReadDefaultSets();

            CurrentSet = SetManager.Sets[0];
        }
    }
}
