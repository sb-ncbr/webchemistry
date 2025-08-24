namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.Platform.Services;

    class NameBond : IEquatable<NameBond>
    {
        public string Key { get; private set; }

        public string A { get; private set; }
        public string B { get; private set; }

        int hash;

        public override bool Equals(object obj)
        {
            var other = obj as NameBond;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public NameBond(IBond bond)
        {
            var a = bond.A.PdbName();
            var b = bond.B.PdbName();

            if (StringComparer.Ordinal.Compare(a, b) <= 0)
            {
                this.Key = a + "-" + b;
                this.A = a;
                this.B = b;
            }
            else
            {
                this.Key = b + "-" + a;
                this.A = b;
                this.B = a;
            }

            hash = this.Key.GetHashCode();
        }

        public bool Equals(NameBond other)
        {
            return other.Key.EqualOrdinal(this.Key);
        }
    }

    class ModelNamingHelper
    {
        public MotiveModel Model { get; private set; }

        public NameBond[] NameBonds { get; private set; }

        public string[] Names { get; private set; }

        public ModelNamingHelper(MotiveModel model)
        {
            this.Model = model;
            this.NameBonds = model.Structure.Bonds.Select(b => new NameBond(b)).ToArray();
            this.Names = model.Structure.Atoms.Select(a => a.PdbName()).ToArray();
        }
    }

    class NamingAnalysisResult
    {
        public IAtom[][] DuplicateNames { get; set; }
        public bool HasNonIsomorphicNaming { get; set; }
        public IAtom[] NonBoundarySubstitutionAndForeignAtoms { get; set; }        
        public string[] UnmatchedNames { get; set; }

        public HashSet<IAtom> HasNamingIssue { get; set; }
        public HashSet<IAtom> HasNamingIssueWithEquivalence { get; set; }
        public HashSet<IAtom> HasNamingIssueWithEquivalenceIgnoreBonds { get; set; }


        public HashSet<IAtom> HasNamingIssueWith_OUT_Equivalence { get; set; }
        public HashSet<IAtom> HasNamingIssueWith_OUT_EquivalenceIgnoreBonds { get; set; }

        public static readonly NamingAnalysisResult Empty = new NamingAnalysisResult
        {
            DuplicateNames = new IAtom[0][],
            HasNonIsomorphicNaming = false,
            NonBoundarySubstitutionAndForeignAtoms = new IAtom[0],
            UnmatchedNames = new string[0],

            HasNamingIssue = new HashSet<IAtom>(),
            HasNamingIssueWithEquivalence = new HashSet<IAtom>(),
            HasNamingIssueWithEquivalenceIgnoreBonds = new HashSet<IAtom>(),
            HasNamingIssueWith_OUT_Equivalence = new HashSet<IAtom>(),
            HasNamingIssueWith_OUT_EquivalenceIgnoreBonds = new HashSet<IAtom>()
        };
    }

    partial class ValidatorService : ServiceBase<ValidatorService, MotiveValidatorConfig, MotiveValidatorStandaloneConfig, object>
    {
        static string FormatNamingEquivalenceClass(Dictionary<string, string> cls)
        {
            return cls
                .GroupBy(c => c.Value)
                .Select(g => g.Select(v => v.Key).OrderBy(v => v, StringComparer.Ordinal).JoinBy(":"))
                .OrderBy(g => g, StringComparer.Ordinal)
                .JoinBy("-");
        }

        static IAtom[][] CheckDuplicateNames(IAtom[] mainResidue)
        {
            return mainResidue
                .GroupBy(a => a.PdbName())
                .Where(g => g.Count() > 1)
                .Select(g => g.ToArray())
                .ToArray();
        }

        static bool CheckNamingIsomorphism(ModelNamingHelper model, IStructure mainInduced, HashSet<string> names)
        {
            var bonds = mainInduced.Bonds.Select(b => new NameBond(b)).ToHashSet();
            
            foreach (var b in model.NameBonds)
            {
                if (names.Contains(b.A) && names.Contains(b.B) && !bonds.Contains(b)) return true;
            }

            return false;
        }

        static IAtom[] CheckSubstitutionsAndForeignBoundary(IStructure matched, IAtom[] mainResidue)
        {
            var main = mainResidue.ToHashSet();
            var atoms = matched.Atoms;
            var bonds = matched.Bonds;

            return atoms
                .Where(a => !main.Contains(a) && bonds[a].Count > 1)
                .ToArray();
        }

        static string[] CheckUnmatchedNames(ModelNamingHelper model, HashSet<string> names)
        {
            return model.Names.Where(n => !names.Contains(n)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }


        internal static NamingAnalysisResult CheckNaming(ModelNamingHelper model, IStructure fragment, IAtom[] mainResidue, IStructure matched)
        {
            var inducedMain = fragment.InducedSubstructure("main", mainResidue.OrderBy(a => a.Id).ToArray(), cloneAtoms: false);
            var names = inducedMain.Atoms.Select(a => a.PdbName()).ToHashSet(StringComparer.Ordinal);

            return new NamingAnalysisResult
            {
                DuplicateNames = CheckDuplicateNames(mainResidue),
                HasNonIsomorphicNaming = CheckNamingIsomorphism(model, inducedMain, names),
                NonBoundarySubstitutionAndForeignAtoms = CheckSubstitutionsAndForeignBoundary(matched, mainResidue),
                UnmatchedNames = CheckUnmatchedNames(model, names)
            };
        }

        internal static HashSet<IAtom> FilterNamingEquivalenceClass(MotiveEntry entry, Dictionary<IAtom, IAtom> withIssueModelToMotif, Dictionary<string, string> cls)
        {
            return entry.ModelToMotivePairing
                .Where(p => withIssueModelToMotif.ContainsKey(p.Key))
                .Where(p => cls.ContainsKey(p.Key.PdbName()) && cls.ContainsKey(p.Value.PdbName()))
                .Where(p => cls[p.Key.PdbName()].EqualOrdinal(cls[p.Value.PdbName()]))
                .Select(p => p.Value)
                .OrderBy(p => p.Id)
                .ToHashSet();
        }

        internal static Dictionary<int, string[]> GetNameMismatchFlags(MotiveEntry entry)
        {
            var naming = entry.NamingAnalysis;
            return entry.NamingAnalysis.HasNamingIssue
                .ToDictionary(
                a => entry.MotiveToModelPairing[a].Id,
                a =>
                {
                    var flags = new List<string>();
                    if (naming.HasNamingIssueWithEquivalence.Contains(a)) flags.Add("ChargeEquiv");
                    if (naming.HasNamingIssueWithEquivalenceIgnoreBonds.Contains(a)) flags.Add("ChargeEquivIgnoreBonds");
                    if (naming.HasNamingIssueWith_OUT_Equivalence.Contains(a)) flags.Add("NonChargeEquiv");
                    if (naming.HasNamingIssueWith_OUT_EquivalenceIgnoreBonds.Contains(a)) flags.Add("NonChargeEquivIgnoreBonds");
                    return flags.ToArray();
                });
        }
    }
}
