using GalaSoft.MvvmLight.Command;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Charges.Silverlight.Visuals;
using WebChemistry.Framework.Core;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Charges.Silverlight.ViewModel
{
    public class VisualizationViewModel : ObservableObject
    {
        [Ninject.Inject]
        public Session Session { get; set; }

        private ICommand _applyCustomChargeRangeCommand;
        public ICommand ApplyCustomChargeRangeCommand
        {
            get
            {
                _applyCustomChargeRangeCommand = _applyCustomChargeRangeCommand ?? new RelayCommand(() => UpdateChargeRange());
                return _applyCustomChargeRangeCommand;
            }
        }
        
        IDisposable selectionObserver;

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
                Update(true);
                NotifyPropertyChanged("SelectionOnly");
            }
        }

        private StructureWrap _currentStructure;
        public StructureWrap CurrentStructure
        {
            get
            {
                return _currentStructure;
            }

            set
            {
                if (_currentStructure == value) return;
                _currentStructure = value;
                Update();

                if (selectionObserver != null)
                {
                    selectionObserver.Dispose();
                    selectionObserver = null;
                }
                if (value != null)
                {
                    selectionObserver = value.Selection.Changed.Subscribe(_ => Update(true));
                }
                NotifyPropertyChanged("CurrentStructure");
            }
        }

        private StructureCharges _currentCharges;
        public StructureCharges CurrentCharges
        {
            get
            {
                return _currentCharges;
            }

            set
            {
                if (_currentCharges == value) return;

                _currentCharges = value;
                Update();
                NotifyPropertyChanged("CurrentCharges");
            }
        }

        private string _currentPartition;
        public string CurrentPartition
        {
            get
            {
                return _currentPartition;
            }

            set
            {
                if (_currentPartition == value) return;

                _currentPartition = value;
                Update();
                NotifyPropertyChanged("CurrentPartition");
            }
        }

        private bool _showWaters;
        public bool ShowWaters
        {
            get
            {
                return _showWaters;
            }

            set
            {
                if (_showWaters == value) return;

                _showWaters = value;
                if (Visual != null) Visual.ShowWaters(value);
                NotifyPropertyChanged("ShowWaters");
            }
        }

        private bool _useCustomChargeRange;
        public bool UseCustomChargeRange
        {
            get
            {
                return _useCustomChargeRange;
            }

            set
            {
                if (_useCustomChargeRange == value) return;

                _useCustomChargeRange = value;
                NotifyPropertyChanged("UseCustomChargeRange");
            }
        }

        private string _minCustomRangeString = "-1.0";
        public string MinCustomRangeString
        {
            get
            {
                return _minCustomRangeString;
            }

            set
            {
                if (_minCustomRangeString == value) return;

                _minCustomRangeString = value;
                NotifyPropertyChanged("MinCustomRangeString");
            }
        }

        private string _maxCustomRangeString = "1.0";
        public string MaxCustomRangeString
        {
            get
            {
                return _maxCustomRangeString;
            }

            set
            {
                if (_maxCustomRangeString == value) return;

                _maxCustomRangeString = value;
                NotifyPropertyChanged("MaxCustomRangeString");
            }
        }

        private ChargeStructureVisual3D _visual;
        public ChargeStructureVisual3D Visual
        {
            get
            {
                return _visual;
            }

            set
            {
                if (_visual == value) return;

                _visual = value;
                NotifyPropertyChanged("Visual");
            }
        }

        private string _caption = "Nothing displayed.";
        public string Caption
        {
            get { return _caption; }
            set
            {
                if (_caption == value) return;
                _caption = value;
                NotifyPropertyChanged("Caption");
            }
        }

        void UpdateChargeRange()
        {
            if (Visual == null) return;

            if (CurrentCharges == null)
            {
                Visual.RemoveCharges();
                return;
            }

            if (UseCustomChargeRange)
            {
                var range = from a in MinCustomRangeString.ToDouble()
                            from b in MaxCustomRangeString.ToDouble()
                            select Tuple.Create(a, b);

                if (range.IsNothing())
                {
                    LogService.Default.Error("Custom Charge Range", "Invalid min/max format.");
                    Visual.SetCharges(CurrentCharges);
                    return;
                }

                Visual.SetCharges(CurrentCharges, range.GetValue());
            }
            else
            {
                Visual.SetCharges(CurrentCharges);
            }
        }

        void UpdateCaption()
        {
            if (CurrentStructure != null)
            {
                if (CurrentPartition != null && CurrentCharges != null && Visual != null)
                {
                    Caption = string.Format("{0} ({1}), {2}, {3} elements", 
                        CurrentStructure.Structure.Id,
                        CurrentPartition,
                        CurrentCharges.Name,
                        Visual.Structure.Atoms.Count);
                    return;
                }
                else if (CurrentPartition != null && Visual != null)
                {
                    Caption = string.Format("{0} ({1}), {2} elements",
                        CurrentStructure.Structure.Id,
                        CurrentPartition,
                        Visual.Structure.Atoms.Count);
                    return;
                }
            }

            Caption = "Nothing displayed.";
        }

        IStructure GetStructure()
        {
            var partition = CurrentStructure.Partitions.FirstOrDefault(p => p.Name == CurrentPartition);
            var pivot = partition.InducedStructure;
            if (!SelectionOnly) return pivot;

            var atoms = pivot.Atoms
                .Where(a => partition.Groups[a.Id].Data.Any(x => x.IsSelected))
                .ToDictionary(a => a.Id);
            var bonds = pivot.Bonds
                .Where(b => atoms.ContainsKey(b.A.Id) && atoms.ContainsKey(b.B.Id))
                .Select(b => Bond.Create(atoms[b.A.Id], atoms[b.B.Id], b.Type));

            var ret = Structure.Create(pivot.Id, AtomCollection.Create(atoms.Values), BondCollection.Create(bonds));
            ret.ToCentroidCoordinates();
            return ret;
        }

        public void Clear()
        {
            if (Visual != null)
            {
                Visual.Dispose();
                Visual = null;
            }
            UpdateCaption();
        }

        public void Update(bool forceNew = false)
        {
            if (CurrentStructure == null)
            {
                if (Session.Structures.Count > 0)
                {
                    CurrentStructure = Session.StructuresView[0] as StructureWrap;
                    NotifyPropertyChanged("CurrentStructure");
                }
                else
                {
                    Clear();
                    return;
                }
            }

            var cp = CurrentPartition;
            if (cp == null)
            {
                if (Session.ComputedPartitions.Count == 0)
                {
                    Clear();
                    return;
                }

                if (Session.ComputedPartitions.Contains("Residues")) cp = "Residues";
                else cp = Session.ComputedPartitions[0];
            }
            _currentPartition = null;
            NotifyPropertyChanged("CurrentPartition");
            _currentPartition = cp;
            NotifyPropertyChanged("CurrentPartition");
                        
            var partition = CurrentStructure.Partitions.FirstOrDefault(p => p.Name == CurrentPartition);
            if (partition == null)
            {
                LogService.Default.Error("Visualization", "Partition not found.");
                Clear();
                return;
            }

            if (Visual != null && (forceNew || !object.ReferenceEquals(Visual.Partition, partition)))
            {
                Clear();
            }

            if (Visual == null)
            {
                var s = GetStructure();

                if (s.Atoms.Count > 2000)
                {
                    LogService.Default.Warning(
                        "Structure '{0}' cannot be displayed because it contains more than 2500 elements. Select a different partition or create a new selection and use 'Show Selection' function to display its fragments.",
                        s.Id);
                    return;
                }

                Visual = new ChargeStructureVisual3D(partition, GetStructure());
            }

            Visual.ShowWaters(ShowWaters);
            UpdateChargeRange();
            UpdateCaption();
        }
    }
}
