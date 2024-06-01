using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using System.Windows.Input;
using WebChemistry.Charges.Core;
using System.Linq;
using WebChemistry.Framework.Core;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.Charges.Silverlight.DataModel
{
    public class ActiveSet : ObservableObject
    {
        private ICommand _removeCommand;
        public ICommand RemoveCommand
        {
            get
            {
                _removeCommand = _removeCommand ?? new RelayCommand(() => Session.ActiveSets.Remove(this));
                return _removeCommand;
            }
        }

        private ICommand _copyIdCommand;
        public ICommand CopyIdCommand
        {
            get
            {
                _copyIdCommand = _copyIdCommand ?? new RelayCommand(() => Clipboard.SetText(this.Id));
                return _copyIdCommand;
            }
        }

        public Session Session { get; private set; }

        private EemParameterSet _set;
        public EemParameterSet Set
        {
            get
            {
                return _set;
            }

            set
            {
                if (_set == value) return;
                
                if (!EemParameterSet.ParameterEqual(_set, value)) cache.Clear();

                _set = value;

                NotifyPropertyChanged("Set");
            }
        }
        
        public ChargeComputationMethod Method { get; private set; }

        public double CutoffRadius { get; private set; }

        public bool CorrectCutoffTotalCharge { get; private set; }

        public string Id { get; private set; }

        public bool IgnoreWaters { get; private set; }

        public bool SelectionOnly { get; private set; }

        public string Description { get; private set; }
        
        Dictionary<EemChargeComputationParameters, ChargeComputationResult> cache = new Dictionary<EemChargeComputationParameters, ChargeComputationResult>();

        public async Task ComputeCharges(StructureWrap structure, ComputationProgress progress)
        {
            var parameters = new EemChargeComputationParameters
            {
                Structure = structure.Structure,
                Method = Method,
                CutoffRadius = CutoffRadius,
                CorrectCutoffTotalCharge = CorrectCutoffTotalCharge,
                IgnoreWaters = IgnoreWaters,
                SelectionOnly = SelectionOnly,
                AtomSelection = SelectionOnly ? structure.Structure.Atoms.Where(a => a.IsSelected).ToArray() : new IAtom[0],
                Set = Set,
                TotalCharge = structure.TotalCharge
            };

            ChargeComputationResult result;

            if (cache.TryGetValue(parameters, out result))
            {
                structure.AddResult(result);
                return;
            }

            result = await TaskEx.Run(() => EemSolver.Compute(parameters, progress));
            cache.Add(parameters, result);
            structure.AddResult(result);
        }

        public override string ToString()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ActiveSet;
            if (other == null) return false;
            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        public ActiveSet(Session session, EemParameterSet set, ChargeComputationMethod method, double cutoffRadius, 
            bool ignoreWaters, bool selectionOnly, bool correctTotal)
        {
            this.Session = session;
            this.Set = set;
            this.IgnoreWaters = ignoreWaters;
            this.SelectionOnly = selectionOnly;
            this.Method = method;
            this.CutoffRadius = cutoffRadius;
            this.CorrectCutoffTotalCharge = correctTotal;

            if (Method == ChargeComputationMethod.Eem)
            {
                this.Id = Set.Name + "_eem";
                this.Description = "EEM";
            }
            else
            {
                this.Id = Set.Name + "_cut_" + CutoffRadius.ToStringInvariant("0");
                this.Description = "EEM Cutoff " + CutoffRadius.ToStringInvariant("0") + (correctTotal ? ", With Correction" : ", No Correction");
                this.Id += correctTotal ? "_corr" : "";
            }

            this.Id += ignoreWaters ? "_nowtr" : "";
            this.Id += selectionOnly ? "_sel" : "";
            
            this.Description += ", " + (ignoreWaters ? "Without Waters" : "With Waters");
            this.Description += ", " + (selectionOnly ? "Selection" : "All Atoms");
        }        
    }
}
