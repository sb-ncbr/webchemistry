namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// Countable query base.
    /// </summary>
    public abstract class CountableQuery : QueryMotive
    {
        public abstract int Count(Motive motive);
    }

    /// <summary>
    /// Query that returns the number of occurrences of a given motive.
    /// </summary>
    public class CountQuery : QueryValueBase
    {
        QueryMotive inner;
        Query where;

        internal override object Execute(ExecutionContext context)
        {
            return Count(inner, where.ExecuteObject(context).MustBe<Motive>(), context);
        }

        public static int Count(QueryMotive what, Motive where, ExecutionContext context)
        {
            var countable = what as CountableQuery;

            if (countable != null)
            {
                return countable.Count(where);
            }

            var oldCurrent = context.CurrentContext;
            context.CurrentContext = where.ToStructure("temp", true, true).MotiveContext();
            var ret = what.Execute(context).Count();
            context.CurrentContext = oldCurrent;
            return ret;
        }

        protected override string ToStringInternal()
        {
            return "Count[" + inner.ToString() + "," + where.ToString() + "]";
        }

        public CountQuery(QueryMotive inner, Query where)
        {
            this.inner = inner;
            this.where = where;
        }
    }

    /// <summary>
    /// Query that returns the number of occurrences of a given motive.
    /// </summary>
    public class SeqCountQuery : QueryValueBase
    {
        QueryMotive what;

        internal override object Execute(ExecutionContext context)
        {
            return what.ExecuteMotive(context).Count();
        }
        
        protected override string ToStringInternal()
        {
            return "SeqCount(" + what.ToString() + ")";
        }

        public SeqCountQuery(QueryMotive what)
        {
            this.what = what;
        }
    }


    /// <summary>
    /// Query that returns the number of occurrences of a given motive.
    /// </summary>
    public class LengthQuery : QueryValueBase
    {
        QueryMotive inner;
        
        internal override object Execute(ExecutionContext context)
        {
            return inner.Execute(context).Count();
        }
        
        protected override string ToStringInternal()
        {
            return "Length[" + inner.ToString() + "]";
        }

        public LengthQuery(QueryMotive inner)
        {
            this.inner = inner;
        }
    }

    /// <summary>
    /// Returns the first atom Id.
    /// </summary>
    public class AtomIdQuery : QueryValueBase
    {
        Query where;

        internal override object Execute(ExecutionContext context)
        {
            var id = int.MaxValue;
            where.ExecuteObject(context).MustBe<Motive>().Atoms.VisitLeaves(atom =>
            {
                if (atom.Id < id) id = atom.Id;
            });
            return id;
        }

        protected override string ToStringInternal()
        {
            return "AtomId[" + where + "]";
        }

        public AtomIdQuery(Query where)
        {
            this.where = where;
        }
    }

    /// <summary>
    /// Return atom chain.
    /// </summary>
    public class AtomChainQuery : QueryValueBase
    {
        Query where;

        internal override object Execute(ExecutionContext context)
        {
            var id = int.MaxValue;
            IAtom atm = null;
            where.ExecuteObject(context).MustBe<Motive>().Atoms.VisitLeaves(atom =>
            {
                if (atom.Id < id)
                {
                    id = atom.Id;
                    atm = atom;
                }
            });
            
            return atm.PdbChainIdentifier().ToString();
        }

        protected override string ToStringInternal()
        {
            return "AtomChain[" + where + "]";
        }

        public AtomChainQuery(Query where)
        {
            this.where = where;
        }
    }

    /// <summary>
    /// Matches any atom from the input set.
    /// </summary>
    public class AtomSetQuery : CountableQuery
    {
        static Func<IAtom, bool> CreateMembershipTest(string[] names, bool complement)
        {
            if (complement)
            {
                if (names.Length == 0)
                {
                    return new Func<IAtom, bool>(a => false);
                }
                if (names.Length == 1)
                {
                    var name = names[0];
                    return new Func<IAtom, bool>(a => !string.Equals(name, a.ElementSymbol.ToString(), StringComparison.OrdinalIgnoreCase));
                }

                var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Func<IAtom, bool>(a =>
                {
                    return !nameSet.Contains(a.ElementSymbol.ToString());
                });
            }
            else
            {
                if (names.Length == 0)
                {
                    return new Func<IAtom, bool>(a => true);
                }
                if (names.Length == 1)
                {
                    var name = names[0];
                    return new Func<IAtom, bool>(a => string.Equals(name, a.ElementSymbol.ToString(), StringComparison.OrdinalIgnoreCase));
                }

                var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Func<IAtom, bool>(a =>
                {
                    return nameSet.Contains(a.ElementSymbol.ToString());
                });
            }
        }

        Func<IAtom, bool> membershipTest;
        string[] names;
        bool complement;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Structure.Atoms.Where(a => membershipTest(a)).Select(a => Motive.FromAtom(a, currentContext));
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", complement ? "AtomSetComplement" : "AtomSet",  string.Join(",", names));
        }

        public AtomSetQuery(IEnumerable<string> names, bool complement)
        {
            this.names = names.Select(n => ElementSymbol.Create(n).ToString()).Distinct().OrderBy(n => n).ToArray();
            this.membershipTest = CreateMembershipTest(this.names, complement);
            this.complement = complement;
        }

        public override int Count(Motive motive)
        {
            int count = 0;
            var flat = motive.Atoms.Flatten();
            for (int i = 0; i < flat.Count; i++)
            {
                if (membershipTest(flat[i])) count++;
            }
            return count;
        }

        public bool IsMember(IAtom atom)
        {
            return membershipTest(atom);
        }
    }

    /// <summary>
    /// Matches any atom from the input set.
    /// </summary>
    public class AtomNamesQuery : CountableQuery
    {
        HashSet<string> names;
        bool complement;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            if (complement) return currentContext.Structure.Atoms.Where(a => !names.Contains(a.PdbName())).Select(a => Motive.FromAtom(a, currentContext));
            return currentContext.Structure.Atoms.Where(a => names.Contains(a.PdbName())).Select(a => Motive.FromAtom(a, currentContext));
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", complement ? "NotAtomNames" : "AtomNames", string.Join(",", names.OrderBy(n => n)));
        }

        public AtomNamesQuery(IEnumerable<string> names, bool complement)
        {
            this.names = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            this.complement = complement;
        }

        public override int Count(Motive motive)
        {
            return motive.Atoms.Count(a => IsMember(a));
        }

        public bool IsMember(IAtom atom)
        {
            return complement ? !names.Contains(atom.PdbName()) : names.Contains(atom.PdbName());
        }
    }

    /// <summary>
    /// Matches any atom from the input set.
    /// </summary>
    public class AtomIdsQuery : CountableQuery
    {
        HashSet<int> ids;
        bool complement;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            if (complement) return currentContext.Structure.Atoms.Where(a => !ids.Contains(a.Id)).Select(a => Motive.FromAtom(a, currentContext));
            
            List<Motive> ret = new List<Motive>(ids.Count);
            var atoms = currentContext.Structure.Atoms;
            foreach (var id in ids)
            {
                IAtom a;
                if (atoms.TryGetAtom(id, out a)) ret.Add(Motive.FromAtom(a, currentContext));
            }
            return ret;
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", complement ? "NotAtomIds" : "AtomIds", string.Join(",", ids.OrderBy(n => n).Select(n => n.ToString())));
        }

        public AtomIdsQuery(IEnumerable<int> ids, bool complement)
        {
            this.ids = new HashSet<int>(ids);
            this.complement = complement;
        }

        public override int Count(Motive motive)
        {
            return motive.Atoms.Count(a => IsMember(a));
        }

        public bool IsMember(IAtom atom)
        {
            return complement ? !ids.Contains(atom.Id) : ids.Contains(atom.Id);
        }
    }

    /// <summary>
    /// Matches a ring with a given fingerprint.
    /// </summary>
    class AtomIdRangeQuery : CountableQuery
    {
        int minId, maxId;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Structure.Atoms.Where(a => a.Id >= minId && a.Id <= maxId).Select(a => Motive.FromAtom(a, currentContext));
        }

        protected override string ToStringInternal()
        {
            return "AtomIdRange[" + minId + "," + maxId + "]";
        }
        
        public AtomIdRangeQuery(int a, int b)
        {
            this.minId = Math.Min(a, b);
            this.maxId = Math.Max(a, b);
        }

        public override int Count(Motive motive)
        {
            return motive.Atoms.Count(a => a.Id >= minId && a.Id <= maxId);
        }
    }

    /// <summary>
    /// Match any residue in/out the set.
    /// </summary>
    class ResidueSetQuery : CountableQuery
    {
        static Func<string, bool> CreateMembershipTest(string[] names, bool complement)
        {
            if (complement)
            {
                if (names.Length == 0) return new Func<string, bool>(_ => false);

                var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Func<string, bool>(name =>
                {
                    return !nameSet.Contains(name);
                });
            }
            else
            {
                if (names.Length == 0) return new Func<string, bool>(_ => true);

                var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Func<string, bool>(name =>
                {
                    return nameSet.Contains(name);
                });
            }
        }

        Func<string, bool> membershipTest;
        string[] names;
        bool complement;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Residues.Values.Where(r => membershipTest(r.Residue.Name)).Select(r => Motive.FromResidue(r, currentContext));
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", complement ? "ResidueSetComplement" : "ResidueSet", string.Join(",", names));
        }

        public ResidueSetQuery(IEnumerable<string> names, bool complement)
        {
            this.names = names.OrderBy(n => n).ToArray();
            this.complement = complement;
            this.membershipTest = CreateMembershipTest(this.names, this.complement);
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> visited = new HashSet<PdbResidueIdentifier>();
            int count = 0;
            motive.Atoms.VisitLeaves(atom =>
            {
                if (visited.Add(atom.ResidueIdentifier()) && membershipTest(atom.PdbResidueName())) count++;
            });
            return count;
        }
    }

    /// <summary>
    /// Match any residue in/out the set.
    /// </summary>
    class ModifiedResiduesQuery : CountableQuery
    {
        static Func<string, bool> CreateMembershipTest(string[] names)
        {
            if (names.Length == 0) return new Func<string, bool>(_ => true);

            {
                var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new Func<string, bool>(name =>
                {
                    return nameSet.Contains(name);
                });
            }
        }

        Func<string, bool> membershipTest;
        string[] names;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Residues.Values.Where(r => r.Residue.IsModified && membershipTest(r.Residue.ModifiedFrom)).Select(r => Motive.FromResidue(r, currentContext));
        }

        protected override string ToStringInternal()
        {
            return NameHelper("ModifiedResidues", names);
        }

        public ModifiedResiduesQuery(IEnumerable<string> names)
        {
            this.names = names.OrderBy(n => n).ToArray();
            this.membershipTest = CreateMembershipTest(this.names);
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> visited = new HashSet<PdbResidueIdentifier>();
            var residues = motive.Context.Structure.PdbResidues();
            int count = 0;
            motive.Atoms.VisitLeaves(atom =>
            {
                if (visited.Add(atom.ResidueIdentifier()))
                {
                    var r = residues.FromIdentifier(atom.ResidueIdentifier());
                    if (r != null && r.IsModified && membershipTest(r.ModifiedFrom)) count++;
                }
            });
            return count;
        }
    }

    class ResidueIdRangeQuery : CountableQuery
    {
        int minId, maxId;
        string chain;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();

            return currentContext.Residues.Values
                .Where(r => r.Residue.ChainIdentifier == chain && r.Residue.Number >= minId && r.Residue.Number <= maxId)
                .Select(r => Motive.FromResidue(r, currentContext));
        }

        protected override string ToStringInternal()
        {
            return "ResidueIdRange[" + chain + "," + minId + "," + maxId + "]";
        }

        public ResidueIdRangeQuery(string chain, int min, int max)
        {
            this.minId = Math.Min(min, max);
            this.maxId = Math.Max(min, max);
            this.chain = chain;
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> visited = new HashSet<PdbResidueIdentifier>();
            int count = 0;

            motive.Atoms.VisitLeaves(atom =>
            {
                if (visited.Add(atom.ResidueIdentifier()))
                {
                    var atomChain = atom.PdbChainIdentifier();
                    var number = atom.PdbResidueSequenceNumber();
                    if (atomChain == chain && number >= minId && number <= maxId)
                    count++;
                }
            });

            return count;
        }
    }

    class ResidueIdsQuery : CountableQuery
    {
        string[] residues;
        HashSet<PdbResidueIdentifier> identifiers;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();

            return currentContext.Residues.Values
                .Where(r => identifiers.Contains(r.Residue.Identifier))
                .Select(r => Motive.FromResidue(r, currentContext));
        }

        protected override string ToStringInternal()
        {
            return NameHelper("ResidueIds", residues);
        }
        
        public ResidueIdsQuery(IEnumerable<string> ids)
        {
            this.residues = ids.ToArray();
            if (residues.Length == 0) throw new ArgumentException("ResidueIds: at least one identifier required.");
            this.identifiers = this.residues.Select(id => PdbResidueIdentifier.Parse(id)).ToHashSet();
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> visited = new HashSet<PdbResidueIdentifier>();
            int count = 0;

            motive.Atoms.VisitLeaves(atom =>
            {
                var id = atom.ResidueIdentifier();
                if (identifiers.Contains(id) && visited.Add(atom.ResidueIdentifier()))
                {
                    count++;
                }
            });

            return count;
        }
    }

    public enum QueriesResidueCategories
    {
        Any,
        BasicAminoAcids,
        AllAminoAcids,
        Ligands,
        BasicNucleotides,
        AllNucleotides
    }

    /// <summary>
    /// Match any residue in/out the set.
    /// </summary>
    class ResidueCategoryQuery : CountableQuery
    {
        static Func<string, bool> CreateMembership(string[] types)
        {
            return null;
            //if (types.Length == 0) return new Func<string, bool>(_ => true);

            //var nameSet = types.ToHashSet(StringComparer.OrdinalIgnoreCase);
            //return new Func<string, bool>(name =>
            //{
            //    return nameSet.Contains(name);
            //});
        }

        static Func<string, bool> CreateMembershipTest(string[] types, bool complement)
        {
            var func = CreateMembership(types);
            if (complement)
            {
                return new Func<string, bool>(name =>
                {
                    return !func(name);
                });
            }
            return func;
        }

        Func<string, bool> membershipTest;
        string[] types;
        bool complement;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Residues.Values.Where(r => membershipTest(r.Residue.Name)).Select(r => Motive.FromResidue(r, currentContext));
        }

        protected override string ToStringInternal()
        {
            return string.Format("{0}[{1}]", complement ? "ResidueCategoryComplement" : "ResidueCategory", string.Join(",", types));
        }

        public ResidueCategoryQuery(IEnumerable<string> types, bool complement)
        {
            this.types = types.OrderBy(n => n).ToArray();
            this.complement = complement;
            this.membershipTest = CreateMembershipTest(this.types, this.complement);
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> visited = new HashSet<PdbResidueIdentifier>();
            int count = 0;
            motive.Atoms.VisitLeaves(atom =>
            {
                if (visited.Add(atom.ResidueIdentifier()) && membershipTest(atom.PdbResidueName())) count++;
            });
            return count;
        }
    }

    /// <summary>
    /// Match any amino acid.
    /// </summary>
    class AminoAcidQuery : CountableQuery
    {
        ResidueChargeType? chargeType;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            if (chargeType.HasValue)
            {
                var type = chargeType.Value;
                var currentContext = context.RequestCurrentContext();
                return currentContext.Residues.Values.Where(r => r.Residue.IsAmino && r.Residue.ChargeType == type).Select(r => Motive.FromResidue(r, currentContext));
            }
            else
            {
                var currentContext = context.RequestCurrentContext();
                return currentContext.Residues.Values.Where(r => r.Residue.IsAmino).Select(r => Motive.FromResidue(r, currentContext));
            }
        }

        protected override string ToStringInternal()
        {
            if (chargeType.HasValue)
            {
                return NameHelper("AminoAcids", new [] { NameOption("ChargeType", chargeType.Value.ToString()) }, new string[0]);
            }
            return "AminoAcids[]";
        }

        public AminoAcidQuery(string chargeType)
        {
            if (!string.IsNullOrWhiteSpace(chargeType))
            {
                ResidueChargeType type;
                if (!Enum.TryParse<ResidueChargeType>(chargeType, true, out type))
                {
                    throw new ArgumentException(string.Format("'{0}' is not a recognized charge type."));
                }
                this.chargeType = type;
            }
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> counted = new HashSet<PdbResidueIdentifier>();
            int count = 0;

            if (chargeType.HasValue)
            {
                var type = chargeType.Value;
                motive.Atoms.VisitLeaves(atom =>
                {
                    var name = atom.PdbResidueName();
                    if (PdbResidue.IsAminoName(name) && PdbResidue.GetChargeType(name) == type && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }
            else
            {
                motive.Atoms.VisitLeaves(atom =>
                {
                    if (PdbResidue.IsAminoName(atom.PdbResidueName()) && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }

            return count;
        }
    }

    /// <summary>
    /// Match any amino acid.
    /// </summary>
    class NotAminoAcidQuery : CountableQuery
    {
        bool ignoreWaters;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var mc = context.RequestCurrentContext();
            if (ignoreWaters) return mc.Residues.Values.Where(r => !r.Residue.IsWater && !r.Residue.IsAmino).Select(r => Motive.FromResidue(r,mc));
            return mc.Residues.Values.Where(r => !r.Residue.IsAmino).Select(r => Motive.FromResidue(r,mc));            
        }

        protected override string ToStringInternal()
        {
            return string.Format("NotAminoAcid[IgnoreWaters={0}]", ignoreWaters);
        }

        public NotAminoAcidQuery(bool ignoreWaters)
        {
            this.ignoreWaters = ignoreWaters;
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> counted = new HashSet<PdbResidueIdentifier>();
            int count = 0;
            
            if (ignoreWaters)
            {
                motive.Atoms.VisitLeaves(atom =>
                {
                    if (!atom.IsWater() && !PdbResidue.IsAminoName(atom.PdbResidueName()) && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }
            else
            {
                motive.Atoms.VisitLeaves(atom =>
                {
                    if (!PdbResidue.IsAminoName(atom.PdbResidueName()) && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }
            return count;
        }
    }

    /// <summary>
    /// Match any amino acid.
    /// </summary>
    class HetResiduesQuery : CountableQuery
    {
        bool ignoreWaters;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var mc = context.RequestCurrentContext();
            if (ignoreWaters) return mc.Residues.Values.Where(r => !r.Residue.IsWater && r.Residue.Atoms[0].IsHetAtomStrict()).Select(r => Motive.FromResidue(r, mc));
            return mc.Residues.Values.Where(r => r.Residue.Atoms[0].IsHetAtomStrict()).Select(r => Motive.FromResidue(r, mc));
        }

        protected override string ToStringInternal()
        {
            return string.Format("HetResidues[IgnoreWaters={0}]", ignoreWaters);
        }

        public HetResiduesQuery(bool ignoreWaters)
        {
            this.ignoreWaters = ignoreWaters;
        }

        public override int Count(Motive motive)
        {
            HashSet<PdbResidueIdentifier> counted = new HashSet<PdbResidueIdentifier>();
            int count = 0;

            if (ignoreWaters)
            {
                motive.Atoms.VisitLeaves(atom =>
                {
                    if (!atom.IsWater() && atom.IsHetAtomStrict() && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }
            else
            {
                motive.Atoms.VisitLeaves(atom =>
                {
                    if (atom.IsHetAtomStrict() && counted.Add(atom.ResidueIdentifier())) count++;
                });
            }
            return count;
        }
    }

    /// <summary>
    /// Matches a ring with a given fingerprint.
    /// </summary>
    class RingQuery : CountableQuery
    {
        public readonly string Fingerprint;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var mc = context.RequestCurrentContext();
            var rings = string.IsNullOrEmpty(Fingerprint) ? mc.Structure.Rings() : mc.Structure.Rings().GetRingsByFingerprint(Fingerprint);
            return rings.Select(r => Motive.FromAtoms(r.Atoms, null, mc));
        }

        protected override string ToStringInternal()
        {
            return "Ring[\"" + Fingerprint + "\"]";
        }

        static string GetRingFingerPrint(string[] names)
        {
            if (names.Length > 8) throw new ArgumentException("Rings with more than 8 atoms are not supported.");
            if (names.Length > 0 && names.Length < 3) throw new ArgumentException("A ring must contain at least 3 elements.");
            if (names.Length == 0) return "";

            var elements = names
                .Select(x => ElementSymbol.Create(x))
                .ToArray();

            return Ring.GetFingerprint(elements, e => e.ToString());
        }

        public RingQuery(string[] names)
        {
            if (names != null) Fingerprint = GetRingFingerPrint(names);
        }

        public override int Count(Motive motive)
        {
            var context = motive.Context;
            var rings = string.IsNullOrEmpty(Fingerprint) ? context.Structure.Rings() : context.Structure.Rings().GetRingsByFingerprint(Fingerprint);
            int count = 0;

            foreach (var ring in rings)
            {
                if (ring.Atoms.All(a => motive.Atoms.Contains(a))) count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Matches a ring with a given fingerprint.
    /// </summary>
    class OnRingQuery : CountableQuery
    {
        string fingerprint;
        RingQuery ring;
        AtomSetQuery atom;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var mc = context.RequestCurrentContext();
            var rings = string.IsNullOrEmpty(fingerprint) ? mc.Structure.Rings() : mc.Structure.Rings().GetRingsByFingerprint(fingerprint);

            return rings.SelectMany(r => r.Atoms.Where(a => atom.IsMember(a))).Select(a => Motive.FromAtom(a, mc));
        }

        protected override string ToStringInternal()
        {
            if (ring == null) return "OnRing[" + atom.ToString() + "]";
            return "OnRing[" + atom.ToString() + "," + ring.ToString() + "]";
        }

        public OnRingQuery(AtomSetQuery atom, RingQuery ring)
        {
            this.ring = ring;
            this.atom = atom;
            this.fingerprint = ring != null ? ring.Fingerprint : null;
        }

        public override int Count(Motive motive)
        {
            var context = motive.Context;
            var ringsAtoms = string.IsNullOrEmpty(fingerprint) ? context.RingAtoms : context.RingAtomsByFingerprint(fingerprint);
            var flat = motive.Atoms.Flatten();
            int count = 0;

            for (int i = 0; i < flat.Count; i++)
            {
                var a = flat[i];
                if (atom.IsMember(a) && ringsAtoms.Contains(a)) count++;
            }

            return count;
        }
    }
}
