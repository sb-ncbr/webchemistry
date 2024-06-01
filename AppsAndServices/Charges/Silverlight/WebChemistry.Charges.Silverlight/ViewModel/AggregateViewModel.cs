using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WebChemistry.Charges.Core;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class AggregateViewModel : ObservableObject
    {
        public class ChargeWrap : InteractiveObject
        {
            public AggregateViewModel Parent { get; set; }
            public StructureCharges Charges { get; set; }

            private ICommand _copyCommand;
            public ICommand CopyCommand
            {
                get
                {
                    _copyCommand = _copyCommand ?? new RelayCommand(() => Copy());
                    return _copyCommand;
                }
            }

            private ICommand _setCurrentCommand;
            public ICommand SetCurrentCommand
            {
                get
                {
                    _setCurrentCommand = _setCurrentCommand ?? new RelayCommand(() => Parent.CurrentCharges = this);
                    return _setCurrentCommand;
                }
            }

            public IList<PartitionClusteringStats> Clusters 
            { 
                get 
                {
                    return Charges.PartitionCharges[Parent.AnalyzeViewModel.CurrentPartition].ClusterStats;                    
                }
            }

            public PartitionClusteringStats CurrentCluster
            {
                get
                {
                    return Clusters.FirstOrDefault(c => c.Clustering.Name == Parent.CurrentAggregateName);
                }
            }

            protected override void OnSelectedChanged()
            {
                if (Parent != null) Parent.Update();
            }

            void Copy()
            {
                try
                {
                    var cluster = CurrentCluster;
                    if (cluster == null)
                    {
                        LogService.Default.Message("No aggregate selected to copy.");
                        return;
                    }
                    var exporter = PartitionClustering.GetClusterStatsExporter("\t", cluster.Stats);
                    Clipboard.SetText(exporter.ToCsvString());
                    LogService.Default.Message("Data copied to clipboard.");
                }
                catch (Exception e)
                {
                    LogService.Default.Error("Clipboard Copy", e.Message);
                }
            }
        }

        [Ninject.Inject]
        public Session Session { get; set; }

        [Ninject.Inject]
        public AnalyzeViewModel AnalyzeViewModel { get; set; }

        string[] propertyTypes = new string[] 
        { 
            "Minimum Charge",  // 0
            "Maximum Charge",  // 1
            "Average Charge", // 2
            "Average Absolute Charge", // 3
            "Standard Charge Deviation", // 4
            "Standard Absolute Charge Deviation", // 5
        };
        public string[] PropertyTypes { get { return propertyTypes; } }

        private int _propertyTypeIndex;
        public int PropertyTypeIndex
        {
            get
            {
                return _propertyTypeIndex;
            }

            set
            {
                if (_propertyTypeIndex == value) return;

                _propertyTypeIndex = value;
                NotifyPropertyChanged("PropertyTypeIndex");
                Update();
            }
        }

        private ChargeWrap[] _charges;
        public ChargeWrap[] Charges
        {
            get
            {
                return _charges;
            }

            set
            {
                if (_charges == value) return;

                _charges = value;
                NotifyPropertyChanged("Charges");
            }
        }

        private string[] _aggregateNames;
        public string[] AggregateNames
        {
            get
            {
                return _aggregateNames;
            }

            set
            {
                if (_aggregateNames == value) return;

                _aggregateNames = value;
                NotifyPropertyChanged("AggregateNames");
            }
        }

        private string _currentAggregateName;
        public string CurrentAggregateName
        {
            get
            {
                return _currentAggregateName;
            }

            set
            {
                if (_currentAggregateName == value) return;

                _currentAggregateName = value;
                NotifyPropertyChanged("CurrentAggregateName");
                CurrentUpdated();
                Update();
            }
        }

        ObservableCollection<PartitionClustering.ClusterStats> currentChargeStats = new ObservableCollection<PartitionClustering.ClusterStats>();
        public PagedCollectionView CurrentChargeStats { get; private set; }

        private ChargeWrap _currentCharges;
        public ChargeWrap CurrentCharges
        {
            get
            {
                return _currentCharges;
            }

            set
            {
                if (_currentCharges == value) return;

                _currentCharges = value;
                NotifyPropertyChanged("CurrentCharges");
                CurrentUpdated();
            }
        }

        Subject<Unit> updated = new Subject<Unit>();
        public IObservable<Unit> Updated { get { return updated; } }

        bool ignoreUpdate = false;
        void Update()
        {
            if (ignoreUpdate) return;
            updated.OnNext(Unit.Default);
        }

        void CurrentUpdated()
        {
            currentChargeStats.Clear();
            if (CurrentCharges == null || CurrentCharges.CurrentCluster == null) return;
            CurrentCharges.CurrentCluster.Stats.ForEach(s => currentChargeStats.Add(s));
        }
        
        public void SyncView()
        {
            var CurrentStructure = AnalyzeViewModel.CurrentStructure;
            var CurrentPartition = AnalyzeViewModel.CurrentPartition;

            if (CurrentStructure == null
                || CurrentPartition == null
                || CurrentStructure.Charges.Count == 0)
            {
                CurrentCharges = null;
                Charges = null;
                Update();
                return;
            }

            var aggrName = CurrentAggregateName;
            var part = CurrentStructure.Charges[0].PartitionCharges[CurrentPartition];
            AggregateNames = part.ClusterStats.Select(c => c.Clustering.Name).OrderBy(n => n).ToArray();
            aggrName = AggregateNames.Contains(aggrName) ? aggrName : AggregateNames[0];

            _currentAggregateName = null;
            NotifyPropertyChanged("CurrentAggregateName");
            _currentAggregateName = aggrName;
            NotifyPropertyChanged("CurrentAggregateName");

            var selectedIndices = Charges == null
                ? new HashSet<int>(CurrentStructure.Charges.Select((_, i) => i))
                : Charges
                    .Select((c, i) => new { Index = i, Charges = c })
                    .Where(c => c.Charges.IsSelected)
                    .Select(c => c.Index)
                    .ToHashSet();


            ignoreUpdate = true;
            var wraps = CurrentStructure.Charges
                .OrderBy(c => c.Name)
                .Select((c, i) => new ChargeWrap
                {
                    IsSelected = selectedIndices.Contains(i),
                    Charges = c,
                    Parent = this
                })
                .ToArray();
            ignoreUpdate = false;

            Charges = wraps;

            if (CurrentCharges != null)
            {
                var currentName = CurrentCharges.Charges.Name;
                CurrentCharges = wraps.FirstOrDefault(c => c.Charges.Name == currentName) ?? wraps[0];
            }
            else CurrentCharges = wraps[0];

            Update();
        }

        public AggregateViewModel()
        {
            CurrentChargeStats = new PagedCollectionView(currentChargeStats);
            CurrentChargeStats.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
        }
    }
}
