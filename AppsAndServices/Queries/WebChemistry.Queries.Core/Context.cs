namespace WebChemistry.Queries.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Queries.Core.Queries;

    /// <summary>
    /// Wraps a residue and adds a lazyly built AtomSet.
    /// </summary>
    public class ResidueWrapper
    {
        /// <summary>
        /// The original residue.
        /// </summary>
        public readonly PdbResidue Residue;

        HashTrie<IAtom> atomSet;
        /// <summary>
        /// Atom set.
        /// </summary>
        public HashTrie<IAtom> AtomSet 
        {
            get
            {
                atomSet = atomSet ?? HashTrie.Create(Residue.Atoms);
                return atomSet;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="residue"></param>
        public ResidueWrapper(PdbResidue residue)
        {
            this.Residue = residue;
        }
    }

    /// <summary>
    /// Wraps an amino chain.
    /// </summary>
    public class ChainWrapper
    {
        /// <summary>
        /// Short amino name string.
        /// </summary>
        public string Chain { get; private set; }

        /// <summary>
        /// i-th residue corresponds to i-th char in the Chain string.
        /// </summary>
        public ResidueWrapper[] Residues { get; private set; }

        ///// <summary>
        ///// Constructor.
        ///// </summary>
        ///// <param name="rs"></param>
        ///// <param name="wrappers"></param>
        //public ChainWrapper(IEnumerable<PdbResidue> rs, Dictionary<PdbResidueIdentifier, ResidueWrapper> wrappers)
        //{
        //    Residues = rs.Where(r => r.IsAmino).Select(r => wrappers[r.Identifier]).ToArray();
        //    Chain = string.Concat(Residues.Select(r => r.Residue.ShortName));
        //}

        /// <summary>
        /// Creates a chain wrapper.
        /// </summary>
        /// <param name="residues"></param>
        /// <param name="nameSelector"></param>
        public ChainWrapper(IEnumerable<ResidueWrapper> residues, Func<ResidueWrapper, string> nameSelector)
        {
            Residues = residues.ToArray();
            Chain = string.Concat(Residues.Select(r => nameSelector(r)));
        }
    }
    
    /// <summary>
    /// Query match context. Might be used by multiple queries.
    /// </summary>
    public class MotiveContext
    {
        /// <summary>
        /// Structure the query is executed against.
        /// </summary>
        public IStructure Structure { get; private set; }

        Motive structureMotive;
        /// <summary>
        /// The motive corresponding to the who structure.
        /// </summary>
        public Motive StructureMotive
        {
            get
            {
                if (structureMotive != null) return structureMotive;
                structureMotive = Motive.FromAtoms(Structure.Atoms, null, this);
                return structureMotive;
            }
        }
        
        IEnumerable<IEnumerable<PdbResidue>> MakeSecondary(Dictionary<PdbResidueIdentifier, int> map, PdbResidueCollection residues)
        {
            return residues
                .Where(r => map.ContainsKey(r.Identifier))
                .GroupBy(r => map[r.Identifier]);
        }

        /// <summary>
        /// Convert a motive to a structre.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="motiveId"></param>
        /// <param name="addBonds"></param>
        /// <param name="asPdb"></param>
        /// <returns></returns>
        public IStructure ToStructure(Motive m, string motiveId, bool addBonds, bool asPdb )
        {
            if (object.ReferenceEquals(m, structureMotive)) return Structure;

            var xs = AtomCollection.Create(m.Atoms.OrderBy(a => a.Id).Select(a => a.Clone()));
            var parent = Structure;
            IBondCollection bonds;
            if (addBonds)
            {
                bonds = BondCollection.Create(xs.SelectMany(a => parent.Bonds[a]).Where(b => xs.Contains(b.A) && xs.Contains(b.B))
                    .Distinct()
                    .Select(b => Bond.Create(xs.GetById(b.A.Id), xs.GetById(b.B.Id), b.Type)));
            }
            else bonds = BondCollection.Empty;

            var name = m.Name.HasValue ? "_" + m.Name.Value.ToString() : "";
            var mId = string.IsNullOrEmpty(motiveId) ? "" : "_" + motiveId;
            var ret = WebChemistry.Framework.Core.Structure.Create(parent.Id + mId + name, xs, bonds);
            ret.SetProperty(MotiveExtensions.IsMotiveProperty, true);
            ret.SetProperty(MotiveExtensions.MotiveParentIdProperty, parent.Id);

            var finalRet = asPdb ? ret.AsPdbStructure(parent.PdbModifiedResidues()) : ret;
            finalRet.StealAtomProperties(parent);

            if (asPdb)
            {
                var rs = finalRet.PdbResidues();
                var helices = new ReadOnlyCollection<PdbHelix>(MakeSecondary(HelixMap, rs).Select(h => PdbHelix.FromResidues(h)).ToList());
                var sheets = new ReadOnlyCollection<PdbSheet>(MakeSecondary(SheetMap, rs).Select(s => PdbSheet.FromResidues(s)).ToList());
                ret.SetProperty(PdbStructure.HelicesProperty, helices);
                ret.SetProperty(PdbStructure.SheetsProperty, sheets);
            }

            return finalRet;
        }

        Dictionary<PdbResidueIdentifier, ResidueWrapper> residues;
        /// <summary>
        /// This will remember lazily computed sets of atoms for each residue.
        /// </summary>
        public Dictionary<PdbResidueIdentifier, ResidueWrapper> Residues
        {
            get 
            {
                residues = residues ?? Structure.PdbResidues().ToDictionary(r => r.Identifier, r => new ResidueWrapper(r));
                return residues;
            }
        }

        HashSet<IAtom> ringAtoms;
        /// <summary>
        /// Atoms on rings.
        /// </summary>
        public HashSet<IAtom> RingAtoms
        {
            get
            {
                ringAtoms = ringAtoms ?? Structure.Rings().SelectMany(r => r.Atoms).ToHashSet();
                return ringAtoms;
            }
        }

        Dictionary<string, HashSet<IAtom>> ringAtomsByFingerprint = new Dictionary<string,HashSet<IAtom>>(StringComparer.Ordinal);
        /// <summary>
        /// Ring atoms by fingerprint.
        /// </summary>
        /// <pparam name="fingerprint"></pparam>
        /// <returns></returns>
        public HashSet<IAtom> RingAtomsByFingerprint(string fingerprint)
        {
            HashSet<IAtom> atoms;
            if (ringAtomsByFingerprint.TryGetValue(fingerprint, out atoms)) return atoms;

            atoms = Structure.Rings().GetRingsByFingerprint(fingerprint).SelectMany(r => r.Atoms).ToHashSet();
            ringAtomsByFingerprint.Add(fingerprint, atoms);
            return atoms;
        }
                
        ChainWrapper[] aminoChains;
        /// <summary>
        /// Amino chains.
        /// </summary>
        public ChainWrapper[] AminoChains
        {
            get
            {
                if (aminoChains == null)
                {
                    aminoChains = Structure.PdbChains()
                        .Select(g => new ChainWrapper(
                            g.Value.Residues
                                .Where(r => r.IsAmino || (r.IsModified && PdbResidue.IsAminoName(r.ModifiedFrom)))
                                .Select(r => Residues[r.Identifier]), 
                            r => PdbResidue.GetShortAminoName(r.Residue.IsModified ? r.Residue.ModifiedFrom : r.Residue.Name)))
                        .ToArray();
                }
                return aminoChains;
            }
        }

        ChainWrapper[] nucleotideChains;
        /// <summary>
        /// Amino chains.
        /// </summary>
        public ChainWrapper[] NucleotideChains
        {
            get
            {
                if (nucleotideChains == null)
                {
                    nucleotideChains = Structure.PdbChains()
                        .Select(g => new ChainWrapper(g.Value.Residues.Where(r => r.IsNucleotide).Select(r => Residues[r.Identifier]), r => PdbResidue.GetShortNucleotideName(r.Residue.Name)))
                        .ToArray();
                }
                return nucleotideChains;
            }
        }

        Dictionary<PdbResidueIdentifier, int> sheetMap, helixMap;

        /// <summary>
        /// Sheet
        /// </summary>
        public Dictionary<PdbResidueIdentifier, int> SheetMap
        {
            get
            {
                if (sheetMap == null)
                {
                    sheetMap = new Dictionary<PdbResidueIdentifier, int>(Structure.PdbResidues().Count);
                    int index = 1;
                    foreach (var s in Structure.PdbSheets())
                    {
                        foreach (var r in s.Residues)
                        {
                            sheetMap[r.Identifier] = index;
                        }
                        index++;
                    }
                }
                return sheetMap;
            }
        }

        /// <summary>
        /// Helix
        /// </summary>
        public Dictionary<PdbResidueIdentifier, int> HelixMap
        {
            get
            {
                if (helixMap == null)
                {
                    helixMap = new Dictionary<PdbResidueIdentifier, int>(Structure.PdbResidues().Count);
                    int index = 1;
                    foreach (var h in Structure.PdbHelices())
                    {
                        foreach (var r in h.Residues)
                        {
                            helixMap[r.Identifier] = index;
                        }
                        index++;
                    }
                }
                return helixMap;
            }
        }

        Dictionary<string, AtomPropertiesBase> atomProperties = new Dictionary<string, AtomPropertiesBase>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get atom property.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="atom"></param>
        /// <returns></returns>
        public object GetAtomProperty(string name, IAtom atom)
        {
            AtomPropertiesBase props;
            if (!atomProperties.TryGetValue(name, out props))
            {
                props = Structure.TryGetAtomProperties(name);
                atomProperties.Add(name, props);
            }

            if (props == null) return null;
            return props.TryGetValue(atom);
        }

        /// <summary>
        /// the cache.
        /// </summary>
        public MotiveCache Cache { get; private set; }

        /// <summary>
        /// New instance of a query context.
        /// </summary>
        /// <param name="structure"></param>
        public MotiveContext(IStructure structure)
        {
            this.Structure = structure;
            this.Cache = new MotiveCache();
        }
    }

    /// <summary>
    /// Query execution context.
    /// </summary>
    public class ExecutionContext
    {
        public Dictionary<string, Stack<object>> symbolStack = new Dictionary<string, Stack<object>>(16, StringComparer.Ordinal);

        /// <summary>
        /// The name must be lowercase.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetSymbolValue(string name)
        {
            Stack<object> values;
            if (symbolStack.TryGetValue(name, out values))
            {
                if (values.Count > 0) return values.Peek();
            }

            throw new InvalidOperationException(string.Format("The symbol '{0}' is not defined.", name));
        }

        /// <summary>
        /// Push a value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void PushSymbolValue(string name, object value)
        {
            Stack<object> values;
            if (symbolStack.TryGetValue(name, out values))
            {
                values.Push(value);
            }
            else
            {
                var stack = new Stack<object>();
                stack.Push(value);
                symbolStack.Add(name, stack);
            }
        }

        /// <summary>
        /// Pop a value if there is any.
        /// </summary>
        /// <param name="name"></param>
        public void PopSymbolValue(string name)
        {
            Stack<object> values;
            if (symbolStack.TryGetValue(name, out values))
            {
                values.Pop();
            }
        }

        /// <summary>
        /// The structures in the current execution context.
        /// </summary>
        public Dictionary<string, IStructure> Environment { get; private set; }

        /// <summary>
        /// Current Motive Context.
        /// </summary>
        public MotiveContext CurrentContext { get; set; }

        /// <summary>
        /// Try to obtain the current context and throw an exception if it's not available.
        /// </summary>
        /// <returns></returns>
        public MotiveContext RequestCurrentContext()
        {
            if (CurrentContext == null) throw new InvalidOperationException("MotiveContext is not set.");
            return CurrentContext;
        }
        
        /// <summary>
        /// The current motive. This is set by queries like Filter.
        /// </summary>
        public Motive CurrentMotive { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static ExecutionContext Create(IStructure structure)
        {
            return new ExecutionContext
            {
                Environment = new Dictionary<string, IStructure>(StringComparer.OrdinalIgnoreCase)
                {
                    { structure.Id, structure }
                },
                CurrentContext = structure.MotiveContext() 
            };
        }

        /// <summary>
        /// Create context from many structures.
        /// </summary>
        /// <param name="structures"></param>
        /// <returns></returns>
        public static ExecutionContext Create(IEnumerable<IStructure> structures)
        {
            return new ExecutionContext
            {
                Environment = structures.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Add structure.
        /// </summary>
        /// <param name="s"></param>
        public void Add(IStructure s)
        {
            if (!Environment.ContainsKey(s.Id)) Environment.Add(s.Id, s);
        }

        /// <summary>
        /// Remove structure.
        /// </summary>
        /// <param name="s"></param>
        public void Remove(IStructure s)
        {
            Environment.Remove(s.Id);
        }

        /// <summary>
        /// Reset the cache.
        /// </summary>
        public void ResetCache()
        {
            Environment.Values.ForEach(s => s.MotiveContext().Cache.Reset());
        }

        private ExecutionContext()
        {
        }
    }
}
