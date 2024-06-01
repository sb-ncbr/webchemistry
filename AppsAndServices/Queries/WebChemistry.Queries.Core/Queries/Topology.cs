namespace WebChemistry.Queries.Core.Queries
{
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using System;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// Ambient atoms. Extends atoms by n-layers.
    /// </summary>
    class ConnectedAtomsQuery : QueryUniqueMotive
    {
        int depth;
        QueryMotive subquery;


        Motive Expand(Motive m)
        {
            List<IAtom> pivots = m.Atoms.ToList(), tempPivots = new List<IAtom>();
            var atomSet = pivots.ToHashSet();

            var structure = m.Context.Structure;

            for (int i = depth; i > 0; i--)
            {
                if (pivots.Count == structure.Atoms.Count) break;

                var oldCount = atomSet.Count;
                tempPivots.Clear();
                foreach (var atom in pivots)
                {
                    var bonds = structure.Bonds[atom];
                    for (int j = 0; j < bonds.Count; j++)
                    {
                        var other = bonds[j].B;
                        if (atomSet.Add(other)) tempPivots.Add(other);
                    }
                }
                var temp = pivots;
                pivots = tempPivots;
                tempPivots = temp;
                if (oldCount == atomSet.Count) break;
            }

            return Motive.FromAtoms(atomSet, m.Name, m.Context);
        }

        protected override IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context)
        {
            foreach (var m in subquery.Execute(context))
            {
                var motive = Expand(m);
                yield return motive;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "ConnectedAtoms",
                new[] { depth.ToString(), NameOption("YieldNamedDuplicates", YieldNamedDuplicates) },
                new[] { subquery.ToString() });
        }

        public ConnectedAtomsQuery(QueryMotive query, int depth, bool yieldNamedDuplicates)
            : base(yieldNamedDuplicates)
        {
            this.depth = depth;
            this.subquery = query;
        }
    }

    /// <summary>
    /// Ambient atoms. Extends atoms by n-layers.
    /// </summary>
    class ConnectedResiduesQuery : QueryUniqueMotive
    {
        int depth;
        QueryMotive subquery;
        
        void Expand(HashSet<IAtom> current, MotiveContext context)
        {
            var structure = context.Structure;
            HashSet<PdbResidueIdentifier> residues = new HashSet<PdbResidueIdentifier>();
            foreach (var a in current.ToArray())
            {
                var ari = a.ResidueIdentifier();
                if (residues.Add(ari)) context.Residues[ari].Residue.Atoms.ForEach(x => current.Add(x));
                foreach (var bond in structure.Bonds[a])
                {
                    var bri = bond.B.ResidueIdentifier();
                    if (ari != bri)
                    {
                        if (residues.Add(bri)) context.Residues[bri].Residue.Atoms.ForEach(x => current.Add(x));
                    }
                }
            }
        }

        void Collect(HashSet<IAtom> current, MotiveContext context)
        {
            HashSet<PdbResidueIdentifier> residues = new HashSet<PdbResidueIdentifier>();
            foreach (var a in current.ToArray())
            {
                var ari = a.ResidueIdentifier();
                if (residues.Add(ari)) context.Residues[ari].Residue.Atoms.ForEach(x => current.Add(x));
            }
        }

        protected override IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context)
        {
            foreach (var m in subquery.Execute(context))
            {
                var ctx = context.RequestCurrentContext();

                var atoms = m.Atoms.Flatten().ToHashSet();
                Collect(atoms, ctx);

                for (int i = 0; i < depth; i++)
                {
                    if (atoms.Count == ctx.Structure.Atoms.Count) break;

                    var oldCount = atoms.Count;
                    Expand(atoms, ctx);
                    if (oldCount == atoms.Count) break;
                }

                var motive = new Motive(HashTrie.Create(atoms), m.Name, ctx);
                yield return motive;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "ConnectedResidues",
                new[] { depth.ToString(), NameOption("YieldNamedDuplicates", YieldNamedDuplicates) },
                new[] { subquery.ToString() });
        }

        public ConnectedResiduesQuery(QueryMotive query, int depth, bool yieldNamedDuplicates)
            : base(yieldNamedDuplicates)
        {
            this.depth = depth;
            this.subquery = query;
        }
    }

    /// <summary>
    /// A path of motifs.
    /// </summary>
    class PathQuery : QueryMotive
    {
        QueryMotive[] subqueries;
        
        void Chain(Motive current, Motive last, int index, Dictionary<IAtom, List<Motive>>[] motives, HashSet<Motive> result)
        {
            if (index == motives.Length)
            {
                result.Add(current);
                return;
            }

            var xs = motives[index];
            var bonds = last.Context.Structure.Bonds;
            last.Atoms.ForEach(a =>
            {
                foreach (var b in bonds[a])
                {
                    List<Motive> candidates;
                    if (!xs.TryGetValue(b.B, out candidates)) continue;
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var m = candidates[i];
                        if (!HashTrie.AreIntersected(current.Atoms, m.Atoms))
                        {
                            Chain(Motive.Merge(current, m, current.Context), m, index + 1, motives, result);
                        }
                    }
                }
            });
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var motives = subqueries.Skip(1).Select(q => 
                {
                    var ret = new Dictionary<IAtom, List<Motive>>();
                    foreach (var m in q.ExecuteMotive(context))
                    {
                        m.Atoms.ForEach(a =>
                        {
                            List<Motive> xs;
                            if (ret.TryGetValue(a, out xs)) xs.Add(m);
                            else ret.Add(a, new List<Motive> { m });
                        });
                    }
                    return ret;
                }).ToArray();

            HashSet<Motive> result = new HashSet<Motive>();

            foreach (var q in subqueries[0].ExecuteMotive(context))
            {
                Chain(q, q, 0, motives, result);
            }

            return result.ToArray();
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "Path",
                new string [0],
                subqueries.Select(q => q.ToString()));
        }

        public PathQuery(IEnumerable<QueryMotive> queries)
        {
            this.subqueries = queries.ToArray();
            if (subqueries.Length == 0)
            {
                throw new ArgumentException("PathQuery: at least one subquery required.");
            }
        }
    }

    /// <summary>
    /// A star of motifs.
    /// </summary>
    class StarQuery : QueryMotive
    {
        QueryMotive center;
        QueryMotive[] subqueries;

        void Star(Motive current, Motive center, int index, Dictionary<IAtom, List<Motive>>[] motives, HashSet<Motive> result)
        {
            if (index == motives.Length)
            {
                result.Add(current);
                return;
            }

            var xs = motives[index];
            var bonds = center.Context.Structure.Bonds;
            center.Atoms.ForEach(a =>
            {
                foreach (var b in bonds[a])
                {
                    List<Motive> candidates;
                    if (!xs.TryGetValue(b.B, out candidates)) continue;
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var m = candidates[i];
                        if (!HashTrie.AreIntersected(current.Atoms, m.Atoms))
                        {
                            Star(Motive.Merge(current, m, current.Context), center, index + 1, motives, result);
                        }
                    }
                }
            });
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var motives = subqueries.Select(q =>
            {
                var ret = new Dictionary<IAtom, List<Motive>>();
                foreach (var m in q.ExecuteMotive(context))
                {
                    m.Atoms.ForEach(a =>
                    {
                        List<Motive> xs;
                        if (ret.TryGetValue(a, out xs)) xs.Add(m);
                        else ret.Add(a, new List<Motive> { m });
                    });
                }
                return ret;
            }).ToArray();

            HashSet<Motive> result = new HashSet<Motive>();

            foreach (var q in center.ExecuteMotive(context))
            {
                Star(q, q, 0, motives, result);
            }

            return result.ToArray();
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "Star",
                new string[0],
                new[] { center.ToString() }.Concat(subqueries.Select(q => q.ToString())));
        }

        public StarQuery(QueryMotive center, IEnumerable<QueryMotive> queries)
        {
            this.center = center;
            this.subqueries = queries.ToArray();
        }
    }

    /// <summary>
    /// Filter those motives that are not connected to a particular motive.
    /// </summary>
    class IsConnectedToQuery : TestMotive
    {
        bool complement;
        QueryMotive subquery;

        internal override object Execute(ExecutionContext context)
        {
            var tree = context.RequestCurrentContext().Cache.GetOrCreateProximityTree(subquery, () => subquery.Execute(context));
            var motive = Where.ExecuteObject(context).MustBe<Motive>();

            var candidates = tree.GetConenctedMotives(4, motive, exclusive: true);

            var empty = candidates.IsEmpty();

            if (complement) return empty;
            else return !empty;
        }

        protected override string ToStringInternal()
        {
            return NameHelper("IsConnectedTo", new[] { NameOption("Complement", complement) }, new[] { Where.ToString(), subquery.ToString() });
        }

        public IsConnectedToQuery(bool complement, Query where, QueryMotive subquery)
            : base(where)
        {
            this.complement = complement;
            this.subquery = subquery;
        }
    }

    /// <summary>
    /// Check if the motive is connected.
    /// </summary>
    class IsConnectedQuery : TestMotive
    {
        static bool IsConnected(Motive m)
        {
            var atoms = m.Atoms;
            if (atoms.Count == 0) return true;
            var bonds = m.Context.Structure.Bonds;
            Stack<IAtom> visitStack = new Stack<IAtom>(atoms.Count);
            HashSet<IAtom> visited = new HashSet<IAtom>();
            
            var pivot = atoms.GetAny();
            visitStack.Push(pivot);
            visited.Add(pivot);

            int markedCount = 0;

            while (visitStack.Count > 0)
            {
                var current = visitStack.Pop();
                markedCount++;
                var bs = bonds[current];
                for (int i = 0; i < bs.Count; i++)
                {
                    var b = bs[i].B;
                    if (atoms.Contains(b) && visited.Add(b))
                    {
                        visitStack.Push(b);
                    }
                }
            }
            
            return atoms.Count == markedCount;
        }

        internal override object Execute(ExecutionContext context)
        {
            var motive = Where.ExecuteObject(context).MustBe<Motive>();
            return IsConnected(motive);
        }

        protected override string ToStringInternal()
        {
            return NameHelper("IsConnected", new string[0], new[] { Where.ToString() });
        }

        public IsConnectedQuery(Query where)
            : base(where)
        {
        }
    }
}
