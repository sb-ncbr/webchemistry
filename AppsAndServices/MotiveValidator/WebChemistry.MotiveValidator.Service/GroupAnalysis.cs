namespace WebChemistry.MotiveValidator.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.MdlMol;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;
    using WebChemistry.MotiveValidator.DataModel;
    using WebChemistry.SiteBinder.Core;

    class GroupAnalysis
    {
        public ValidatorService Service { get; set; }
        public MotiveModel Model { get; set; }
        public MotiveEntry[] Entries { get; set; }

        public Dictionary<string, int> Summary { get; private set; }
        public MotiveEntry[] AnalyzedEntries { get; private set; }
        public Dictionary<int, string> ModelNames { get; private set; }
        public Dictionary<int, string> ModelAtomTypes { get; private set; }
        public Dictionary<string, int> ModelBonds { get; private set; }
        public string[] StructureNames { get; private set; }
        public string PairingMatrix { get; private set; }
        public int AnalyzeErrors { get; set; }

        public bool IsAnalyzed { get; private set; }
        
        public static string Convert(IAtom atom)
        {
            return string.Format("{0} {1} {2}", atom.PdbName(), atom.ElementSymbol, atom.Id);
        }
        
        ////static string Convert(MdlMolStereoParity parity)
        ////{
        ////    switch (parity)
        ////    {
        ////        case MdlMolStereoParity.NotStereo: return "None";
        ////        case MdlMolStereoParity.Odd: return "Odd";
        ////        case MdlMolStereoParity.Even: return "Even";
        ////        case MdlMolStereoParity.EitherOrUnmarked: return "Either";
        ////    }
        ////    return "Unknown";

        ////}

        static Dictionary<int, string> Convert(Dictionary<IAtom, IAtom> atoms)
        {
            return atoms.OrderBy(a => a.Key.Id).ToDictionary(a => a.Key.Id, a => Convert(a.Value));
        }

        static Dictionary<int, string> ConvertForeign(Dictionary<IAtom, IAtom> atoms)
        {
            return atoms.OrderBy(a => a.Key.Id).ToDictionary(a => a.Key.Id, a => string.Format("{0}, {1} {2} {3}", Convert(a.Value), a.Value.PdbResidueName(), a.Value.PdbResidueSequenceNumber(), a.Value.PdbChainIdentifier()));
        }
        
        void ComputeSummary()
        {
            var summary = AnalysisFlags.Flags.ToDictionary(f => f, _ => 0, StringComparer.Ordinal);

            foreach (var e in AnalyzedEntries)
            {
                var flags = e.GetFlags();
                for (int i = 0; i < flags.Length; i++)
                {
                    summary[flags[i]]++;
                }
            }
            Summary = summary;
        }

        void CreatePairingMatrix(Dictionary<string, MotiveEntry> map)
        {
            var exp = new MotiveEntry { Motive = Model.Structure, ModelToMotivePairing = Model.Structure.Atoms.ToDictionary(a => a, a => a) }.ToSingletonArray()
                .Concat(AnalyzedEntries.Where(e => e.Result.State == ValidationResultEntryState.Validated).OrderBy(e => e.Motive.Id))
                .GetExporter()
                .AddExportableColumn(e => e.Motive.Id, ColumnType.String, "Id");

            foreach (var _a in Model.Structure.Atoms.OrderBy(a => a.Id))
            {
                var atom = _a;
                exp.AddExportableColumn(e =>
                    {
                        IAtom b;
                        e.ModelToMotivePairing.TryGetValue(atom, out b);
                        return b == null ? "-" : b.Id.ToString();
                    }, ColumnType.Number, atom.PdbName());
            }

            exp.AddExportableColumn(_ => "", ColumnType.Number, "-");

            foreach (var _a in Model.Structure.Atoms.OrderBy(a => a.Id))
            {
                var atom = _a;
                exp.AddExportableColumn(e =>
                {
                    IAtom b;
                    e.ModelToMotivePairing.TryGetValue(atom, out b);
                    return b == null ? "-" : b.PdbName();
                }, ColumnType.String, atom.PdbName());
            }

            PairingMatrix = exp.ToCsvString();
        }
        
        static WrongBondInfo MakeWrongBondInfo(IBond model, IAtom a, IAtom b)
        {
            return new WrongBondInfo
            {
                ModelFrom = Math.Min(model.A.Id, model.B.Id),
                ModelTo = Math.Max(model.A.Id, model.B.Id),
                Type = "Missing",
                MotiveAtoms = string.Format("{0}-{1}", Convert(a), Convert(b))
            };
        }

        static WrongBondInfo MakeWrongBondInfo(IBond model, IBond matched)
        {
            return new WrongBondInfo
            {
                ModelFrom = Math.Min(model.A.Id, model.B.Id),
                ModelTo = Math.Max(model.A.Id, model.B.Id),
                Type = ((int)matched.Type).ToString(),
                MotiveAtoms = string.Format("{0}-{1}", Convert(matched.A), Convert(matched.B))
            };
        }

        static WrongBondInfo MakeExtraWrongBondInfo(IBond matched, IAtom a, IAtom b)
        {
            return new WrongBondInfo
            {
                ModelFrom = Math.Min(a.Id, b.Id),
                ModelTo = Math.Max(a.Id, b.Id),
                Type = "Extra",
                MotiveAtoms = string.Format("{0}-{1}", Convert(matched.A), Convert(matched.B))
            };
        }
        
        //static void ThrowBondDiscrepancy()
        //{
        //    throw new InvalidOperationException("Internal/OpenBabel bond discrepancy.");
        //}

        static void WarnBondDiscrepancy(IAtom a, IAtom b, bool isExtra, List<string> warnings)
        {
            var warning = string.Format("{0} bond {1}-{2} ({3} ang)",
                isExtra ? "extra" : "missing", 
                Convert(a),
                Convert(b),
                a.InvariantPosition.DistanceTo(b.InvariantPosition).ToStringInvariant("0.000"));
            warnings.Add(warning);
            //throw new InvalidOperationException("Internal/OpenBabel bond discrepancy.");
        }

        static void WarnBondDiscrepancy(List<string> warnings)
        {
            var warning = "different bond count";
            warnings.Add(warning);
        }

        static void AddDisrepancyWarnings(MotiveEntry e, List<string> warningList)
        {
            if (warningList.Count == 0) return;

            e.HasBondDiscrepancy = true;
            string[] warnings;
            if (!e.Model.MotiveWarnings.TryGetValue(e.Motive.Id, out warnings))
            {
                warnings = new string[0];
            }
            e.Model.MotiveWarnings[e.Motive.Id] = warnings.Concat(new[] 
            {
                string.Format("{0}: Model/Input Motif bond discrepancy: {1}. Likely causes: 1) Degenerate motif (i.e. misplaced atoms), 2) Wrong CONECT record(s) in the parent PDB (if PDB format was used).",
                    e.Model.Name.ToUpperInvariant(),
                    string.Join(", ", warningList))
            }).ToArray();
        }

        static bool VerifyBondCounts(Dictionary<string, int> modelCounts, Dictionary<string, int> motiveCounts)
        {
            foreach (var p in modelCounts)
            {
                int c;
                if (!motiveCounts.TryGetValue(p.Key, out c)) return false;
                if (c != p.Value) return false;
            }

            foreach (var p in motiveCounts)
            {
                int c;
                if (!modelCounts.TryGetValue(p.Key, out c)) return false;
                if (c != p.Value) return false;
            }

            return true;
        }

        static WrongBondInfo[] CheckBonds(MotiveModel mm, MotiveEntry entry)
        {
            var model = mm.Structure;
            var matched = entry.Matched;
            var matchedAndMoved = entry.MatchedWithMovedBonds;
            
            List<string> discrepancyWarnings = new List<string>();

            // Check if the bond count matches.
            if (matched.Bonds.Count != matchedAndMoved.Bonds.Count)
            {
                WarnBondDiscrepancy(discrepancyWarnings);
            }

            // Check if there is some extra bond.
            foreach (var bond in matchedAndMoved.Bonds)
            {
                if (!matched.Bonds.Contains(bond.A, bond.B))
                {
                    WarnBondDiscrepancy(bond.A, bond.B, false, discrepancyWarnings);
                }
            }

            // Check if there is some missing bond.
            foreach (var bond in matched.Bonds)
            {
                if (!matchedAndMoved.Bonds.Contains(bond.A, bond.B))
                {
                    WarnBondDiscrepancy(bond.A, bond.B, true, discrepancyWarnings);
                }
            }

            AddDisrepancyWarnings(entry, discrepancyWarnings);

            var ret = new List<WrongBondInfo>();

            HashSet<IAtom> okMotiveAtoms = new HashSet<IAtom>();

            // check bond counts.
            foreach (var p in entry.ModelToMotivePairing)
            {
                var a = p.Key;
                var b = p.Value;

                var modelBonds = mm.Structure.Bonds[a]
                    .Where(x => entry.ModelToMotivePairing.ContainsKey(x.B))
                    .GroupBy(x => x.B.ElementSymbol.ToString() + (int)x.Type)
                    .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

                var motiveBonds = matchedAndMoved.Bonds[b]
                    .GroupBy(x => x.B.ElementSymbol.ToString() + (int)x.Type)
                    .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

                if (VerifyBondCounts(modelBonds, motiveBonds))
                {
                    okMotiveAtoms.Add(b);
                }
            }

            // check missing and wrong types
            foreach (var b in model.Bonds)
            {
                IAtom x, y;
                entry.ModelToMotivePairing.TryGetValue(b.A, out x);
                entry.ModelToMotivePairing.TryGetValue(b.B, out y);

                if (x != null && y != null)
                {
                    if (okMotiveAtoms.Contains(x) || okMotiveAtoms.Contains(y)) continue;

                    var bond = matched.Bonds[x, y];
                    if (bond == null)
                    {
                        ret.Add(MakeWrongBondInfo(b, x, y));
                    }
                    else
                    {
                        var expected = mm.GetBondType(b.A, b.B);
                        var got = matchedAndMoved.Bonds[x, y];

                        if (expected == null && got == null) continue;

                        if (got == null) ret.Add(MakeWrongBondInfo(b, x, y));
                        else if (expected == null) ret.Add(MakeExtraWrongBondInfo(Bond.Create(x, y, got.Type), b.A, b.B));
                        else if (expected.Value != got.Type) ret.Add(MakeWrongBondInfo(b, Bond.Create(x, y, got.Type) ));
                    }
                }
            }

            // check for extra bonds.
            foreach (var b in matchedAndMoved.Bonds)
            {
                var x = entry.MotiveToModelPairing[b.A];
                var y = entry.MotiveToModelPairing[b.B];

                var type = mm.GetBondType(x, y);
                if (type == null)
                {
                    ret.Add(MakeExtraWrongBondInfo(Bond.Create(b.A, b.B, b.Type), x, y));
                }
            }

            ////// correct swaps
            ////HashSet<WrongBondInfo> corrections = new HashSet<WrongBondInfo>();
            ////ret.GroupBy(b => b.ModelFrom)
            ////    .ForEach(g =>
            ////    {
            ////        if (g.Count() == 2) g.ForEach(b => corrections.Add(b));
            ////    });
            
            return ret.ToArray();
        }

        public static string MakeMotifMol(string motifPdbString, MotiveEntry entry)
        {
            return entry.Motive.ToMolString();

        }

        static void CopyBondsFromMatched(MotiveEntry entry)
        {
            var model = entry.Model;
            var matched = entry.Matched;
            var pairing = entry.MotiveToModelPairing;
            var types = matched.Bonds.Select(b => new { Bond = b, Type = model.GetBondType(pairing[b.A], pairing[b.B]) });
            var newBonds = BondCollection.Create(types.Where(t => t.Type != null).Select(b => Bond.Create(b.Bond.A, b.Bond.B, b.Type.Value)));
            entry.MatchedWithMovedBonds = Structure.Create(matched.Id, matched.Atoms, newBonds).AsPdbStructure(null);
        }

        public static AnalysisResult Analyze(MotiveEntry entry)
        {
            var motifPdbString = entry.Motive.ToPdbString(sortAtoms: false);
            var motifMol = MakeMotifMol(motifPdbString, entry);

            string molString = entry.MatchedWithMovedBonds.ToMolString();           
            var bonds = CheckBonds(entry.Model, entry);

            return new AnalysisResult
            {
                DifferentChirality = ChiralityAnalyzer.GetAtomsWithDifferentChirality(entry),
                //MolString = molString,
                //MotiveMolString = motifMol,
                MotivePdbString = motifPdbString,
                WrongBonds = bonds,
                RingCounts = entry.Matched.Rings().GroupBy(r => r.Fingerprint).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal)
            };
        }


        static readonly HashSet<ElementSymbol>[] SubstitutionClasses = new[]
        {
            new HashSet<ElementSymbol> { ElementSymbols.C, ElementSymbols.O, ElementSymbols.N, ElementSymbols.S, ElementSymbols.P },
            new HashSet<ElementSymbol> { ElementSymbols.S, ElementSymbols.Cl },
            new HashSet<ElementSymbol> { ElementSymbols.F, ElementSymbols.O },
            new HashSet<ElementSymbol> { ElementSymbols.Cl, ElementSymbols.N },
        };
        
        static bool CanSubstitute(IAtom a, IAtom b)
        {
            for (int i = 0; i < SubstitutionClasses.Length; i++)
            {
                var cls = SubstitutionClasses[i];
                if (cls.Contains(a.ElementSymbol) && cls.Contains(b.ElementSymbol)) return true;
            }
            ////if (a.ElementSymbol == ElementSymbols.O) return b.ElementSymbol == ElementSymbols.N;
            ////else if (a.ElementSymbol == ElementSymbols.N) return b.ElementSymbol == ElementSymbols.O;
            return a.ElementSymbol == b.ElementSymbol;
        }
        
        static IAtom TryFindSubstitute(IAtom a, IList<IBond> other, Dictionary<IAtom, IAtom> inverse)
        {
            IAtom closest = null;
            double dist = double.MaxValue;

            for (int i = 0; i < other.Count; i++)
            {
                var b = other[i].B;
                if (CanSubstitute(a, b) && !inverse.ContainsKey(b))
                {
                    var d = a.Position.DistanceToSquared(b.Position);
                    if (d < dist)
                    {
                        dist = d;
                        closest = b;
                    }
                }
            }

            return closest;
        }

        static void FindPairing(IStructure model, IStructure motive, PairwiseMatching<IAtom> matching, 
            out Dictionary<IAtom, IAtom> pairing,
            out Dictionary<IAtom, IAtom> inversePairing,
            out Dictionary<IAtom, IAtom> substitutions)
        {
            var modelToMotive = matching.PivotOrdering.Zip(matching.OtherOrdering, (a, b) => new { A = a.Vertex, B = b.Vertex }).ToDictionary(e => e.A, e => e.B);
            var motiveToModel = modelToMotive.ToDictionary(e => e.Value, e => e.Key);
            var subst = new Dictionary<IAtom, IAtom>();

            var modelBonds = model.Bonds;
            var motiveBonds = motive.Bonds;

            bool findSubstitution = true;
            while (findSubstitution)
            {
                findSubstitution = false;
                foreach (var pair in modelToMotive)
                {
                    foreach (var bond in modelBonds[pair.Key])
                    {
                        var a = bond.B;
                        if (!modelToMotive.ContainsKey(a))
                        {
                            var b = TryFindSubstitute(a, motiveBonds[pair.Value], motiveToModel);
                            if (b != null)
                            {
                                modelToMotive.Add(a, b);
                                motiveToModel.Add(b, a);
                                if (a.ElementSymbol != b.ElementSymbol) subst.Add(a, b);
                                findSubstitution = true;
                                break;
                            }
                        }
                    }
                    if (findSubstitution) break;
                }

                if (findSubstitution && modelToMotive.Count == model.Atoms.Count) break;

            }

            pairing = modelToMotive;
            inversePairing = motiveToModel;
            substitutions = subst;
        }

        static void AnalyzeEntry(MotiveEntry entry, PairwiseMatching<IAtom> matching)
        {
            var model = entry.Model.Structure;
            var motive = entry.Motive;

            Dictionary<IAtom, IAtom> pairing, inversePairing, substitutions;
            FindPairing(entry.Model.Structure, entry.Motive, matching, out pairing, out inversePairing, out substitutions);
            entry.ModelToMotivePairing = pairing;
            entry.MotiveToModelPairing = inversePairing;

            var orderedPivot = pairing.Keys.OrderBy(a => a.Id).ToArray();
            entry.ModelPdbString = model.InducedSubstructure("", orderedPivot, cloneAtoms: false).ToPdbString(sortAtoms: false);
            entry.Matched = entry.Motive.InducedSubstructure(entry.Motive.Id, orderedPivot.Select(v => pairing[v]), cloneAtoms: false);
            // !!
            CopyBondsFromMatched(entry);

            entry.MatchedPdbString = entry.MatchedWithMovedBonds.ToPdbString(sortAtoms: false);

            entry.Analysis = Analyze(entry);

            var missingAtoms = model.Atoms.Where(a => !entry.ModelToMotivePairing.ContainsKey(a)).ToArray();

            var residueGroups = entry.Matched.Atoms.GroupBy(a => a.ResidueIdentifier()).Where(g => g.First().PdbResidueName().EqualOrdinalIgnoreCase(entry.Model.Name)).ToArray();
            if (residueGroups.Length == 0) residueGroups = entry.Matched.Atoms.GroupBy(a => a.ResidueIdentifier()).ToArray();

            var mainResidue = residueGroups.MaxBy(g => g.Count())[0];
            var mainResidueId = mainResidue.Key;

            var foreignAtoms = entry.ModelToMotivePairing
                .Where(e => e.Value.ResidueIdentifier() != mainResidueId)
                .ToDictionary(e => e.Key, e => e.Value);

            entry.Analysis.ForeignAtoms = foreignAtoms;
            entry.Analysis.Substitutions = substitutions;

            Dictionary<string, int> missingRings = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var ring in entry.Model.RingCounts)
            {
                int count;
                entry.Analysis.RingCounts.TryGetValue(ring.Key, out count);

                if (count != ring.Value)
                {
                    missingRings.Add(ring.Key, ring.Value - count);
                }
            }
            entry.Analysis.RingCounts.Where(e => !entry.Model.RingCounts.ContainsKey(e.Key)).ForEach(e => missingRings.Add(e.Key, -e.Value));

            var mismatchedStereoParityAtoms = entry
                .Analysis.DifferentChirality
                .ToDictionary(a => inversePairing[a], a => a);

            ////var nearPlanarAtoms = entry.Model.NearPlanarAtoms;
            ////var planarityWarnings = new List<string>();
            ////var planarityWarningModelAtoms = new List<IAtom>();
            ////foreach (var p in mismatchedStereoParityAtoms)
            ////{
            ////    if (nearPlanarAtoms.Contains(p.Key))
            ////    {
            ////        planarityWarnings.Add(string.Format("Atom '{0}' is planar. The chirality error might not be significant.", p.Key.PdbName()));
            ////        planarityWarningModelAtoms.Add(p.Key);
            ////    }
            ////}
            ////if (planarityWarnings.Count > 0)
            ////{
            ////    entry.Model.AddWarnings(entry.Motive.Id, planarityWarnings);
            ////}

            var nameMismatches = entry.ModelToMotivePairing
                .Where(e => e.Value.ResidueIdentifier() == mainResidueId && !e.Key.PdbName().EqualOrdinalIgnoreCase(e.Value.PdbName()))
                .Where(e => !foreignAtoms.ContainsKey(e.Key) && !substitutions.ContainsKey(e.Key))
                .ToDictionary(e => e.Key, e => e.Value);

            entry.NamingAnalysis = ValidatorService.CheckNaming(entry.Model.NamingHelper, entry.Motive, mainResidue.ToArray(), entry.Matched);

            entry.NamingAnalysis.HasNamingIssue = nameMismatches.Values.OrderBy(v => v.Id).ToHashSet();
            entry.NamingAnalysis.HasNamingIssueWithEquivalence = ValidatorService.FilterNamingEquivalenceClass(entry, nameMismatches, entry.Model.AtomNamingEquivalence[AtomNamingEquivalenceType.Charge]);
            entry.NamingAnalysis.HasNamingIssueWithEquivalenceIgnoreBonds = ValidatorService.FilterNamingEquivalenceClass(entry, nameMismatches, entry.Model.AtomNamingEquivalence[AtomNamingEquivalenceType.ChargeIgnoreBondTypes]);
            //var hasNamingIssueSpecialSet = entry.NamingAnalysis.HasNamingIssueWithEquivalence.Concat(entry.NamingAnalysis.HasNamingIssueWithEquivalenceIgnoreBonds).ToHashSet();
            entry.NamingAnalysis.HasNamingIssueWith_OUT_Equivalence = entry.NamingAnalysis
                .HasNamingIssue
                .Where(a => !entry.NamingAnalysis.HasNamingIssueWithEquivalence.Contains(a)).ToHashSet();

            entry.NamingAnalysis.HasNamingIssueWith_OUT_EquivalenceIgnoreBonds = entry.NamingAnalysis
                .HasNamingIssue
                .Where(a => !entry.NamingAnalysis.HasNamingIssueWithEquivalenceIgnoreBonds.Contains(a)).ToHashSet();

            entry.Result = new DataModel.ValidationResultEntry
            {
                ModelName = entry.Model.Name,
                Id = entry.Motive.Id,
                State = ValidationResultEntryState.Validated,

                MainResidue = entry.Motive.PdbResidues().FromIdentifier(mainResidueId).ToString(),
                Residues = entry.Matched.PdbResidues().OrderBy(r => r).Select(r => r.ToString()).ToArray(),
                ResidueCount = entry.Matched.PdbResidues().Count,

                MissingAtomCount = missingAtoms.Length,
                MissingRingCount = missingRings.Sum(r => Math.Max(r.Value, 0)),
                NameMismatchCount = nameMismatches.Count,
                ForeignAtomCount = foreignAtoms.Count,
                ChiralityMismatchCount = mismatchedStereoParityAtoms.Count,
                SubstitutionCount = substitutions.Count,
                WrongBondCount = entry.Analysis.WrongBonds.Length,
                HasBondDiscrepancy = entry.HasBondDiscrepancy,
                UnmatchedMotiveAtomCount = entry.Motive.Atoms.Count - entry.Matched.Atoms.Count,
                //PlanarAtomsWithWrongChiralityCount = planarityWarningModelAtoms.Count,
                
                MissingAtoms = missingAtoms.OrderBy(a => a.Id).Select(a => a.Id).ToArray(),
                MissingRings = String.Join("; ", missingRings.Select(e => e.Key + "*" + e.Value)),

                NameMismatches = Convert(nameMismatches),
                NameMismatchFlags = ValidatorService.GetNameMismatchFlags(entry),
                ForeignAtoms = ConvertForeign(foreignAtoms),
                Substitutions = Convert(substitutions),

                ChiralityMismatches = Convert(mismatchedStereoParityAtoms),
                WrongBonds = entry.Analysis.WrongBonds,
                //PlanarAtomsWithWrongChirality = planarityWarningModelAtoms.Select(a => a.Id).ToArray(),

                DuplicateNames = entry.NamingAnalysis.DuplicateNames.Select(g => g.Select(Convert).ToArray()).ToArray(),
                NonBoundarySubstitutionAndForeignAtoms = entry.NamingAnalysis.NonBoundarySubstitutionAndForeignAtoms.Select(Convert).ToArray(),
                UnmatchedModelNames = entry.NamingAnalysis.UnmatchedNames,

                AlternateLocationCount = entry.AlternateLocationCount,
                ModelRmsd = entry.Rmsd
            };
            entry.IsAnalyzed = true;

            // must be last
            entry.Result.Flags = entry.GetFlags();
        }

        static void NonValidatedEntry(MotiveEntry entry, ValidationResultEntryState state)
        {
            entry.Result = new DataModel.ValidationResultEntry
            {
                ModelName = entry.Model.Name,
                Id = entry.Motive.Id,
                State = state,

                Residues = entry.Motive.PdbResidues().OrderBy(r => r).Select(r => r.ToString()).ToArray(),
                ResidueCount = entry.Motive.PdbResidues().Count,

                MissingAtomCount = 0,
                MissingRingCount = 0,
                NameMismatchCount = 0,
                ForeignAtomCount = 0,
                ChiralityMismatchCount = 0,
                SubstitutionCount = 0,
                WrongBondCount = 0,
                HasBondDiscrepancy = false,
                UnmatchedMotiveAtomCount = 0,
                //PlanarAtomsWithWrongChiralityCount = 0,

                MissingAtoms = new int[0],
                MissingRings = "",

                NameMismatches = new Dictionary<int,string>(),
                NameMismatchFlags = new Dictionary<int, string[]>(),
                ForeignAtoms = new Dictionary<int,string>(),
                Substitutions = new Dictionary<int,string>(),

                ChiralityMismatches = new Dictionary<int,string>(),
                WrongBonds = new WrongBondInfo[0],
                //PlanarAtomsWithWrongChirality = new int[0],

                AlternateLocationCount = 0,
                ModelRmsd = 0
            };
            entry.IsAnalyzed = true;
            entry.NamingAnalysis = NamingAnalysisResult.Empty;
            // must be last
            entry.Result.Flags = entry.GetFlags();
        }
        
        public void Run(int maxParallelism = 8, bool showLog = true)
        {
            try
            {
                var motiveMap = Entries.ToDictionary(e => e.Motive.Id, StringComparer.Ordinal);

                var modelGraph = MatchGraph.Create(Model.Structure, onlySelection: false, ignoreHydrogens: true);

                if (!modelGraph.IsConnected())
                {
                    Service.LogError(Model.Structure.Id, "Model graph is not connected and will not be analyzed. Likely cause: Misplaced and/or missing atoms.");
                    return;
                }

                List<MatchGraph<IAtom>> motiveGraphs = new List<MatchGraph<IAtom>>() { modelGraph };

                HashSet<string> added = new HashSet<string>() { Model.Structure.Id };
                foreach (var e in Entries)
                {
                    if (!added.Add(e.Motive.Id)) continue;

                    try
                    {
                        var graph = MatchGraph.Create(e.Motive, onlySelection: false, ignoreHydrogens: true);

                        if (graph.IsConnected()) motiveGraphs.Add(graph);
                        else
                        {
                            //Model.MotiveErrors[e.Motive.Id] = "Not connected. Likely cause: Misplaced and/or missing atoms.";
                            //if (showLog) Service.ShowError("{0}: Not connected.", e.Motive.Id);
                            NonValidatedEntry(e, ValidationResultEntryState.Degenerate);
                        }
                    }
                    catch (Exception ex)
                    {
                        Model.MotiveErrors[e.Motive.Id] = ex.Message;
                        if (showLog) Service.ShowError("{0}: {1}", e.Motive.Id, ex.Message);
                    }
                }


                if (showLog) Service.Log("Superimposing {0}.", Model.Name);
                Service.UpdateProgress(string.Format("Superimposing {0}...", Model.Name.ToUpperInvariant()));
                var progress = new ComputationProgress();
                progress.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName != "Current") return;
                        Service.UpdateProgress(string.Format("Superimposing {0}...", Model.Name.ToUpperInvariant()), progress.Current, progress.Length);
                    };
                var matching = MultipleMatching<IAtom>.Find(motiveGraphs, PivotType.SpecificStructure, pivotIndex: 0, progress: progress);

                if (showLog) Service.Log("Analyzing {0}.", Model.Name);
                var model = Model.Structure;
                int analyzedCount = 0;
                Service.UpdateProgress(string.Format("Analyzing {0}...", Model.Name.ToUpperInvariant()));
                object sync = new object();
                //foreach (var m in matching.MatchingsList)
                Parallel.ForEach(matching.MatchingsList, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, m =>
                {
                    if (m.Other.Token == model.Id) return;

                    var entry = motiveMap[m.Other.Token];
                    if (m.Size == 0)
                    {
                        //var msg = "Zero atoms matched. This is probably caused by a degenerate motif.";
                        //if (showLog) Service.ShowError("{0}: {1}", entry.Motive.Id, msg);
                        //Model.MotiveErrors[entry.Motive.Id] = msg;
                        //AnalyzeErrors++;
                        NonValidatedEntry(entry, ValidationResultEntryState.Degenerate);
                        return; // continue;
                    }

                    var transform = OptimalTransformation.Find(m.PivotOrdering, m.OtherOrdering, a => a.Vertex.Position);
                    transform.Apply(entry.Motive);
                    entry.Rmsd = Math.Round(m.Rmsd, 3);

                    try
                    {
                        AnalyzeEntry(entry, m);
                    }
                    catch (Exception e)
                    {
                        lock (sync)
                        {
                            if (showLog) Service.ShowError("{0}: {1}", entry.Motive.Id, e.Message);
                            Model.MotiveErrors[entry.Motive.Id] = e.Message;
                            AnalyzeErrors++;
                        }
                    }
                    lock (sync)
                    {
                        analyzedCount++;
                        Service.UpdateProgress(string.Format("Analyzing {0}...", Model.Name.ToUpperInvariant()), analyzedCount, motiveGraphs.Count);
                    }
                });
                Service.UpdateProgress(string.Format("Analyzing {0}...", Model.Name.ToUpperInvariant()));

                ModelNames = Model.Structure.Atoms.ToDictionary(a => a.Id, a => a.PdbName());
                ModelAtomTypes = Model.Structure.Atoms.ToDictionary(a => a.Id, a => a.ElementSymbol.ToString());

                AnalyzedEntries = Entries.Where(e => e.IsAnalyzed).ToArray();
                
                StructureNames = Entries.Select(e => new string(e.Motive.Id.TakeWhile(c => c != '_').ToArray())).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
                ModelBonds = Model.Structure
                    .Bonds
                    .ToDictionary(b => string.Format("{0}-{1}",
                        Math.Min(b.A.Id, b.B.Id),
                        Math.Max(b.A.Id, b.B.Id)),
                        b => (int)b.Type);
                CreatePairingMatrix(motiveMap);
                ComputeSummary();
                IsAnalyzed = true;
            }
            catch (Exception e)
            {
                Service.LogError(Model.Structure.Id, "This motif ({1}) will not be analyzed: {0}", e.Message, Model.Name);
                Entries.ForEach(x => x.IsAnalyzed = false);
            }
        }
    }
}
