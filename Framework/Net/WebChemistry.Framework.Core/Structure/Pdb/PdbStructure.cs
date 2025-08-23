namespace WebChemistry.Framework.Core.Pdb
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    
    /// <summary>
    /// SEcondary structure element (sheet/helix)
    /// </summary>
    public class PdbSecondaryElement : InteractiveObject
    {
        /// <summary>
        /// All the residues.
        /// </summary>
        public IList<PdbResidue> Residues { get; private set; }

        protected override void OnHighlightedChanged()
        {
            Residues.ForEach(r => r.IsHighlighted = IsHighlighted);
        }

        protected override void OnSelectedChanged()
        {
            Residues.ForEach(r => r.IsSelected = IsSelected);
        }

        /// <summary>
        /// The first residue.
        /// </summary>
        public PdbResidue StartResidue { get; private set; }

        /// <summary>
        /// The last residue.
        /// </summary>
        public PdbResidue EndResidue { get; private set; }

        protected PdbSecondaryElement(IList<PdbResidue> residues, SecondaryStructureType type)
        {            
            this.StartResidue = residues[0];
            this.EndResidue = residues[residues.Count - 1];
            this.Residues = residues;
            foreach (var r in residues) r.SecondaryType = type;
        }
    }

    /// <summary>
    /// A helix.
    /// </summary>
    public class PdbHelix : PdbSecondaryElement
    {
        public static PdbHelix FromResidues(IEnumerable<PdbResidue> residues)
        {
            var list = residues.ToList();
            list.Sort();
            return FromOrderedResidues(list);
        }

        public static PdbHelix FromOrderedResidues(IList<PdbResidue> residues)
        {
            return new PdbHelix(residues);
        }


        private PdbHelix(IList<PdbResidue> residues)
            : base(residues, SecondaryStructureType.Helix)
        {
        }
    }

    /// <summary>
    /// A sheet.
    /// </summary>
    public class PdbSheet : PdbSecondaryElement
    {
        public static PdbSheet FromResidues(IEnumerable<PdbResidue> residues)
        {
            var list = residues.ToList();
            list.Sort();
            return FromOrderedResidues(list);
        }

        public static PdbSheet FromOrderedResidues(IList<PdbResidue> residues)
        {
            return new PdbSheet(residues);
        }

        private PdbSheet(IList<PdbResidue> residues)
            : base(residues, SecondaryStructureType.Sheet)
        {
        }
    }

    /// <summary>
    /// Chain.
    /// </summary>
    public class PdbChain : InteractiveObject
    {
        public ReadOnlyCollection<PdbResidue> Residues { get; private set; }
        public string Identifier { get; private set; }

        protected override void OnHighlightedChanged()
        {
            Residues.ForEach(r => r.IsHighlighted = IsHighlighted);
        }

        protected override void OnSelectedChanged()
        {
            Residues.ForEach(r => r.IsSelected = IsSelected);
        }

        public PdbChain(string identifier, IEnumerable<PdbResidue> residues)
        {
            this.Identifier = identifier;
            this.Residues = new ReadOnlyCollection<PdbResidue>(residues.AsList());
        }
    }

    /// <summary>
    /// Pdb structure properties and stuff.
    /// </summary>
    public static class PdbStructure
    {
        const string PdbCategory = "PdbStructure";

        public static readonly PropertyDescriptor<bool> IsPdbStructureProperty
            = PropertyHelper.Bool("IsPdbStructure", category: PdbCategory);

        public static readonly PropertyDescriptor<bool> IsPdbComponentStructureProperty
            = PropertyHelper.Bool("IsPdbComponentStructure", category: PdbCategory);
        
        public static readonly PropertyDescriptor<PdbBackbone> BackboneProperty
            = PropertyHelper.OfType<PdbBackbone>("Backbone", category: PdbCategory);

        public static readonly PropertyDescriptor<PdbResidueCollection> ResiduesProperty
            = PropertyHelper.OfType<PdbResidueCollection>("Residues", category: PdbCategory);
        
        public static readonly PropertyDescriptor<IDictionary<string, PdbChain>> ChainsProperty
            = PropertyHelper.OfType<IDictionary<string, PdbChain>>("Chains", category: PdbCategory);

        public static readonly PropertyDescriptor<PdbMetadata> MetadataProperty
            = PropertyHelper.OfType<PdbMetadata>("PdbMetadata", category: PdbCategory);

        public static readonly PropertyDescriptor<ReadOnlyCollection<PdbHelix>> HelicesProperty
            = PropertyHelper.OfType<ReadOnlyCollection<PdbHelix>>("Helices", category: PdbCategory);

        public static readonly PropertyDescriptor<ReadOnlyCollection<PdbResidue>> ModifiedResiduesProperty
            = PropertyHelper.OfType<ReadOnlyCollection<PdbResidue>>("ModifiedResidues", category: PdbCategory);

        public static readonly PropertyDescriptor<ReadOnlyCollection<PdbSheet>> SheetsProperty
            = PropertyHelper.OfType<ReadOnlyCollection<PdbSheet>>("Sheets", category: PdbCategory);

        public static readonly PropertyDescriptor<bool> PqrContainsChargesProperty
            = PropertyHelper.Bool("PqrContainsCharges", category: PdbCategory);

        //public static readonly PropertyDescriptor<ReadOnlyCollection<IStructure>> ModelsProperty
        //    = PropertyHelper.OfType<ReadOnlyCollection<IStructure>>("Models", category: PdbCategory);
        
        /// <summary>
        /// Modifies the current structure to become a PDB structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="modifiedResidues">can be null</param>
        /// <returns></returns>
        public static IStructure AsPdbStructure(IStructure structure, IList<PdbResidue> modifiedResidues)
        {
            if (structure.IsPdbStructure()) return structure;

            List<PdbChain> chains = new List<PdbChain>();
            foreach (var chain in structure.Atoms.GroupBy(a => a.PdbChainIdentifier()))
            {
                List<PdbResidue> residues = new List<PdbResidue>();
                foreach (var residue in chain.GroupBy(a => a.PdbResidueSequenceNumber().ToString() + a.PdbInsertionResidueCode()))
                {
                    residues.Add(PdbResidue.Create(residue.OrderBy(a => a.Id)));
                }
                chains.Add(new PdbChain(chain.Key, residues.OrderBy(r => r.Number).ThenBy(r => r.InsertionResidueCode)));
            }

            var orderedChains = new ReadOnlyCollection<PdbChain>(chains.OrderBy(c => c.Identifier).ToArray());
            var orderedResidues = PdbResidueCollection.Create(orderedChains.SelectMany(c => c.Residues));
            var orderedAtoms = AtomCollection.Create(orderedResidues.SelectMany(r => r.Atoms));

            if (modifiedResidues != null)
            {
                foreach (var mr in modifiedResidues)
                {
                    var r = orderedResidues.FromIdentifier(mr.Identifier);
                    if (r != null) r.ModifiedFrom = mr.ModifiedFrom;
                }
            }
            
            structure.SetProperty(PdbStructure.ResiduesProperty, orderedResidues);
            structure.SetProperty(PdbStructure.IsPdbStructureProperty, true);
            structure.SetProperty(PdbStructure.ChainsProperty, orderedChains.ToDictionary(c => c.Identifier));

            return structure;
        }

        /// <summary>
        /// Clone a PDB structure.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newId"></param>
        /// <returns></returns>
        public static IStructure ClonePdb(this IStructure source, string newId = null)
        {
            if (!source.IsPdbStructure()) throw new ArgumentException("The source must be a PDB structure");

            var ac = new Dictionary<int, IAtom>(source.Atoms.Count);
            for (int i = 0; i < source.Atoms.Count; i++)
            {
                var a = source.Atoms[i];
                ac.Add(a.Id, a.Clone());
            }
            var newAtoms = AtomCollection.Create(ac.Values);
            var bonds = source.Bonds.Select(b => Bond.Create(ac[b.A.Id], ac[b.B.Id], b.Type));
            var newBonds = BondCollection.Create(bonds);
            var structure = Structure.Create(newId ?? source.Id, newAtoms, newBonds);

            var residues = source.PdbResidues().Select(r => PdbResidue.Create(r.Atoms.Select(a => ac[a.Id])));
            var rsc = PdbResidueCollection.Create(residues);
            structure.SetProperty(PdbStructure.ResiduesProperty, rsc);
            structure.SetProperty(PdbStructure.IsPdbStructureProperty, true);
            structure.SetProperty(PdbStructure.ChainsProperty, residues
                .GroupBy(r => r.ChainIdentifier)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => new PdbChain(g.Key, g)));

            var modres = source.PdbModifiedResidues();
            foreach (var r in modres)
            {
                var mr = rsc.FromIdentifier(r.Identifier);
                if (mr != null) mr.ModifiedFrom = r.ModifiedFrom;
            }

            structure.SetProperty(PdbStructure.MetadataProperty, source.PdbMetadata());
            
            structure.SetProperty(PdbStructure.HelicesProperty, new ReadOnlyCollection<PdbHelix>(source.PdbHelices()
                .Select(h => PdbHelix.FromOrderedResidues(h.Residues.Select(r => rsc.FromIdentifier(r.Identifier)).ToList()))
                .ToList()));

            structure.SetProperty(PdbStructure.SheetsProperty, new ReadOnlyCollection<PdbSheet>(source.PdbSheets()
                .Select(s => PdbSheet.FromOrderedResidues(s.Residues.Select(r => rsc.FromIdentifier(r.Identifier)).ToList()))
                .ToList()));

            return structure;
        }

        /// <summary>
        /// Creates a deep copy of the structure containing only the listed chains.
        /// Coordinates are preserved.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="chains"></param>
        /// <param name="newId"></param>
        /// <returns></returns>
        public static IStructure CloneWithChains(this IStructure source, IEnumerable<string> chains, string newId = null)
        {
            if (!source.IsPdbStructure()) throw new ArgumentException("The source must be a PDB structure");

            var chainSet = chains.ToHashSet();
            var ac = new Dictionary<int, IAtom>(source.Atoms.Count);
            for (int i = 0; i < source.Atoms.Count; i++)
            {
                var a = source.Atoms[i];
                if (!chainSet.Contains(a.PdbChainIdentifier())) continue;

                ac.Add(a.Id, a.Clone());
            }
            var newAtoms = AtomCollection.Create(ac.Values);

            var bonds = source.Bonds
                .Where(b => ac.ContainsKey(b.A.Id) && ac.ContainsKey(b.B.Id))
                .Select(b => Bond.Create(ac[b.A.Id], ac[b.B.Id], b.Type));
            var newBonds = BondCollection.Create(bonds);
            var structure = Structure.Create(newId ?? source.Id, newAtoms, newBonds);

            var residues = source.PdbResidues()
                .Where(r => chainSet.Contains(r.ChainIdentifier))
                .Select(r => PdbResidue.Create(r.Atoms.Select(a => ac[a.Id])));
            var rsc = PdbResidueCollection.Create(residues);
            structure.SetProperty(PdbStructure.ResiduesProperty, rsc);
            structure.SetProperty(PdbStructure.IsPdbStructureProperty, true);
            structure.SetProperty(PdbStructure.MetadataProperty, source.PdbMetadata());
            structure.SetProperty(PdbStructure.ChainsProperty, residues
                .GroupBy(r => r.ChainIdentifier)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => new PdbChain(g.Key, g)));
            
            structure.SetProperty(PdbStructure.HelicesProperty, new ReadOnlyCollection<PdbHelix>(source.PdbHelices()
                .Where(h => chainSet.Contains(h.StartResidue.ChainIdentifier) && chainSet.Contains(h.EndResidue.ChainIdentifier))
                .Select(h => PdbHelix.FromOrderedResidues(h.Residues.Select(r => rsc.FromIdentifier(r.Identifier)).ToList()))
                .ToList()));

            structure.SetProperty(PdbStructure.SheetsProperty, new ReadOnlyCollection<PdbSheet>(source.PdbSheets()
                .Where(s => chainSet.Contains(s.StartResidue.ChainIdentifier) && chainSet.Contains(s.EndResidue.ChainIdentifier))
                .Select(s => PdbSheet.FromOrderedResidues(s.Residues.Select(r => rsc.FromIdentifier(r.Identifier)).ToList()))
                .ToList()));

            return structure;
        } 
    }
}