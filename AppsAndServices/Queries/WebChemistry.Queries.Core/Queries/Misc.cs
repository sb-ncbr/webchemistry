namespace WebChemistry.Queries.Core.Queries
{

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core.Utils;
    using System.Reflection;

    public enum RegexQueryTypes
    {
        Amino = 0,
        Nucleotide
    }

    /// <summary>
    /// Match a regex in an AminoChain.
    /// </summary>
    class RegexQuery : QueryMotive
    {
        static Regex GetRegex(string input)
        {
            var options = /*RegexOptions.CultureInvariant  | RegexOptions.Singleline -- not compatible with ecmascript | */ RegexOptions.IgnoreCase | RegexOptions.ECMAScript;
            return new Regex(input, RegexOptions.Compiled | options);
        }

        Query input;
        RegexQueryTypes type;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var ctx = context.RequestCurrentContext();

            var e = input.ExecuteObject(context).MustBe<string>();
            var regex = GetRegex(e);
            var src = type == RegexQueryTypes.Amino ? ctx.AminoChains : ctx.NucleotideChains;

            foreach (var chain in src)
            {
                foreach (Match match in regex.Matches(chain.Chain))
                {
                    var rs = new ResidueWrapper[match.Length];
                    Array.Copy(chain.Residues, match.Index, rs, 0, match.Length);
                    yield return Motive.FromResidues(rs, null, ctx);
                }
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper("RegexMotive", new[] { NameOption("Type", type.ToString()) }, new[] { input.ToString() });
        }

        public RegexQuery(Query input, string type)
        {
            if (!EnumHelper<RegexQueryTypes>.TryParseFast(type, out this.type))
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid regular motif query type.", type));
            }
            this.input = input;
        }
    }

    /// <summary>
    /// return a sequence of amino acids.
    /// </summary>
    public class AminoSequenceStringQuery : QueryValueBase
    {
        Query where;

        internal override object Execute(ExecutionContext context)
        {
            var motive = where.ExecuteObject(context).MustBe<Motive>();
            var residues = motive.Context.Structure.PdbResidues();
            var seq = string.Concat(motive.Atoms
                .Select(a => a.ResidueIdentifier())
                .Distinct()
                .Select(id => residues.FromIdentifier(id))
                .Where(r => r != null && r.IsAmino)
                .OrderBy(r => r)
                .Select(r => r.ShortName));
            return seq;
        }

        protected override string ToStringInternal()
        {
            return "AminoSequenceString[" + where.ToString() + "]";
        }

        public AminoSequenceStringQuery(Query where)
        {
            this.where = where;
        }
    }

    /// <summary>
    /// Create motives from CatalyticSiteAtlas entries.
    /// </summary>
    class CSAQuery : QueryMotive
    {
        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var ctx = context.RequestCurrentContext();

            var sites = CatalyticSiteAtlas.GetActiveSites(ctx.Structure);

            foreach (var site in sites)
            {
                yield return Motive.FromResidues(site.Residues.Select(r => ctx.Residues[r.Identifier]), null, ctx);
            }
        }

        protected override string ToStringInternal()
        {
            return "CSA[]";
        }

        public CSAQuery()
        {
        }
    }



    enum MotiveSimilarityType
    {
        AtomJaccard,
        ResidueJaccard
    }

    class MotiveSimilarityQuery : QueryValueBase
    {
        MotiveSimilarityType type;
        Query a, b;

        static double Similarity(List<string> xs, List<string> ys)
        {
            var xc = xs.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var yc = ys.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
                        
            int common = 0;
            
            foreach (var p in xc)
            {
                int count;
                if (yc.TryGetValue(p.Key, out count))
                {
                    common += Math.Min(p.Value, count);
                }
            }

            return ((double)common) / ((double)(xs.Count + ys.Count - common));
        }

        internal override object Execute(ExecutionContext context)
        {
            var x = a.ExecuteObject(context).MustBe<Motive>();
            var y = b.ExecuteObject(context).MustBe<Motive>();

            if (type == MotiveSimilarityType.AtomJaccard) return Similarity(x.Atoms.Select(t => t.ElementSymbol.ToString()).ToList(), y.Atoms.Select(t => t.ElementSymbol.ToString()).ToList());

            var xResidues = x.Context.Structure.PdbResidues();
            var yResidues = y.Context.Structure.PdbResidues();

            var xs = x.Atoms.GroupBy(t => t.ResidueIdentifier()).Select(r => xResidues.FromIdentifier(r.Key).Name).ToList();
            var ys = y.Atoms.GroupBy(t => t.ResidueIdentifier()).Select(r => yResidues.FromIdentifier(r.Key).Name).ToList();

            return Similarity(xs, ys);
        }

        protected override string ToStringInternal()
        {
            return string.Format("MotiveSimilarity[{0},{1},{2}]", type, a, b);
        }

        public MotiveSimilarityQuery(MotiveSimilarityType type, Query a, Query b)
        {
            this.type = type;
            this.a = a;
            this.b = b;
        }
    }

    class DynamicInvokeQuery : QueryValueBase
    {
        bool dynamicArgs;
        bool isProperty;
        string name;
        object[] args;
        Query value;
        
        internal override object Execute(ExecutionContext context)
        {
            var obj = value.ExecuteObject(context);
            var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

            if (isProperty)
            {
                return obj.GetType().InvokeMember(name, flags | BindingFlags.GetProperty, null, obj, null);
            }
            else
            {
                if (dynamicArgs)
                {
                    var nArgs = new object[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        var a = args[i];
                        if (a is Query) nArgs[i] = (a as Query).ExecuteObject(context);
                        else nArgs[i] = a;
                    }
                    return obj.GetType().InvokeMember(name, flags | BindingFlags.InvokeMethod, null, obj, nArgs);
                }
                else
                {
                    return obj.GetType().InvokeMember(name, flags | BindingFlags.InvokeMethod, null, obj, args);
                }
            }
        }

        protected override string ToStringInternal()
        {
            if (args != null) return NameHelper("DynamicInvoke", value.ToString(), name, isProperty.ToString(), string.Join(", ", args.Select(a => a.ToString())));
            return NameHelper("DynamicInvoke", value.ToString(), name, isProperty.ToString());
        }

        public DynamicInvokeQuery(Query value, string name, bool isProperty, object[] args)
        {
            this.name = name.ToLowerInvariant();
            this.value = value;
            this.isProperty = isProperty;
            this.args = args;
            this.dynamicArgs = args != null && args.Any(a => a is Query);
        }
    }


    class AtomPropertyQuery : QueryValueBase
    {
        string name;
        Query motive;
        
        internal override object Execute(ExecutionContext context)
        {
            var m = motive.ExecuteObject(context).MustBe<Motive>();
            if (!m.Atoms.IsSingleton) return null;
            return m.Context.GetAtomProperty(name, m.Atoms.SingleElement);
        }

        protected override string ToStringInternal()
        {
            return string.Format("AtomProperty[{0}, \"{1}\"]", motive.ToString(), name);
        }

        public AtomPropertyQuery(string name, Query motive)
        {
            this.name = name;
            this.motive = motive;
        }
    }
    
    class StructureDescriptorQuery : QueryValueBase
    {
        string name;
        Query motive;

        internal override object Execute(ExecutionContext context)
        {
            var m = motive.ExecuteObject(context).MustBe<Motive>();

            if (object.ReferenceEquals(m, m.Context.StructureMotive))
            {
                return m.Context.Structure.Descriptors()[name];
            }

            throw new InvalidOperationException("Descriptors can only be read from 'structure level' motives.");
        }

        protected override string ToStringInternal()
        {
            return string.Format("Descriptor[\"{0}\",{1}]", name, motive);
        }

        public StructureDescriptorQuery(string name, Query motive)
        {
            this.name = name.Trim();
            this.motive = motive;
        }
    }

    /// <summary>
    /// "Name" the inner seq.
    /// </summary>
    public class NamedQuery : QueryMotive
    {
        QueryMotive inner;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            return inner.Execute(context).Select(m => Motive.Named(m));
        }

        protected override string ToStringInternal()
        {
            return string.Format("Named[{0}]", inner.ToString());
        }

        public NamedQuery(QueryMotive inner)
        {
            this.inner = inner;
        }
    }

    /// <summary>
    /// Filters the query.
    /// </summary>
    public class UnionQuery : QueryMotive
    {
        QueryMotive inner;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            HashSet<IAtom> atoms = new HashSet<IAtom>();
            int? name = null;
            foreach (var m in inner.Execute(context))
            {
                name = Motive.GetName(name, m.Name);
                m.Atoms.VisitLeaves(a => atoms.Add(a));
            }
            if (atoms.Count > 0) yield return Motive.FromAtoms(atoms, name, context.RequestCurrentContext());
        }

        protected override string ToStringInternal()
        {
            return string.Format("Union[{0}]", inner.ToString());
        }

        public UnionQuery(QueryMotive inner)
        {
            this.inner = inner;
        }
    }

    /// <summary>
    /// Convert a sequence to Motive.
    /// </summary>
    class ToQueries : Query<Motive>
    {
        QueryMotive inner;

        internal override Motive Execute(ExecutionContext context)
        {
            HashSet<IAtom> atoms = new HashSet<IAtom>();
            MotiveContext parentContext = null;
            int? name = null;
            foreach (var m in inner.Execute(context))
            {                
                if (parentContext == null) parentContext = m.Context;
                else if (!object.ReferenceEquals(parentContext, m.Context))
                {
                    throw new InvalidOperationException(string.Format("Cannot convert the sequence '{0}' to a motive because the individual motives come from different contexts.", inner.ToString()));
                }
                name = Motive.GetName(name, m.Name);
                m.Atoms.VisitLeaves(a => atoms.Add(a));
            }
            if (atoms.Count > 0) return Motive.FromAtoms(atoms, name, parentContext);
            throw new InvalidOperationException(string.Format("Cannot convert the sequence '{0}' to a motive because it contains no atoms.", inner.ToString()));
        }

        protected override string ToStringInternal()
        {
            return string.Format("ToMotive[{0}]", inner.ToString());
        }

        public ToQueries(QueryMotive inner)
        {
            this.inner = inner;
        }
    }

    enum ToElementsQueryType
    {
        Atoms,
        Residues
    }

    class ToElementsQuery : QueryMotive
    {
        ToElementsQueryType type;
        QueryMotive inner;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            HashSet<IAtom> atoms = new HashSet<IAtom>();
            foreach (var m in inner.Execute(context))
            {
                m.Atoms.VisitLeaves(a => atoms.Add(a));
            }
            {
                var currentContext = context.RequestCurrentContext();
                if (type == ToElementsQueryType.Atoms) return atoms.Select(a => Motive.FromAtom(a, currentContext));
                return atoms
                    .GroupBy(a => a.ResidueIdentifier())
                    .Select(g => Motive.FromAtoms(g, null, currentContext));
            }
        }

        protected override string ToStringInternal()
        {
            return string.Format("ToElements[{0},{1}]", type, inner.ToString());
        }

        public ToElementsQuery(ToElementsQueryType type, QueryMotive inner)
        {
            this.type = type;
            this.inner = inner;
        }
    }

    /// <summary>
    /// Common atoms.
    /// </summary>
    /// <summary>
    /// Matches any atom from the input set.
    /// </summary>
    public class CommonAtomsQuery : QueryMotive
    {
        string id;
        StructureQueries motive;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var ctx = context.RequestCurrentContext();
            var atoms = ctx.Structure.Atoms;
            List<IAtom> ret = new List<IAtom>();
            foreach (var t in motive.Execute(context).Atoms)
            {
                IAtom a;
                if (!atoms.TryGetAtom(t.Id, out a)) continue;
                if (a.ElementSymbol == t.ElementSymbol
                    && a.PdbChainIdentifier() == t.PdbChainIdentifier()
                    && a.PdbResidueSequenceNumber() == t.PdbResidueSequenceNumber())
                {
                    ret.Add(a);
                }
            }
            return new[] { Motive.FromAtoms(ret, null, ctx) };
        }

        protected override string ToStringInternal()
        {
            return NameHelper("CommonAtoms", id);
        }

        public CommonAtomsQuery(string id)
        {
            this.id = id;
            this.motive = new StructureQueries(id);
        }
    }


    class ChainsQuery : QueryMotive
    {
        string[] ids;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            var chains = currentContext.Structure.PdbChains();

            if (ids.Length == 0)
            {
                return chains.Select(c => Motive.FromAtoms(c.Value.Residues.SelectMany(r => r.Atoms), null, currentContext)).ToList();
            }

            return ids.Where(id => chains.ContainsKey(id)).Select(id => Motive.FromAtoms(chains[id].Residues.SelectMany(r => r.Atoms), null, currentContext)).ToList();
        }

        protected override string ToStringInternal()
        {
            return NameHelper("Chains", ids);
        }

        public ChainsQuery(IEnumerable<string> ids)
        {
            this.ids = ids.Distinct().ToArray();
        }
    }

    class SecondaryElementQuery : QueryMotive
    {
        public enum ElementType
        {
            Sheet,
            Helix
        }

        ElementType Type;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var ctx = context.RequestCurrentContext();
            if (Type == ElementType.Helix)
            {
                return ctx.Structure.PdbHelices().Select(h => Motive.FromResidues(h.Residues.Select(r => ctx.Residues[r.Identifier]), null, ctx)).ToList();
            }
            else
            {
                return ctx.Structure.PdbSheets().Select(s => Motive.FromResidues(s.Residues.Select(r => ctx.Residues[r.Identifier]), null, ctx)).ToList();
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper("SecondaryElement", Type.ToString());
        }

        public SecondaryElementQuery(ElementType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Matches any atom from the input set.
    /// </summary>
    public class GroupedAtomsQuery : QueryMotive
    {
        HashSet<string> names;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var currentContext = context.RequestCurrentContext();
            return currentContext.Structure.Atoms
                .GroupBy(a => a.ElementSymbol)
                .Where(g => names.Count == 0 || names.Contains(g.Key.ToString()))
                .Select(g => Motive.FromAtoms(g, null, currentContext))
                .ToList();
        }

        protected override string ToStringInternal()
        {
            return NameHelper("GroupedAtoms", names.OrderBy(n => n, StringComparer.Ordinal), new string[0]);
        }

        public GroupedAtomsQuery(IEnumerable<string> names)
        {
            this.names = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }
    }

}
