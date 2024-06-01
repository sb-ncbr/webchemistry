using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using WebChemistry.Charges.Core;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.Silverlight.Common.Services;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Charges.Silverlight.ViewModel;
using System.Windows.Media;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.Charges.Silverlight.DataModel
{
    public class StructureCharges
    {
        private ICommand _CopyCommand;
        public ICommand CopyCommand
        {
            get
            {
                _CopyCommand = _CopyCommand ?? new RelayCommand<string>(what => Copy(what));
                return _CopyCommand;
            }
        }

        private ICommand _visualizeCommand;
        public ICommand VisualizeCommand
        {
            get
            {
                _visualizeCommand = _visualizeCommand ?? new RelayCommand(() => ServiceLocator.Current.GetInstance<VisualizationViewModel>().CurrentCharges = this);
                return _visualizeCommand;
            }
        }

        public StructureWrap Structure { get; private set; }
        
        public string Name { get; private set; }

        public Color Color { get; private set; }

        public ChargeComputationResult Result { get; private set; }

        public IDictionary<string, AtomPartitionCharges> PartitionCharges { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public void AddChargeColumn(ListExporter<IAtom> exporter)
        {
            var charges = Result.Charges;
            var name = Name;
            exporter.AddExportableColumn(a =>
                {
                    ChargeValue value;
                    if (charges.TryGetValue(a, out value)) return value.Charge.ToStringInvariant("0.000");
                    return "-";
                }, ColumnType.Number, Name);
        }

        public void AddParitionChargeColumn(string partition, ListExporter<AtomPartition.Group> exporter)
        {
            var charges = PartitionCharges[partition];
            var name = Name + "_" + partition; 
            exporter.AddExportableColumn(a =>
            {
                double value;
                if (charges.PartitionCharges.TryGetValue(a, out value)) return value.ToStringInvariant("0.000");
                return "-";
            }, ColumnType.Number, Name);
        }

        public void AddDetailColumns(ListExporter<IAtom> exporter)
        {
            var charges = Result.Charges;
            var name = Name;
            exporter
            .AddExportableColumn(a =>
            {
                ChargeValue value;
                if (charges.TryGetValue(a, out value)) return value.Parameters.TargetQueryString;
                return "-";
            }, ColumnType.Number, Name + "_targetquery")
            .AddExportableColumn(a =>
            {
                ChargeValue value;
                if (charges.TryGetValue(a, out value)) return value.Charge.ToStringInvariant("0.000");
                return "-";
            }, ColumnType.Number, Name);
        }

        void Copy(string what)
        {
            try
            {
                switch (what.ToLower())
                {
                    case "csv":
                        {
                            var result = Result;
                            var exporter = Structure.Structure.Atoms.GetExporter("\t")
                                .AddExportableColumn(a => a.Id, ColumnType.Number, "AtomId");
                            AddChargeColumn(exporter);
                            Clipboard.SetText(exporter.ToCsvString());
                        }
                        break;
                    ////case "csvresidues":
                    ////    {
                    ////        throw 
                    ////        var result = Result;
                    ////        var exporter = Structure.Structure.PdbResidues().GetExporter("\t")
                    ////            .AddExportableColumn(r => r.Number, ColumnType.Number, "SerialNumber")
                    ////            .AddExportableColumn(r => r.ChainIdentifier.ToString(), ColumnType.Number, "Chain")
                    ////            .AddExportableColumn(r => r.Name, ColumnType.String, "Name");
                    ////        AddResidueChargeColumn(exporter);
                    ////        Clipboard.SetText(exporter.ToCsvString());
                    ////    }
                    ////    break;
                    case "csvdetails":
                        {
                            var result = Result;
                            var exporter = Structure.Structure.Atoms.GetExporter("\t")
                                .AddExportableColumn(a => a.Id, ColumnType.Number, "AtomId")
                                .AddExportableColumn(a => a.ElementSymbol, ColumnType.String, "Element")
                                .AddExportableColumn(a =>
                                {
                                    int mult;
                                    if (result.Multiplicities.TryGetValue(a, out mult)) return mult.ToString();
                                    return "-";
                                }, ColumnType.Number, "Type");
                            AddDetailColumns(exporter);
                            Clipboard.SetText(exporter.ToCsvString());
                        }
                        break;
                    case "mol2":
                        {
                            var charges = Result.Charges;
                            var mol2 = Structure.Structure.ToMol2String(remark: Name, chargeSelector: a =>
                                {
                                    ChargeValue value;
                                    if (charges.TryGetValue(a, out value)) return value.Charge;
                                    return 0.0;
                                });
                            Clipboard.SetText(mol2);
                            break;
                        }
                    case "id":
                        Clipboard.SetText(Result.Parameters.Id);
                        break;
                }
                LogService.Default.Message("Data copied to clipboard.");
            }
            catch (Exception e)
            {
                LogService.Default.Error("Clipboard Copy", e.Message);
            }
        }

        public void ClearProperties()
        {
            Structure.Structure.RemoveAtomProperties(Result.Parameters.Id);
        }

        public void RecalcPartitionCharges()
        {
            this.PartitionCharges = Structure.Partitions.ToDictionary(p => p.Name, p => p.GetGroupCharges(Result), StringComparer.OrdinalIgnoreCase);
        }

        static int GetColorSeed(EemChargeComputationParameters prms)
        {
            int seed = 31;
            seed = unchecked(seed * 23 + prms.Method.GetHashCode());
            seed = unchecked(seed * 23 + prms.IgnoreWaters.GetHashCode());
            seed = unchecked(seed * 23 + prms.SelectionOnly.GetHashCode());
            seed = unchecked(seed * 23 + prms.Set.GetHashCode());
            if (prms.Method == ChargeComputationMethod.EemCutoff) seed = unchecked(seed * 23 + prms.CutoffRadius.GetHashCode());
            return seed;
        }

        public StructureCharges(StructureWrap parent, ChargeComputationResult result)
        {
            this.Structure = parent;
            this.Result = result;
            this.PartitionCharges = Structure.Partitions.ToDictionary(p => p.Name, p => p.GetGroupCharges(Result), StringComparer.OrdinalIgnoreCase);
            this.Name = result.Parameters.Id;

            var seed = GetColorSeed(result.Parameters);
            Pallete.UpdateSeed(seed);
            this.Color = Pallete.GetRandomColor();
            
            {
                parent.Structure.RemoveAtomProperties(result.Parameters.Id);
                var charges = result.Charges;
                var props = RealAtomProperties.Create(parent.Structure, result.Parameters.Id, "Charges", a =>
                {
                    ChargeValue val;
                    if (charges.TryGetValue(a, out val)) return val.Charge;
                    return null;
                });
                parent.Structure.AttachAtomProperties(props);
            }
        }
    }
}
