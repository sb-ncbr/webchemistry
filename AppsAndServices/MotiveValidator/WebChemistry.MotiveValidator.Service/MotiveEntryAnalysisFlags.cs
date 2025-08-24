namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.MotiveValidator.DataModel;

    static class AnalysisFlags
    {
        public static Dictionary<string, Func<MotiveEntry, bool>> FlagMaps;
        public static string[] Flags;

        static AnalysisFlags()
        {
            FlagMaps = new Dictionary<string, Func<MotiveEntry, bool>>(StringComparer.Ordinal) 
            {
                { "Missing", e => e.Result.MissingAtomCount > 0 || e.Result.MissingRingCount > 0 || e.Result.State != ValidationResultEntryState.Validated },
                { "Missing_Atoms", e => e.Result.MissingAtomCount > 0 && e.Result.MissingRingCount == 0 },
                { "Missing_Rings", e => e.Result.MissingRingCount > 0 },
                { "Missing_Degenerate", e => e.Result.State == ValidationResultEntryState.Degenerate },
                //{ "Missing_Disconnected", e => e.Result.State == ValidationResultEntryState.Disconnected },

                { "HasAll", e => e.Result.State == ValidationResultEntryState.Validated && e.Result.MissingAtomCount == 0 && e.Result.MissingRingCount == 0 },

                { "HasAll_GoodChirality", e => e.Result.ChiralityMismatchCount == 0 && e.HasAll_Chirality() },
                { "HasAll_GoodChirality_IgnorePlanarAndNonSingleBondErrors", e => e.HasAll_Chirality() && 
                    (e.Result.ChiralityMismatchCount == 0 || e.Result.ChiralityMismatches.Keys.All(a => e.Model.ChiralAtomsPlanar.Contains(a) || e.Model.ChiralAtomsNonSingleBond.Contains(a)))},

                { "HasAll_BadChirality", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality() },
                { "HasAll_BadChirality_Planar", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality()  && e.Result.ChiralityMismatches.Keys.Any(a => e.Model.ChiralAtomsPlanar.Contains(a)) },
                { "HasAll_BadChirality_NonSingleBond", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality()  && e.Result.ChiralityMismatches.Keys.Any(a => e.Model.ChiralAtomsNonSingleBond.Contains(a)) },
                { "HasAll_BadChirality_Metal", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality()  && e.Result.ChiralityMismatches.Keys.Any(a => e.Model.ChiralAtomsMetal.Contains(a)) },
                { "HasAll_BadChirality_Carbon", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality()  && e.Result.ChiralityMismatches.Keys.Any(a => e.Model.ChiralAtomsCarbon.Contains(a)) },
                { "HasAll_BadChirality_Other", e => e.Result.ChiralityMismatchCount > 0 && e.HasAll_Chirality() && e.Result.ChiralityMismatches.Keys.Any(a => 
                         !e.Model.ChiralAtomsCarbon.Contains(a) && !e.Model.ChiralAtomsMetal.Contains(a) && !e.Model.ChiralAtomsNonSingleBond.Contains(a) && !e.Model.ChiralAtomsPlanar.Contains(a))},
            
                { "HasAll_WrongBonds", e => e.Result.WrongBondCount > 0 && e.HasAll() },

                { "HasAll_Substitutions", e => e.Result.SubstitutionCount > 0 && e.HasAll() },
                { "HasAll_Foreign", e => e.Result.ForeignAtomCount > 0 && e.HasAll() },
                //{ "HasAll_NameMismatch", e => e.Result.NameMismatchCount > 0 && e.HasAll() },
                { "HasAll_ZeroRmsd", e => e.Result.ModelRmsd < 0.0000001 && e.HasAll() },

                { "HasAll_NameMismatch", e => e.NamingAnalysis.HasNamingIssue.Count > 0 && e.HasAll() },
                { "HasAll_NameMismatch_ChargeEquiv", e => e.NamingAnalysis.HasNamingIssueWithEquivalence.Count > 0 && e.HasAll() },
                { "HasAll_NameMismatch_ChargeEquivIgnoreBondType", e => e.NamingAnalysis.HasNamingIssueWithEquivalenceIgnoreBonds.Count > 0 && e.HasAll() },
                { "HasAll_NameMismatch_NonChargeEquiv", e => e.NamingAnalysis.HasNamingIssueWith_OUT_Equivalence.Count > 0 && e.HasAll() },
                { "HasAll_NameMismatch_NonChargeEquivIgnoreBondType", e => e.NamingAnalysis.HasNamingIssueWith_OUT_EquivalenceIgnoreBonds.Count > 0 && e.HasAll() },

                { "Has_AlternateLocation", e => e.AlternateLocationCount > 0 },

                { "Has_NamingIssue_Duplicates", e => e.NamingAnalysis != null && e.NamingAnalysis.DuplicateNames.Length > 0 },
                { "Has_NamingIssue_NonIsomorphic", e => e.NamingAnalysis != null && e.NamingAnalysis.HasNonIsomorphicNaming },
                { "Has_NamingIssue_NonBoundarySubstitutionOrForeign", e => e.NamingAnalysis != null && e.NamingAnalysis.NonBoundarySubstitutionAndForeignAtoms.Length > 0 },

                //{ "Has_DuplicateNames", e => e.NamingAnalysis != null && e.NamingAnalysis.DuplicateNames.Length > 0 },
                //{ "Has_NonIsomorphicNaming", e => e.NamingAnalysis != null && e.NamingAnalysis.HasNonIsomorphicNaming },
                //{ "Has_NonBoundarySubstitutionOrForeign", e => e.NamingAnalysis != null && e.NamingAnalysis.NonBoundarySubstitutionAndForeignAtoms.Length > 0 },

                { "Analyzed", e => e.IsAnalyzed },
                { "NotAnalyzed", e => !e.IsAnalyzed }
            };
            Flags = FlagMaps.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        }
        

        //{ "CanDoChiralityAnalysis", e => HasAll() && Result.WrongBondCount == 0; }

        //{ "CorrectMotives", e => Result.ChiralityMismatchCount == 0 && Result.MissingAtomCount == 0; }
    }
}
