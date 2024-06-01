namespace WebChemistry.MotiveValidator.Service
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using WebChemistry.MotiveValidator.DataModel;

    class AnalysisResult
    {
        //public bool IsChiral { get; set; }
        //public Dictionary<IAtom, MdlMolStereoParity> Chirality { get; set; }        
        public List<IAtom> DifferentChirality { get; set; }
        public string MotivePdbString { get; set; }
        public WrongBondInfo[] WrongBonds { get; set; }
        public Dictionary<string, int> RingCounts { get; set; }
        public Dictionary<IAtom, IAtom> ForeignAtoms { get; set; }
        public Dictionary<IAtom, IAtom> Substitutions { get; set; }
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum CoordinateSourceTypes
    {
        Default = 0,
        CifModel,
        CifIdeal
    }

    static class AtomNamingEquivalenceType
    {
        public static readonly string Charge = "Charge";
        public static readonly string ChargeIgnoreBondTypes = "ChargeIgnoreBondTypes";
    }

    class ModelMetaInfo
    {
        public string LongName;
        public string ChargeNamingEquivalence;
        public string ChargeNamingEquivalenceIgnoreBondTypes;
    }

    class MotiveModel
    {
        public string Name { get; private set; }
        public string LongName { get; set; }
        public string Formula { get; private set; }

        public CoordinateSourceTypes SourceType { get; private set; }
        public IStructure Structure { get; private set; }
        public IAtom[] ChiralAtoms { get; private set; }

        public Dictionary<string, Dictionary<string, string>> AtomNamingEquivalence { get; private set; }

        public HashSet<IAtom> NearPlanarAtoms { get; private set; }

        public Dictionary<string, int[]> ChiralAtomsInfo { get; private set; }
        public HashSet<int> ChiralAtomsPlanar { get; private set; }
        public HashSet<int> ChiralAtomsCarbon { get; private set; }
        public HashSet<int> ChiralAtomsMetal { get; private set; }
        public HashSet<int> ChiralAtomsNonSingleBond { get; private set; }

        public Dictionary<string, int> RingCounts { get; private set; }
        
        public Dictionary<string, string> MotiveErrors { get; private set; }
        public Dictionary<string, string[]> MotiveWarnings { get; private set; }

        public string PdbString { get; private set; }
        //public string MolString { get; private set; }

        public ModelNamingHelper NamingHelper { get; private set; }

        public MotiveModel Clone()
        {
            return new MotiveModel
            {
                Name = Name,
                LongName = LongName,
                Formula = Formula,

                SourceType = SourceType,
                Structure = Structure,
                ChiralAtoms = ChiralAtoms,

                AtomNamingEquivalence = AtomNamingEquivalence,

                ChiralAtomsInfo = ChiralAtomsInfo,
                ChiralAtomsPlanar = ChiralAtomsPlanar,
                ChiralAtomsCarbon = ChiralAtomsCarbon,
                ChiralAtomsMetal = ChiralAtomsMetal,
                ChiralAtomsNonSingleBond = ChiralAtomsNonSingleBond,

                NearPlanarAtoms = NearPlanarAtoms,
                RingCounts = RingCounts,

                PdbString = PdbString,
                //MolString = MolString,

                MotiveErrors = MotiveErrors.ToDictionary(e => e.Key, e => e.Value),
                MotiveWarnings = MotiveWarnings.ToDictionary(e => e.Key, e => e.Value.ToArray()),

                NamingHelper = NamingHelper
            };
        }

        public void AddWarnings(string key, IEnumerable<string> xs)
        {
            if (MotiveWarnings.ContainsKey(key))
            {
                MotiveWarnings[key] = MotiveWarnings[key].Concat(xs).ToArray();
            }
            else MotiveWarnings[key] = xs.ToArray();
        }

        public void AddWarnings(string key, IEnumerable<StructureReaderWarning> ws)
        {
            var toAdd = new List<string>();
            foreach (var w in ws)
            {
                if (w is AtomStructureReaderWarning)
                {
                    if ((w as AtomStructureReaderWarning).Atom.PdbName().EqualOrdinalIgnoreCase(Name)) toAdd.Add(w.ToString());
                }
                else if (w is ResidueStructureReaderWarning)
                {
                    if ((w as ResidueStructureReaderWarning).Name.EqualOrdinalIgnoreCase(Name)) toAdd.Add(w.ToString());
                }
                else toAdd.Add(w.ToString());
            }

            if (toAdd.Count == 0) return;

            if (MotiveWarnings.ContainsKey(key))
            {
                MotiveWarnings[key] = MotiveWarnings[key].Concat(toAdd).ToArray();
            }
            else MotiveWarnings[key] = toAdd.ToArray();
        }

        public void AddModelWarnings(params string[] ws)
        {
            if (ws.Length == 0) return;

            if (MotiveWarnings.ContainsKey("model"))
            {
                MotiveWarnings["model"] = MotiveWarnings["model"].Concat(ws).ToArray();
            }
            else MotiveWarnings["model"] = ws;
        }

        static double fiveDegreesInRadians = MathHelper.DegreesToRadians(5.0);
        public static bool IsPlanar(IAtom atom, IBondCollection bonds)
        {
            var xs = bonds[atom];
            if (xs.Count >= 3)
            {
                double angle = 0;

                for (int i = 0; i < xs.Count - 2; i++)
                {
                    for (int j = i + 1; j < xs.Count - 1; j++)
                    {
                        for (int k = j + 1; k < xs.Count; k++)
                        {
                            Vector3D a = xs[i].B.Position, b = xs[j].B.Position, c = xs[k].B.Position;
                            var plane = Plane3D.FromPoints(a, b, c);
                            double localAngle = Math.Max(
                                plane.GetAngleInRadians(Line3D.Create(a, atom.Position)),
                                Math.Max(plane.GetAngleInRadians(Line3D.Create(b, atom.Position)), plane.GetAngleInRadians(Line3D.Create(c, atom.Position))));

                            angle = Math.Max(angle, localAngle);
                        }
                    }
                }

                if (angle < fiveDegreesInRadians) return true;
                return false;
            }
            return false;
        }

        void AnalyzeNearPlanarAtoms()
        {
            var fiveDegreesInRadians = MathHelper.DegreesToRadians(5.0);
            NearPlanarAtoms = new HashSet<IAtom>();

            var bonds = Structure.Bonds;
            foreach (var atom in ChiralAtoms)
            {
                 if (IsPlanar(atom, bonds)) NearPlanarAtoms.Add(atom);
            }
        }

        /// <summary>
        /// get bond type using PDB ID atoms.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public BondType? GetBondType(IAtom a, IAtom b)
        {
            var n = Structure.Bonds[a, b];
            if (n != null) return n.Type;
            return null;
        }

        private MotiveModel()
        {

        }
        
        public MotiveModel(
            string name, string formula,
            IStructure structure, IAtom[] chiralAtoms, CoordinateSourceTypes sourceType)
        {
            MotiveErrors = new Dictionary<string, string>();
            MotiveWarnings = new Dictionary<string, string[]>();

            this.Name = name;
            this.Formula = formula;

            this.Structure = structure;
            this.ChiralAtoms = chiralAtoms;
            this.SourceType = sourceType;
            this.RingCounts = Structure.Rings().GroupBy(r => r.Fingerprint).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            AnalyzeNearPlanarAtoms();

            //this.MolString = structure.ToMolString();
            this.PdbString = structure.ToPdbString(sortAtoms: false);

            var bonds = structure.Bonds;
            ChiralAtomsPlanar = ChiralAtoms.Where(a => NearPlanarAtoms.Contains(a)).Select(a => a.Id).ToHashSet();
            ChiralAtomsCarbon = ChiralAtoms.Where(a => a.ElementSymbol == ElementSymbols.C).Select(a => a.Id).ToHashSet();
            ChiralAtomsMetal = ChiralAtoms.Where(a => ElementAndBondInfo.IsMetalSymbol(a.ElementSymbol)).Select(a => a.Id).ToHashSet();
            ChiralAtomsNonSingleBond = ChiralAtoms.Where(a => bonds[a].Any(b => b.Type != BondType.Single && b.Type != BondType.Metallic)).Select(a => a.Id).ToHashSet();

            AtomNamingEquivalence = new Dictionary<string, Dictionary<string, string>>();
            AtomNamingEquivalence[AtomNamingEquivalenceType.Charge] = new Dictionary<string, string>();
            AtomNamingEquivalence[AtomNamingEquivalenceType.ChargeIgnoreBondTypes] = new Dictionary<string, string>();

            this.ChiralAtomsInfo = new Dictionary<string, int[]>(StringComparer.Ordinal)
            {
                { "Planar", ChiralAtomsPlanar.OrderBy(a => a).ToArray() },
                { "Carbon", ChiralAtomsCarbon.OrderBy(a => a).ToArray() },
                { "Metal", ChiralAtomsMetal.OrderBy(a => a).ToArray() },
                { "NonSingleBond", ChiralAtomsNonSingleBond.OrderBy(a => a).ToArray() }
            };

            this.NamingHelper = new ModelNamingHelper(this);
        }
    }

    class MotiveEntry
    {
        public bool IsAnalyzed { get; set; }
        
        public MotiveModel Model { get; set; }
        public IStructure Motive { get; set; }
        //public string Id { get; set; }

        public IStructure Matched { get; set; }
        public IStructure MatchedWithMovedBonds { get; set; }

        public double Rmsd { get; set; }
        public string ModelPdbString { get; set; }
        public string MatchedPdbString { get; set; }

        public bool HasBondDiscrepancy { get; set; }

        public int AlternateLocationCount { get; set; }

        /// <summary>
        /// Key: model atom
        /// Value: motive atom
        /// </summary>
        public Dictionary<IAtom, IAtom> ModelToMotivePairing { get; set; }
        public Dictionary<IAtom, IAtom> MotiveToModelPairing { get; set; }

        public AnalysisResult Analysis { get; set; }

        public NamingAnalysisResult NamingAnalysis { get; set; }

        public ValidationResultEntry Result { get; set; }

        static string ConvertMapping(Dictionary<int, string> xs, MotiveModel model)
        {
            return string.Join(";", xs.Select(e => string.Format("{0} -> {1}", GroupAnalysis.Convert(model.Structure.Atoms.GetById(e.Key)), e.Value)));
        }

        static string ConvertBonds(WrongBondInfo[] xs, MotiveModel model)
        {
            return string.Join(";", xs.Select(b => 
                {
                    var x = model.Structure.Atoms.GetById(b.ModelFrom);
                    var y = model.Structure.Atoms.GetById(b.ModelTo);
                    var t = model.GetBondType(x, y);

                    return string.Format("{0}-{1}:{2}:{3}/{4}",
                        GroupAnalysis.Convert(x),
                        GroupAnalysis.Convert(y),
                        b.MotiveAtoms,
                        t.HasValue ? ((int)t).ToString() : "None",
                        b.Type);
                }));
        }

        public bool Missing() { return Result.MissingAtomCount > 0 || Result.MissingRingCount > 0 || Result.State != ValidationResultEntryState.Validated; }
        public bool HasAll() { return Result.State == ValidationResultEntryState.Validated && Result.MissingAtomCount == 0 && Result.MissingRingCount == 0; }

        public bool HasAll_Chirality() { return Result.State == ValidationResultEntryState.Validated && Result.MissingAtomCount == 0 && Result.MissingRingCount == 0 && Result.WrongBondCount == 0; }

        string[] flags;
        public string[] GetFlags() 
        {
            if (flags != null) return flags;
            
            var maps = AnalysisFlags.FlagMaps;
            flags = AnalysisFlags.Flags.Where(f => maps[f](this)).ToArray();
            return flags;
        }

        //public bool Missing_Atoms() { return Result.MissingAtomCount > 0 && Result.MissingRingCount == 0; }
        //public bool Missing_Rings() { return Result.MissingRingCount > 0; }
        //public bool Missing_Degenerate() { return Result.State == ValidationResultEntryState.Degenerate; }
        //public bool Missing_Disconnected() { return Result.State == ValidationResultEntryState.Disconnected; }
        //public bool Missing_WrongBonds() { return Missing() && Result.WrongBondCount > 0; }
        //public bool Missing_NameMismatch() { return Missing() && Result.NameMismatchCount > 0; }
        
        //public bool HasAll_GoodChirality() { return Result.ChiralityMismatchCount == 0 && Result.WrongBondCount == 0 && HasAll(); }
        //public bool HasAll_GoodChirality_IgnorePlanarAndNonSingleBondErrors() { return Result.ChiralityMismatchCount == 0 && Result.WrongBondCount == 0 && HasAll(); }
        
        //public bool HasAll_BadChirality() { return Result.ChiralityMismatchCount > 0 && Result.WrongBondCount == 0 && HasAll(); }
        //public bool HasAll_BadChirality_Planar() { return Result.PlanarAtomsWithWrongChiralityCount > 0 && Result.WrongBondCount == 0 && HasAll(); }
        //public bool HasAll_BadChirality_NonSingleBond() { return Result.PlanarAtomsWithWrongChiralityCount > 0 && Result.WrongBondCount == 0 && HasAll(); }
        //public bool HasAll_BadChirality_Metal() { return Result.PlanarAtomsWithWrongChiralityCount > 0 && Result.WrongBondCount == 0 && HasAll(); }
        //public bool HasAll_BadChirality_Carbon() { return Result.PlanarAtomsWithWrongChiralityCount > 0 && Result.WrongBondCount == 0 && HasAll(); }


        //public bool HasAll_WrongBonds() { return Result.WrongBondCount > 0 && HasAll(); }

        //public bool HasAll_Substitutions() { return Result.SubstitutionCount > 0 && HasAll(); }
        //public bool HasAll_Foreign() { return Result.ForeignAtomCount > 0 && HasAll(); }
        //public bool HasAll_NameMismatch() { return Result.NameMismatchCount > 0 && HasAll(); }
        //public bool HasAll_ZeroRmsd() { return Result.ModelRmsd < 0.0000001 && HasAll(); }

        //public bool Has_AlternateLocation() { return AlternateLocationCount > 0; }


        public static ListExporter GetResultExporter(IEnumerable<ValidationResultEntry> xs, Dictionary<string, MotiveModel> models, string separator = ",")
        {
            return xs.GetExporter(separator)
                .AddExportableColumn(e => e.ModelName, ColumnType.String, "ModelName", desctiption: "Residue name of the model.")
                .AddExportableColumn(e => e.Id, ColumnType.String, "Id", desctiption: "Name of the validated motif without extension.")
                .AddExportableColumn(e => EnumHelper.ToString(e.State), ColumnType.String, "State", desctiption: "Validation state of the entry.")

                .AddExportableColumn(e => e.MainResidue, ColumnType.String, "MainResidue", desctiption: "Name and number of main residue of validated motif")
                .AddExportableColumn(e => e.ResidueCount, ColumnType.Number, "ResidueCount", desctiption: "Number of residues present in validated motif file")
                .AddExportableColumn(e => string.Join(";", e.Residues), ColumnType.String, "Residues", desctiption: "List of residues present in this motif file. List entries are separated by ;.")

                .AddExportableColumn(e => e.MissingAtomCount, ColumnType.Number, "MissingAtomCount", desctiption: "Number of atoms that are lacking in validated motif.")
                .AddExportableColumn(e => e.MissingRingCount, ColumnType.Number, "MissingRingCount", desctiption: "Number of rings (chemical cycles) that are lacking in validated motif.")
                .AddExportableColumn(e => e.NameMismatchCount, ColumnType.Number, "NameMismatchCount", desctiption: "Number of motif's atoms that are incorrectly named in the sense of alignment with the model.")
                .AddExportableColumn(e => e.ChiralityMismatchCount, ColumnType.Number, "ChiralityMismatchCount", desctiption: "Number of chiral atoms in validated motif that have different ligand position from the model.")
                //.AddExportableColumn(e => e.PlanarAtomsWithWrongChiralityCount, ColumnType.Number, "ChiralityMismatchPlanarWarningCount", desctiption: "Number of chiral atoms in validated motif that have different ligand position from the model and are planar.")
                .AddExportableColumn(e => e.SubstitutionCount, ColumnType.Number, "SubstitutionCount", desctiption: "Number of atoms in validated motif that have been substituted with other element.")
                .AddExportableColumn(e => e.WrongBondCount, ColumnType.Number, "WrongBondCount", desctiption: "Number of wrong bonds.")
                .AddExportableColumn(e => e.HasBondDiscrepancy ? 1 : 0, ColumnType.Number, "BondDiscrepancy", "Signals whether the matched motif bond(s) differ from the model ones.")

                .AddExportableColumn(e => string.Join(";", e.MissingAtoms.Select(a => GroupAnalysis.Convert(models[e.ModelName].Structure.Atoms.GetById(a)))), ColumnType.String, "MissingAtoms", desctiption: "List of atoms from validated motif that are considered missing. Each entry of this list consists of atom name, atom element and index of atom from the model that does not have its counterpart in this motif.")
                .AddExportableColumn(e => e.MissingRings, ColumnType.String, "MissingRings", desctiption: "List of rings from validated motif that are considered missing. Each entry of this list consists of a sequence of atoms that comprise the missing ring and an integer that signifies number of missing rings of each composition (ex. CCCCO*1 means one missing ring of composition CCCCO).")
                .AddExportableColumn(e => ConvertMapping(e.NameMismatches, models[e.ModelName]), ColumnType.String, "NameMismatches", desctiption: "List of atoms from this validated motif which have been paired to a differently named atoms from the model. Each entry of this list consists of motif atom specifications (name, element and index), a separator (->) and paired model atom specifications (name, element and index).")
                .AddExportableColumn(e => ConvertMapping(e.ForeignAtoms, models[e.ModelName]), ColumnType.String, "ForeignAtoms", desctiption: "List of atoms in this validated motif that do not belong there. Each entry of this list consists of model atom specifications (name, element and index), a separator (->), paired motif atom specifications (name, element and index), separator (,) and specifications of the foreign atom (name, index and residue chain identifier).")
                .AddExportableColumn(e => ConvertMapping(e.Substitutions, models[e.ModelName]), ColumnType.String, "Substitutions", desctiption: "List of atoms with different element symbols from the model structure (ie. O substituted by N).")
                .AddExportableColumn(e => ConvertMapping(e.ChiralityMismatches, models[e.ModelName]), ColumnType.String, "ChiralityMismatches", desctiption: "List of chiral atoms from this validated motif that differ in spatial composition of connected atom(s) from paired chiral atoms of the model. Each entry of this list consists of model atom specifications (name, element and index), a separator (->) and paired motif atom specifications (name, element and index).")
                .AddExportableColumn(e => ConvertBonds(e.WrongBonds, models[e.ModelName]), ColumnType.String, "WrongBonds (ModelFrom-ModelTo:Motive:Exp/Got)", desctiption: "Wrong Bonds. Currently unused.")

                .AddExportableColumn(e => e.ModelRmsd, ColumnType.Number, "ModelRmsd", desctiption: "Root mean square deviation of atomic positions (RMSD) value of the model-motif alignment.");

        }
    }
}
