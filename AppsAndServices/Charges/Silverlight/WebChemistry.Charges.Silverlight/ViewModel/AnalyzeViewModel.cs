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
    public enum AnalyzeMode
    {
        Correlate,
        Aggregate
    }

    public class AnalyzeViewModel : ObservableObject
    {
        [Ninject.Inject]
        public Session Session { get; set; }

        [Ninject.Inject]
        public CorrelationViewModel CorrelationViewModel { get; set; }

        [Ninject.Inject]
        public AggregateViewModel AggregateViewModel { get; set; }

        private AnalyzeMode _mode;
        public AnalyzeMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode == value) return;

                _mode = value;
                NotifyPropertyChanged("Mode");
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
                NotifyPropertyChanged("CurrentStructure");
                Sync();
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
                NotifyPropertyChanged("CurrentPartition");
                Sync();
            }
        }        

        public void Show(bool showing)
        {
            if (CurrentStructure == null && Session.Structures.Count > 0)
            {
                CurrentStructure = Session.StructuresView[0] as StructureWrap;
            }
        }

        void Sync()
        {
            _currentPartition = CurrentStructure == null ? null : _currentPartition;

            if (_currentPartition == null && Session.ComputedPartitions.Count > 0)
            {
                _currentPartition = CurrentStructure == null ? null : Session.ComputedPartitions[0];
            }
            if (CurrentStructure != null && !CurrentStructure.Correlations.ContainsKey(CurrentPartition))
            {
                _currentPartition = null;
            }

            var part = _currentPartition;
            _currentPartition = null;
            NotifyPropertyChanged("CurrentPartition");
            _currentPartition = part;
            NotifyPropertyChanged("CurrentPartition");
            
            CorrelationViewModel.SyncView();
            AggregateViewModel.SyncView();
        }
    }
}
