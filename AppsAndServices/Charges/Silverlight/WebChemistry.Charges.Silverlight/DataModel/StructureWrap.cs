namespace WebChemistry.Charges.Silverlight.DataModel
{
    using System.Collections.ObjectModel;
    using WebChemistry.Charges.Core;
    using System.Linq;
    using WebChemistry.Silverlight.Common.DataModel;
    using System.Windows.Data;
    using System.Collections.Generic;
    using WebChemistry.Charges.Silverlight.ViewModel;
    using WebChemistry.Framework.Core;
    using System.Threading.Tasks;
    using System.ComponentModel;
    using System;
    using GalaSoft.MvvmLight.Command;
    using System.Windows.Input;
    using System.Windows.Controls;
    using System.IO;
    using WebChemistry.Silverlight.Common.Services;
    using Microsoft.Practices.ServiceLocation;
    using System.Windows;
    using System.Text;

    public class StructureWrap : StructureWrapBase<StructureWrap>
    {
        private ICommand _addReferenceChargesCommand;
        public ICommand AddReferenceChargesCommand
        {
            get
            {
                _addReferenceChargesCommand = _addReferenceChargesCommand ?? new RelayCommand(() => AddReferenceCharges());
                return _addReferenceChargesCommand;
            }
        }

        private ICommand _removeReferenceChargesCommand;
        public ICommand RemoveReferenceChargesCommand
        {
            get
            {
                _removeReferenceChargesCommand = _removeReferenceChargesCommand ?? new RelayCommand<StructureCharges>(c => RemoveReferenceCharges(c));
                return _removeReferenceChargesCommand;
            }
        }

        private ICommand _removeCommand;
        public ICommand RemoveCommand
        {
            get
            {
                _removeCommand = _removeCommand ?? new RelayCommand(() => ServiceLocator.Current.GetInstance<Session>().Remove(this) );
                return _removeCommand;
            }
        }

        private int _totalCharge;
        public int TotalCharge
        {
            get
            {
                return _totalCharge;
            }

            set
            {
                if (_totalCharge == value) return;

                _totalCharge = value;
                NotifyPropertyChanged("TotalCharge");
            }
        }

        public ObservableCollection<StructureCharges> ReferenceCharges { get; private set; }
        
        public ObservableCollection<StructureCharges> Charges { get; private set; }
        public PagedCollectionView ChargesView { get; set; }

        //public ObservableCollection<Correlation> Correlations { get; private set; }
        //public ObservableCollection<Correlation> ResidueCorrelations { get; private set; }

        public IDictionary<string, Correlation[]> Correlations { get; private set; }

        public ObservableCollection<StructureCharges> Results { get; set; }
        public PagedCollectionView ResultsView { get; set; }

        public ObservableCollection<AtomPartition> Partitions { get; private set; }
        
        /// <summary>
        /// Adds a partition.
        /// </summary>
        /// <param name="d"></param>
        public void AddPartition(PartitionDescriptor d)
        {
            var p = d.Apply(Structure);
            this.Partitions.Add(p);
        }

        /// <summary>
        /// Removes a partition if it exists.
        /// </summary>
        /// <param name="d"></param>
        public void RemovePartition(PartitionDescriptor d)
        {
            var p = this.Partitions.FirstOrDefault(x => x.Name.EqualOrdinalIgnoreCase(d.Name));
            if (p != null) this.Partitions.Remove(p);
        }

        public void ResetResults()
        {
            Charges.Clear();
            Correlations.Clear();
            Results.ForEach(r => r.ClearProperties());
            Results.Clear();
            ReferenceCharges.ForEach(c => Charges.Add(c));
        }

        public void ResetCorrelations()
        {
            Correlations.Clear();
        }

        public async Task ComputeCorrelations(bool selectionOnly, ComputationProgress progress)
        {
            if (Charges.Count < 2) return;

            if (selectionOnly && Selection.SelectedCount == 2)
            {
                LogService.Default.Info("Cannot correlate sets of '{0}' using atom selection: Select at least 2 atoms.");
                return;
            }

            var correlations = await TaskEx.Run(() =>
                {

                    var pairs = from i in Enumerable.Range(0, Charges.Count - 1)
                                from j in Enumerable.Range(i + 1, Charges.Count - i - 1)
                                select Tuple.Create(i, j);

                    var sync = new object();
                    var ret = new List<Correlation>();

                    PortableTPL.Parallel.ForEach(pairs.ToArray(), p =>
                    {
                        progress.ThrowIfCancellationRequested();
                        
                        var cxs = Partitions.Select(t => Correlation.Create(this, selectionOnly, t.Name, p.Item1, p.Item2)).ToList();

                        lock (sync)
                        {
                            ret.AddRange(cxs);
                            ret.AddRange(cxs.Select(t => t.Invert()));
                        }
                    });

                    return ret;
                });
            
            Correlations = correlations
                .GroupBy(c => c.PartitionName)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        public void AddResult(ChargeComputationResult result)
        {
            var charges = new StructureCharges(this, result);
            Results.Add(charges);

            if (result.State != ChargeResultState.Error)
            {
                Charges.Add(charges);
            }
        }

        public override void Dispose()
        {
            
        }

        public override string ToString()
        {
            return Structure.Id;
        }

        public void AddReferenceCharges(FileInfo info)
        {
            using (var r = info.OpenText())
            {
                AddReferenceCharges(info.Name, r.ReadToEnd());
            }
        }

        protected override void OnPropertiesAttached(AtomPropertiesBase props)
        {
            if (!props.Tag.EqualOrdinalIgnoreCase("charges")) return;

            var log = LogService.Default;

            var name = props.Name.EndsWith("_ref", StringComparison.OrdinalIgnoreCase)
                ? props.Name.Substring(0, props.Name.Length - 4) : props.Name;


            Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>();

            foreach (var a in Structure.Atoms)
            {
                var value = props.TryGetValue(a);
                if (value != null) charges.Add(a, new ChargeValue { IsReference = true, Charge = (double)value });
            }
            
            var set = EemParameterSet.ReferenceSet(name);
            var result = new ChargeComputationResult(charges)
            {
                State = ChargeResultState.Ok,
                Parameters = new EemChargeComputationParameters
                {
                    Method = ChargeComputationMethod.Reference,
                    Structure = Structure,
                    Set = set
                }
            };
            var chrgs = new StructureCharges(this, result);
            if (ReferenceCharges.Any(c => c.Name.Equals(chrgs.Name, StringComparison.OrdinalIgnoreCase)))
            {
                log.Info("Charges '{0}' have already been loaded.", name);
                return;
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => this.ReferenceCharges.Add(chrgs));
            }
        }

        //Dictionary<IAtom, ChargeValue> ReadReferenceCharges(string filename, string content)
        //{
        //    var log = LogService.Default;
        //    if (filename.EndsWith(".pqr", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var rcr = StructureReader.Read(filename, () => new StringReader(content));
        //        var rc = rcr.Structure;

        //        Dictionary<IAtom, ChargeValue> ret = new Dictionary<IAtom, ChargeValue>();

        //        foreach (var a in rc.Atoms)
        //        {
        //            IAtom b;
        //            if (Structure.Atoms.TryGetAtom(a.Id, out b))
        //            {
        //                ret.Add(b, new ChargeValue { IsReference = true, Charge = a.PqrCharge() });
        //            }
        //        }

        //        return ret;
        //    }
        //    else if (filename.EndsWith(".mol2", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var rcr = StructureReader.Read(filename, () => new StringReader(content));
        //        var rc = rcr.Structure;
        //        if (!rc.Mol2ContainsCharges())
        //        {
        //            log.Error("Ref. Charges", "The file '{0}' does not contain information about charges.", filename);
        //            return null;
        //        }

        //        if (rc.Atoms.Count != Structure.Atoms.Count)
        //        {
        //            log.Error("Ref. Charges", "'{0}': Atom counts do not match. Expected {1}, got {2}.", filename, Structure.Atoms.Count, rc.Atoms.Count);
        //            return null;
        //        }

        //        Dictionary<IAtom, ChargeValue> ret = new Dictionary<IAtom, ChargeValue>();

        //        for (int i = 0; i < rc.Atoms.Count; i++)
        //        {
        //            var a = Structure.Atoms[i];
        //            var c = rc.Atoms[i];

        //            if (a.ElementSymbol != c.ElementSymbol)
        //            {
        //                log.Error("Ref. Charges", "'{0}': Atom element symbols do not match. Atom Id {1}, expected {1}, got {2}.", filename, a.Id, a.ElementSymbol, c.ElementSymbol);
        //                return null;
        //            }

        //            ret.Add(a, new ChargeValue { Charge = c.Mol2PartialCharge(), IsReference = true });
        //        }

        //        return ret;
        //    }
        //    else
        //    {
        //        using (var reader = new StringReader(content))
        //        {
        //            reader.ReadLine();
        //            int count = int.Parse(reader.ReadLine());

        //            if (count != Structure.Atoms.Count)
        //            {
        //                log.Error("Ref. Charges", "Could not add charges '{0}' to structure '{1}': the atom counts do not match (got {2}, expected {3}).", filename, Structure.Id, count, Structure.Atoms.Count);
        //                return null;
        //            }

        //            var set = EemParameterSet.ReferenceSet(filename);
        //            Dictionary<IAtom, ChargeValue> charges = new Dictionary<IAtom, ChargeValue>(count);

        //            List<Tuple<int, ElementSymbol, double>> records = new List<Tuple<int, ElementSymbol, double>>();

        //            var sep = " ".ToCharArray();
        //            for (int i = 0; i < count; i++)
        //            {
        //                var line = reader.ReadLine();
        //                var fields = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        //                if (fields.Length == 3) records.Add(Tuple.Create(int.Parse(fields[0]), ElementSymbol.Create(fields[1]), fields[2].ToDouble().GetValue()));
        //            }

        //            bool error = false;
        //            int errorIndex = 0;
        //            for (int i = 0; i < count; i++)
        //            {
        //                var entry = records[i];

        //                if (Structure.Atoms[i].ElementSymbol != entry.Item2)
        //                {
        //                    error = true;
        //                    errorIndex = i;
        //                    break;
        //                }

        //                charges[Structure.Atoms[i]] = new ChargeValue
        //                {
        //                    IsReference = true,
        //                    Charge = entry.Item3
        //                };
        //            }

        //            if (error)
        //            {
        //                log.Error("Reference Charges", "Could not add charges '{0}' to structure '{1}': error at line {2}.", filename, Structure.Id, errorIndex + 3);
        //                return null;
        //            }

        //            return charges;
        //        }
        //    }
        //}

        public void AddReferenceCharges(string filename, string content)
        {
            var log = LogService.Default;
            try
            {
                //var set = EemParameterSet.ReferenceSet(filename);
                //var charges = ReadReferenceCharges(filename, content);
                //if (charges == null) return;

                //var result = new ChargeComputationResult(charges)
                //{
                //    State = ResultState.Ok,
                //    Parameters = new EemChargeComputationParameters
                //    {
                //        Method = ChargeComputationMethod.Reference,
                //        Structure = Structure,
                //        Set = set
                //    }
                //};

                ChargeComputationResult result;
                using (var ms = new MemoryStream())
                using (var writer = new StreamWriter(ms))
                {
                    writer.Write(content);
                    writer.Flush();
                    ms.Position = 0;
                    result = ChargeComputationResult.FromReference(filename, Structure, () => ms);
                }

                var chrgs = new StructureCharges(this, result);
                if (ReferenceCharges.Any(c => c.Name.Equals(chrgs.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    log.Info("Charges '{0}' have already been loaded.", filename);
                    return;
                }
                else
                {
                    this.ReferenceCharges.Add(chrgs);
                }
            }
            catch (Exception e)
            {
                log.Error("Reference charges", "Error adding charges '{0}' to structure '{1}': {2}", filename, Structure.Id, e.Message);
            }
        }

        async void AddReferenceCharges()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter =
                    string.Format("Properties (*.?chrg, *.pqr, *.mol2, *{0})|*.chrg;*.achrg;*.bchrg;*.cchrg;*.dchrg;*.echrg;*.fchrg;*.gchrg;*.hchrg;*.ichrg;*.jchrg;*.kchrg;*.lchrg;*.mchrg;*.nchrg;*.ochrg;*.pchrg;*.qchrg;*.rchrg;*.schrg;*.uchrg;*.tchrg;*.vchrg;*.wchrg;*.xchrg;*.ychrg;*.zchrg;*.pqr;*.mol2;*{0}",
                        AtomPropertiesEx.DefaultExtension),
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                foreach (var file in ofd.Files.Where(f => !f.Name.EndsWith(AtomPropertiesEx.DefaultExtension, StringComparison.OrdinalIgnoreCase))) AddReferenceCharges(file);
                await Session.Load(ofd.Files.Where(f => f.Name.EndsWith(AtomPropertiesEx.DefaultExtension, StringComparison.OrdinalIgnoreCase)).ToArray());
            }
        }

        void RemoveReferenceCharges(StructureCharges c)
        {
            ReferenceCharges.Remove(c);
        }

        protected override bool SelectAtomsInternal(IEnumerable<IAtom> atoms, bool selected)
        {
            bool changed = false;
            foreach (var atom in atoms)
            {
                if (atom.IsSelected != selected)
                {
                    atom.IsSelected = selected;
                    changed = true;
                }

            }
            return changed;
        }

        protected override void OnCreate()
        {
            this.Correlations = new Dictionary<string, Correlation[]>();
            this.ReferenceCharges = new ObservableCollection<StructureCharges>();
            this.Charges = new ObservableCollection<StructureCharges>();
            this.Results = new ObservableCollection<StructureCharges>();
            
            this.Partitions = new ObservableCollection<AtomPartition>();

            var session = this.Session as Session;
            foreach (var d in session.PartitionDescriptors)
            {
                this.Partitions.Add(d.Apply(Structure));
            }

            if (Structure.Mol2ContainsCharges())
            {
                var charges = new ChargeComputationResult(Structure.Atoms.ToDictionary(a => a, a => new ChargeValue
                {
                    IsReference = true,
                    Charge = a.Mol2PartialCharge()
                }))
                {
                    State = ChargeResultState.Ok,
                    Parameters = new EemChargeComputationParameters
                    {
                        Method = ChargeComputationMethod.Reference,
                        Structure = Structure,
                        Set = EemParameterSet.ReferenceSet("Mol2")
                    }
                };
                this.ReferenceCharges.Add(new StructureCharges(this, charges));
            }
            else if (StructureType == Framework.Core.StructureType.Pqr)
            {
                var charges = new ChargeComputationResult(Structure.Atoms.ToDictionary(a => a, a => new ChargeValue
                {
                    IsReference = true,
                    Charge = a.PqrCharge()
                }))
                {
                    State = ChargeResultState.Ok,
                    Parameters = new EemChargeComputationParameters
                    {
                        Method = ChargeComputationMethod.Reference,
                        Structure = Structure,
                        Set = EemParameterSet.ReferenceSet("Pqr")
                    }
                };
                this.ReferenceCharges.Add(new StructureCharges(this, charges));

            }
            
            this.ChargesView = new PagedCollectionView(this.Charges);
            this.ResultsView = new PagedCollectionView(this.Results);

            this.ResultsView.SortDescriptions.Add(new SortDescription("Result.State", ListSortDirection.Ascending));
            this.ResultsView.SortDescriptions.Add(new SortDescription("Result.Parameters.MethodId", ListSortDirection.Ascending));
            
            ResetResults();
        }
    }
}
