namespace WebChemistry.Queries.Core.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Geometry;

    /// <summary>
    /// Matches if all subqueries are within a given distance from each other.
    /// </summary>
    class ClusterQuery : QueryMotive
    {
        QueryMotive[] subqueries;
        double maxDistance;

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var matches = subqueries.Select(q => q.Execute(context).AsList()).ToArray();

            int count = matches[0].Count;
            int pivotIndex = 0;
            bool isEmpty = false;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].Count == 0)
                {
                    isEmpty = true;
                    break;
                }
                if (count > matches[i].Count)
                {
                    count = matches[i].Count;
                    pivotIndex = i;
                }
            }
                        
            if (!isEmpty)
            {
                var pivot = matches[pivotIndex];
                var trees = subqueries.Select((q, i) => context.RequestCurrentContext().Cache.GetOrCreateProximityTree(q, matches[i])).ToArray();

                HashSet<Motive> yielded = new HashSet<Motive>();

                foreach (var m in pivot)
                {
                    var nears = trees.Select(t => t.GetCloseMotives(maxDistance, m)).ToArray();
                    var x = m;
                    isEmpty = false;
                    for (int i = 0; i < nears.Length; i++)
                    {
                        bool empty = true;
                        foreach (var n in nears[i])
                        {
                            empty = false;
                            x = Motive.Merge(x, n, context.RequestCurrentContext());
                        }
                        if (empty)
                        {
                            isEmpty = true;
                            break;
                        }
                    }
                    if (!isEmpty && yielded.Add(x)) yield return x;
                }
            }
        }

        protected override string ToStringInternal()
        {
            return "Cluster(" + maxDistance.ToStringInvariant("0.00") + ")[" + string.Join(", ", subqueries.Select(q => q.ToString())) + "]";
        }

        public ClusterQuery(double maxDistance, IEnumerable<Query> subqueries)
        {
            this.maxDistance = maxDistance;
            this.subqueries = subqueries.Select(q => q as QueryMotive).ToArray();
        }
    }


    public class DistanceClusterQuery : QueryMotive
    {
        QueryMotive[] subqueries;
        double[][] distanceMatrixMin, distanceMatrixMax;
        
        class PatternWrapTree
        {
            K3DTree<PatternWrap> tree;
            double maxRadius;
            
            public IEnumerable<PatternWrap> GetCloseNonIdenticalPatterns(double maxDistance, PatternWrap m)
            {
                var center = m.Pattern.Center;
                maxDistance = maxDistance + m.Pattern.Radius + maxRadius;
                var near = tree.NearestRadius(center, maxDistance);
                for (int i = 1; i < near.Count; i++)
                {
                    var c = near[i];
                    if (Motive.AreNear(maxDistance, m.Pattern, c.Value.Pattern)) yield return c.Value;
                }
            }
            
            public PatternWrapTree(IEnumerable<PatternWrap> motives)
            {
                this.maxRadius = double.MinValue;

                var ma = motives.AsList();
                tree = new K3DTree<PatternWrap>(motives, m => m.Pattern.Center, method: K3DPivotSelectionMethod.Average);

                for (int i = 0; i < ma.Count; i++)
                {
                    var r = ma[i].Pattern.Radius;
                    if (r > maxRadius) maxRadius = r;
                }
            }
        }

        class PatternWrap : IEquatable<PatternWrap>
        {
            public Motive Pattern;
            public int Index;
            public List<int> Labels;

            string signature;
            public string Signature
            {
                get
                {
                    if (signature != null) return signature;

                    signature = Labels.OrderBy(l => l).JoinBy(" ");
                    return signature;
                }
            }

            public override int GetHashCode()
            {
                return Pattern.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Equals((PatternWrap)obj);
            }

            public bool Equals(PatternWrap other)
            {
                return this.Pattern.Equals(other.Pattern);
            }

            public PatternWrap(Motive pattern, int index, int initialLabel)
            {
                this.Pattern = pattern;
                this.Index = index;
                this.Labels = new List<int> { initialLabel };
            }
        }


        IStructure BuildGraph(List<PatternWrap> patterns)
        {
            var tree = new PatternWrapTree(patterns);
            var maxDistance = distanceMatrixMax.SelectMany(r => r).Max();
            var minDistance = distanceMatrixMin.SelectMany(r => r).Min();

            //var atoms = patterns.Select((p, i) => Framework.Core.Pdb.PdbAtom.Create(i, ElementSymbols.C, position: p.Pattern.Center, name: "C", 
            //    residueSequenceNumber: p.Pattern.Atoms.Flatten()[0].PdbResidueSequenceNumber(), residueName: p.Pattern.Atoms.Flatten()[0].PdbResidueName())).ToArray();
            var atoms = patterns.Select((p, i) => Framework.Core.Pdb.PdbAtom.Create(i, ElementSymbols.C, position: p.Pattern.Center)).ToArray();
            var bonds = new HashSet<IBond>();

            //Console.WriteLine("dist {0} {1}", maxDistance, minDistance);

            foreach (var p in patterns)
            {
                foreach (var n in tree.GetCloseNonIdenticalPatterns(maxDistance, p))
                {
                    var distance = Motive.Distance(n.Pattern, p.Pattern);
                    //Console.WriteLine("-----------------");
                    //Console.WriteLine("A: {0} {1}", p.Pattern.Atoms.Flatten()[0].PdbResidueSequenceNumber(), p.Pattern.Atoms.Flatten()[0].PdbResidueName());
                    //Console.WriteLine("B: {0} {1}", n.Pattern.Atoms.Flatten()[0].PdbResidueSequenceNumber(), n.Pattern.Atoms.Flatten()[0].PdbResidueName());
                    //Console.WriteLine("dist {0}", distance);

                    if (distance >= minDistance)
                    {

                        for (int i = 0; i < p.Labels.Count; i++)
                        {
                            for (int j = 0; j < n.Labels.Count; j++)
                            {                                
                                if (distanceMatrixMin[p.Labels[i]][n.Labels[j]] <= distance && distanceMatrixMax[p.Labels[i]][n.Labels[j]] >= distance)
                                {
                                    bonds.Add(Bond.Create(atoms[p.Index], atoms[n.Index]));
                                    i = p.Labels.Count;
                                    break;
                                }
                            }
                        }                        
                    }
                }
            }

            return Structure.Create("core-graph", AtomCollection.Create(atoms), BondCollection.Create(bonds));
        }

        bool IsClique(List<IAtom> atoms, IAtom extra, IStructure graph, bool[] tags)
        {
            var bonds = graph.Bonds[extra];
            if (bonds.Count < atoms.Count) return false;

            for (int i = 0; i < atoms.Count; i++) tags[atoms[i].Id] = false;
            for (int i = 0; i < bonds.Count; i++) tags[bonds[i].B.Id] = true;
            for (int i = 0; i < atoms.Count; i++) if (!tags[atoms[i].Id]) return false;
            return true;
        }

        List<HashTrie<IAtom>> FindKCliques(IStructure graph, bool[] tags, List<HashTrie<IAtom>> previous)
        {
            var ret = new List<HashTrie<IAtom>>();
            var set = new HashSet<HashTrie<IAtom>>();
            var tested = new HashSet<IAtom>();
            var atoms = new List<IAtom>();
            var bonds = graph.Bonds;

            foreach (var core in previous)
            {
                atoms.Clear();
                core.VisitLeaves(a => atoms.Add(a));
                tested.Clear();

                foreach (var a in atoms)
                {
                    foreach (var b in bonds[a])
                    {
                        if (core.ContainsKey(b.B.Id) || !tested.Add(b.B)) continue;

                        if (IsClique(atoms, b.B, graph, tags))
                        {
                            var m = core.Add(b.B);
                            if (set.Add(m)) ret.Add(m);
                        }
                    }
                }
            }

            return ret;
        }

        bool TestLabels(List<PatternWrap> patterns, int k, HashTrie<IAtom> clique)
        {
            var groups = clique.Flatten()
                .GroupBy(a => patterns[a.Id].Signature, StringComparer.Ordinal)
                .ToArray();

            return groups.All(g => g.Count() == patterns[g.First().Id].Labels.Count) && groups.Sum(g => g.Count()) == k;

            //return clique.Flatten().SelectMany(a => patterns[a.Id].Labels).ToHashSet().Count == k;
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var subPatterns = subqueries.Select(q => q.Execute(context).ToArray()).ToArray();
            var patternMap = new Dictionary<Motive, PatternWrap>(subPatterns.Sum(p => p.Length));
            var allPatterns = new List<PatternWrap>();
            
            for (int i = 0; i < subqueries.Length; i++)
            {
                foreach (var p in subPatterns[i])
                {
                    PatternWrap w;
                    if (patternMap.TryGetValue(p, out w))
                    {
                        if (!w.Labels.Contains(i)) w.Labels.Add(i);
                    }
                    else
                    {
                        var n = new PatternWrap(p, allPatterns.Count, i);
                        patternMap[p] = n;
                        allPatterns.Add(n);
                    }
                }
            }
            
            var graph = BuildGraph(allPatterns);
            
            //System.IO.File.WriteAllText(@"E:\test\pqcluster.mol2", graph.ToMol2String(chargeSelector: a => 0.0));

            var ctx = context.CurrentContext;
            var twoCliques = graph.Bonds.Select(b => HashTrie.Create<IAtom>(new[] { b.A, b.B })).ToList();

            var ret = twoCliques;
            if (subqueries.Length > 2)
            {
                var tags = new bool[graph.Atoms.Count];
                for (int k = 3; k <= subqueries.Length; k++)
                {
                    ret = FindKCliques(graph, tags, ret);
                }
            }
            
            return ret
                .Where(xs => TestLabels(allPatterns, subqueries.Length, xs))
                .Select(xs =>
                {
                    var list = xs.Flatten();
                    var atoms = allPatterns[list[0].Id].Pattern.Atoms.Flatten().ToList();
                    for (int i = 1; i < list.Count; i++)
                    {
                        atoms.AddRange(allPatterns[list[i].Id].Pattern.Atoms.Flatten());
                    }                    
                    return Motive.FromAtoms(atoms, null, ctx);
                })
                .ToList();
        }

        protected override string ToStringInternal()
        {
            var matrixMin = "{" + string.Join(",", distanceMatrixMin.Select(r => "{" + string.Join(",", r.Select(v => v.ToStringInvariant("0.000"))) + "}")) + "}";
            var matrixMax = "{" + string.Join(",", distanceMatrixMax.Select(r => "{" + string.Join(",", r.Select(v => v.ToStringInvariant("0.000"))) + "}")) + "}";
            return NameHelper("DistanceCluster", new[] { matrixMin, matrixMax }, subqueries.Select(q => q.ToString()));
        }

        void Init(double[][] distanceMatrixMin, double[][] distanceMatrixMax)
        {
            if (distanceMatrixMin.Length != subqueries.Length - 1)
            {
                throw new InvalidOperationException("Invalid min distance matrix dimensions.");
            }

            if (distanceMatrixMax.Length != subqueries.Length - 1)
            {
                throw new InvalidOperationException("Invalid max distance matrix dimensions.");
            }

            for (int i = 0; i < subqueries.Length - 1; i++)
            {
                if (distanceMatrixMin[i].Length != i + 1)
                {
                    throw new InvalidOperationException("Invalid min distance matrix dimensions.");
                }

                if (distanceMatrixMax[i].Length != i + 1)
                {
                    throw new InvalidOperationException("Invalid max distance matrix dimensions.");
                }
            }

            var min = new double[subqueries.Length][];
            var max = new double[subqueries.Length][];
            for (int i = 0; i < subqueries.Length; i++)
            {
                min[i] = new double[subqueries.Length];
                max[i] = new double[subqueries.Length];
            }

            for (int i = 0; i < subqueries.Length - 1; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    if (distanceMatrixMin[i][j] > distanceMatrixMax[i][j])
                    {
                        throw new InvalidOperationException(string.Format("Invalid distance matrix. Min value at position ({0},{1}) is greater than the max value.", i, j));
                    }


                    min[i + 1][j] = distanceMatrixMin[i][j];
                    min[j][i + 1] = min[i + 1][j];

                    max[i + 1][j] = distanceMatrixMax[i][j];
                    max[j][i + 1] = max[i + 1][j];
                }
            }

            //var matrixMin = string.Join("\n", min.Select(r => "{" + string.Join(",", r.Select(v => v.ToStringInvariant("0.000"))) + "}"));
            //var matrixMax = string.Join("\n", max.Select(r => "{" + string.Join(",", r.Select(v => v.ToStringInvariant("0.000"))) + "}"));
            //Console.WriteLine(matrixMin);
            //Console.WriteLine("------------");
            //Console.WriteLine(matrixMax);

            this.distanceMatrixMax = max;
            this.distanceMatrixMin = min;
        }

        public DistanceClusterQuery(IEnumerable<Query> subqueries, double[][] distanceMatrixMin, double[][] distanceMatrixMax)
        {
            this.subqueries = subqueries.Select(q => q as QueryMotive).ToArray();

            if (this.subqueries.Length < 2)
            {
                throw new ArgumentException("DistanceCluster must operate at least on 2 patterns.");
            }
            
            Init(distanceMatrixMin, distanceMatrixMax);
        }
    }


    /////// <summary>
    /////// Clusters the queries based on a distance matrix. TODO
    /////// </summary>
    ////class DistanceClusterQuery : QueryMotive
    ////{
    ////    struct SignatureElement
    ////    {
    ////        public readonly int Label;
    ////        public readonly double Distance;
    ////    }

    ////    class Signature
    ////    {
    ////        public SignatureElement[] Elements;

    ////        public static Signature Create(List<SignatureElement> elems)
    ////        {
    ////            return new Signature { Elements = elems.OrderBy(e => e.Label).ThenBy(e => e.Distance).ToArray() };
    ////        }

    ////        public static bool AreCompatible(Signature pivot, Signature sig, double tolerance)
    ////        {
    ////            int pivotIndex, sigIndex = 0;

    ////            var threshold = 1 - tolerance;
    ////            var pe = pivot.Elements;
    ////            var se = sig.Elements;
    ////            var sigLen = se.Length;

    ////            for (pivotIndex = 0; pivotIndex < pe.Length; pivotIndex++)
    ////            {
    ////                var p = pe[pivotIndex];
    ////                var pDist = p.Distance;
    ////                while (sigIndex < sigLen)
    ////                {
    ////                    var s = se[sigIndex];

    ////                    if (s.Label < p.Label)
    ////                    {
    ////                        sigIndex++;
    ////                        continue;
    ////                    }
    ////                    else if (s.Label > p.Label) 
    ////                    {
    ////                        return false;
    ////                    }
    ////                    else // if (s.Label == p.Label)
    ////                    {
    ////                        sigIndex++;
    ////                        var sDist = s.Distance;
    ////                        double ratio = pDist > sDist ? sDist / pDist : pDist / sDist;
    ////                        if (ratio >= threshold) break;
    ////                        else continue;
    ////                    }
    ////                }
    ////            }
                
    ////            return pivotIndex == pe.Length; 
    ////        }
    ////    }

    ////    class Vertex : IEquatable<Vertex>
    ////    {
    ////        public readonly int Label;
    ////        public readonly int Id;
    ////        public readonly Motive Motive;
    ////        public readonly List<SignatureElement> SignatureElements;

    ////        public Vertex(int label, int id, Motive motive, int signatureSize)
    ////        {
    ////            this.Label = label;
    ////            this.Id = id;
    ////            this.Motive = motive;
    ////            this.SignatureElements = new List<SignatureElement>();
    ////        }

    ////        public bool Equals(Vertex other)
    ////        {
    ////            return this.Id == other.Id;
    ////        }

    ////        public override bool Equals(object obj)
    ////        {
    ////            return (obj as Vertex).Id == Id;
    ////        }

    ////        public override int GetHashCode()
    ////        {
    ////            return Id;
    ////        }
    ////    }

    ////    class Edge : IEquatable<Edge>
    ////    {
    ////        readonly uint Id;
    ////        public readonly Vertex A, B;
    ////        public readonly double Weight;

    ////        public Edge(Vertex a, Vertex b, double weight)
    ////        {
    ////            this.A = a;
    ////            this.A = b;

    ////            if (a.Id < b.Id) this.Id = ((uint)a.Id << 16) | (uint)b.Id;
    ////            else this.Id = ((uint)b.Id << 16) | (uint)a.Id;
    ////        }

    ////        public bool Equals(Edge other)
    ////        {
    ////            return this.Id == other.Id;
    ////        }

    ////        public override bool Equals(object obj)
    ////        {
    ////            return (obj as Edge).Id == Id;
    ////        }

    ////        public override int GetHashCode()
    ////        {
    ////            return Id.GetHashCode();
    ////        }
    ////    }

    ////    QueryMotive[] subqueries;
    ////    double[][] distanceMatrix;
    ////    double tolerance;
                
    ////    internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
    ////    {
    ////        //var matches = uniqueSubqueries.Select(q => q.Execute(context).AsList()).ToArray();
    ////        //var trees = uniqueSubqueries.Select((q, i) => context.RequestCurrentContext().Cache.GetOrCreateProximityTree(q, matches[i])).ToArray();

    ////        return null;
    ////    }

    ////    protected override string ToStringInternal()
    ////    {
    ////        var matrix = "{" + string.Join(",", distanceMatrix.Select(r => "{" + string.Join(",", r.Select(v => v.ToStringInvariant("0.000"))) + "}")) + "}";
    ////        return "DistanceCluster(" + matrix + ")[" + string.Join(",", subqueries.Select(q => q.ToString())) + "]";
    ////    }

    ////    void Init()
    ////    {
    ////        var queryGroups = subqueries
    ////            .Select((q, i) => new { Index = i, Query = q })
    ////            .GroupBy(q => q.Query.ToString())
    ////            .Select((g, i) =>
    ////            {
    ////                var indices = g.Select(q => q.Index).ToArray();
    ////                var pivot = distanceMatrix[indices[0]];
    ////                return new { Id = i, Query = g.First().Query, Distances = indices.Skip(1).Select(k => pivot[k]).OrderBy(v => v).ToArray() };
    ////            })
    ////            .ToArray();
    ////    }

    ////    public DistanceClusterQuery(IEnumerable<Query> subqueries, double[][] distanceMatrix, double tolerance)
    ////    {
    ////        this.subqueries = subqueries.Select(q => q as QueryMotive).ToArray();
    ////        this.distanceMatrix = distanceMatrix;
    ////        this.tolerance = tolerance;
    ////        Init();
    ////    }
    ////}

    /// <summary>
    /// Matches if all subqueries are within a given distance from each other.
    /// </summary>
    class NearQuery : QueryMotive
    {
        QueryMotive[] subqueries, uniqueSubqueries;

        Dictionary<string, int> counts;

        double maxDistance;

        bool VerifyCounts(Motive m, ExecutionContext context)
        {
            for (int i = 0; i < uniqueSubqueries.Length; i++)
            {
                var q = uniqueSubqueries[i];
                var count = CountQuery.Count(q, m, context);
                if (count != counts[q.ToString()]) return false;
            }

            return true;
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var matches = uniqueSubqueries.Select(q => q.Execute(context).AsList()).ToArray();

            int count = matches[0].Count;
            int pivotIndex = 0;
            bool isEmpty = false;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].Count == 0)
                {
                    isEmpty = true;
                    break;
                }
                if (count > matches[i].Count)
                {
                    count = matches[i].Count;
                    pivotIndex = i;
                }
            }

            if (!isEmpty)
            {
                var pivot = matches[pivotIndex];
                var trees = uniqueSubqueries.Select((q, i) => context.RequestCurrentContext().Cache.GetOrCreateProximityTree(q, matches[i])).ToArray();

                HashSet<Motive> yielded = new HashSet<Motive>();

                foreach (var m in pivot)
                {
                    var nears = trees.Select(t => t.GetCloseMotives(maxDistance, m)).ToArray();
                    var x = m;
                    isEmpty = true;
                    for (int i = 0; i < nears.Length; i++)
                    {
                        bool empty = true;
                        foreach (var n in nears[i])
                        {
                            empty = false;
                            x = Motive.Merge(x, n, context.RequestCurrentContext());
                        }
                        if (empty)
                        {
                            isEmpty = true;
                            break;
                        }
                    }
                    if (!isEmpty && yielded.Add(x) && VerifyCounts(x, context)) yield return x;
                }
            }
        }

        protected override string ToStringInternal()
        {
            return "Near(" + maxDistance.ToStringInvariant("0.00") + ")[" + string.Join(",", subqueries.Select(q => q.ToString())) + "]";
        }

        public NearQuery(double maxDistance, IEnumerable<Query> subqueries)
        {
            this.maxDistance = maxDistance;
            this.subqueries = subqueries.Select(q => q as QueryMotive).ToArray();
            this.uniqueSubqueries = this.subqueries.Distinct(q => q.ToString()).ToArray();
            this.counts = this.subqueries.GroupBy(q => q.ToString()).ToDictionary(g => g.Key, g => g.Count());
        }
    }

    /// <summary>
    /// Expands by all atoms in a given radius.
    /// </summary>
    class AmbientAtomsQuery : QueryUniqueMotive
    {
        QueryMotive subquery;
        double maxDistance;
        bool ignoreWaters;
        bool excludeBase;

        public Motive Expand(Motive m, double maxDistance)
        {
            if (excludeBase)
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();

                HashSet<IAtom> newAtoms = new HashSet<IAtom>();

                m.Atoms.VisitLeaves(a =>
                    {
                        foreach (var x in tree.NearestRadius(a.InvariantPosition, maxDistance))
                        {
                            if (ignoreWaters && x.Value.IsWater()) continue;
                            if (!m.Atoms.Contains(x.Value)) newAtoms.Add(x.Value);
                        }
                    });

                return new Motive(HashTrie.Create(newAtoms), m.Name, m.Context);
            }
            else
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();
                var ret = m.Atoms;

                var newAtoms = m.Atoms.SelectMany(a => tree.NearestRadius(a.InvariantPosition, maxDistance))
                    .Select(a => a.Value);

                if (ignoreWaters) newAtoms = newAtoms.Where(a => !a.IsWater());

                //newAtoms = newAtoms.Distinct(a => a.Id).ToArray();

                return new Motive(HashTrie.Create(ret.Flatten().Concat(newAtoms)), m.Name, m.Context);
            }
        }

        protected override IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context)
        {
            foreach (var m in subquery.Execute(context))
            {
                var e = Expand(m, maxDistance);
                if (e.Atoms.Count > 0) yield return e;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "AmbientAtoms",
                new[] { maxDistance.ToStringInvariant("0.000"), NameOption("IgnoreWaters", ignoreWaters), NameOption("ExcludeBase", excludeBase), NameOption("YieldNamedDuplicates", YieldNamedDuplicates) },
                new[] { subquery.ToString() });
        }

        public AmbientAtomsQuery(double maxDistance, QueryMotive query, bool ignoreWaters, bool excludeBase, bool yieldNamedDuplicates)
            : base(yieldNamedDuplicates)
        {
            this.maxDistance = maxDistance;
            this.subquery = query;
            this.ignoreWaters = ignoreWaters;
            this.excludeBase = excludeBase;
        }
    }

    /// <summary>
    /// Expands by all residues in a given radius.
    /// </summary>
    class AmbientResiduesQuery : QueryUniqueMotive
    {
        QueryMotive subquery;
        double maxDistance;
        bool ignoreWaters;
        bool excludeBase;

        public Motive Expand(Motive m, double maxDistance)
        {
            if (excludeBase)
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();
                var ret = HashTrie<IAtom>.Empty;

                HashSet<IAtom> newAtoms = new HashSet<IAtom>();

                m.Atoms.VisitLeaves(a =>
                    {
                        foreach (var x in tree.NearestRadius(a.InvariantPosition, maxDistance))
                        {
                            if (ignoreWaters && x.Value.IsWater()) continue;
                            if (!m.Atoms.Contains(x.Value)) newAtoms.Add(x.Value);
                        }
                    });

                var residues = newAtoms
                    .GroupBy(a => a.ResidueIdentifier())
                    .Select(g => m.Context.Residues[g.Key]);

                var atoms = HashTrie.Create(residues.SelectMany(r => r.Residue.Atoms));
                return new Motive(atoms, m.Name, m.Context);
            }
            else
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();
                var residues = m.Atoms.SelectMany(a => tree.NearestRadius(a.InvariantPosition, maxDistance))
                    .GroupBy(a => a.Value.ResidueIdentifier())
                    .Select(g => m.Context.Residues[g.Key]);

                if (ignoreWaters) residues = residues.Where(r => !r.Residue.IsWater);

                // center can be a water, thus the union.
                var atoms = HashTrie.Create(m.Atoms.Flatten().Concat(residues.SelectMany(r => r.Residue.Atoms)));
                return new Motive(atoms, m.Name, m.Context);
            }
        }

        protected override IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context)
        {
            foreach (var m in subquery.Execute(context))
            {
                var e = Expand(m, maxDistance);
                if (e.Atoms.Count > 0) yield return e;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "AmbientResidues",
                new[] { maxDistance.ToStringInvariant("0.000"), NameOption("IgnoreWaters", ignoreWaters), NameOption("ExcludeBase", excludeBase), NameOption("YieldNamedDuplicates", YieldNamedDuplicates) },
                new[] { subquery.ToString() });
        }

        public AmbientResiduesQuery(double maxDistance, QueryMotive query, bool ignoreWaters, bool excludeBase, bool yieldNamedDuplicates)
            : base(yieldNamedDuplicates)
        {
            this.maxDistance = maxDistance;
            this.subquery = query;
            this.ignoreWaters = ignoreWaters;
            this.excludeBase = excludeBase;
        }
    }

    /// <summary>
    /// Expands by all atoms in a given radius.
    /// </summary>
    class SpherifyQuery : QueryUniqueMotive
    {
        QueryMotive subquery;
        double maxDistance;
        bool ignoreWaters;
        bool excludeBase;

        public Motive Expand(Motive m)
        {
            if (excludeBase)
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();

                HashSet<IAtom> newAtoms = new HashSet<IAtom>();
                
                foreach (var x in tree.NearestRadius(m.Center, maxDistance))
                {
                    if (ignoreWaters && x.Value.IsWater()) continue;
                    if (!m.Atoms.Contains(x.Value)) newAtoms.Add(x.Value);
                }

                return new Motive(HashTrie.Create(newAtoms), m.Name, m.Context);
            }
            else
            {
                var tree = m.Context.Structure.InvariantKdAtomTree();
                var ret = m.Atoms;
                var newAtoms = tree.NearestRadius(m.Center, maxDistance).Select(a => a.Value);
                if (ignoreWaters) newAtoms = newAtoms.Where(a => !a.IsWater());
                return new Motive(HashTrie.Create(ret.Flatten().Concat(newAtoms)), m.Name, m.Context);
            }
        }

        protected override IEnumerable<Motive> ExecuteMotiveInternal(ExecutionContext context)
        {
            foreach (var m in subquery.Execute(context))
            {
                var e = Expand(m);
                if (e.Atoms.Count > 0) yield return e;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper(
                "Spherify",
                new[] { maxDistance.ToStringInvariant("0.000"), NameOption("IgnoreWaters", ignoreWaters), NameOption("ExcludeBase", excludeBase), NameOption("YieldNamedDuplicates", YieldNamedDuplicates) },
                new[] { subquery.ToString() });
        }

        public SpherifyQuery(double maxDistance, QueryMotive query, bool ignoreWaters, bool excludeBase, bool yieldNamedDuplicates)
            : base(yieldNamedDuplicates)
        {
            this.maxDistance = maxDistance;
            this.subquery = query;
            this.ignoreWaters = ignoreWaters;
            this.excludeBase = excludeBase;
        }
    }

    /// <summary>
    /// Fills the motive with atoms in a circumsphere.
    /// </summary>
    class FilledQuery : QueryMotive
    {
        QueryMotive subquery;
        double radiusFactor;
        bool ignoreWaters;

        Motive FillLinear(Motive m, IStructure s)
        {
            var center = m.Center;
            var radius = m.Radius * radiusFactor;
            var radiusSq = radius * radius;

            var atoms = m.Atoms;

            foreach (var a in s.Atoms)
            {
                if (ignoreWaters && a.IsWater()) continue;
                if (a.InvariantPosition.DistanceToSquared(center) <= radiusSq)
                {
                    atoms = atoms.Add(a);
                }
            }

            return new Motive(atoms, m.Name, m.Context);
        }

        Motive FillTree(Motive m, KDAtomTree t)
        {
            var center = m.Center;
            var radius = m.Radius * radiusFactor;
            var atoms = m.Atoms;
            foreach (var a in t.NearestRadius(center, radius))
            {
                if (ignoreWaters && a.Value.IsWater()) continue;
                atoms = atoms.Add(a.Value);
            }
            return new Motive(atoms, m.Name, m.Context);
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            HashSet<Motive> yielded = new HashSet<Motive>();
            var structure = context.RequestCurrentContext().Structure;

            if (structure.HasInvariantKdAtomTree())
            {
                var tree = structure.InvariantKdAtomTree();
                foreach (var m in subquery.Execute(context))
                {
                    var e = FillTree(m, tree);
                    if (yielded.Add(e)) yield return e;
                }
            }
            else
            {
                var inner = subquery.Execute(context).AsList();
                if (inner.Count <= 2 * Math.Log(structure.Atoms.Count))
                {
                    foreach (var m in inner)
                    {
                        var e = FillLinear(m, structure);
                        if (yielded.Add(e)) yield return e;
                    }
                }
                else
                {
                    var tree = structure.InvariantKdAtomTree();
                    foreach (var m in inner)
                    {
                        var e = FillTree(m, tree);
                        if (yielded.Add(e)) yield return e;
                    }
                }
            }
        }

        protected override string ToStringInternal()
        {
            return "Filled(" + radiusFactor.ToStringInvariant("0.000") + ",IgnoreWaters=" + ignoreWaters + ")[" + subquery.ToString() + "]";
        }

        public FilledQuery(QueryMotive query, double radiusFactor, bool ignoreWaters)
        {
            this.radiusFactor = radiusFactor;
            this.subquery = query;
            this.ignoreWaters = ignoreWaters;
        }
    }

    /// <summary>
    /// nearest distance.
    /// </summary>
    class NearestDistanceToQuery : QueryValueBase
    {
        Query motive;
        QueryMotive subquery;

        internal override dynamic Execute(ExecutionContext context)
        {
            var tree = context.RequestCurrentContext().Cache.GetOrCreateProximityTree(subquery, () => subquery.Execute(context));
            var motive = this.motive.ExecuteObject(context).MustBe<Motive>();

            var nearest = tree.GetNearestMotive(motive);
            return Motive.Distance(motive, nearest);
        }

        protected override string ToStringInternal()
        {
            return string.Format("NearestDistanceTo[{0},{1}]", motive,subquery);
        }

        public NearestDistanceToQuery(Query motive, QueryMotive subquery)
        {
            this.motive = motive;
            this.subquery = subquery;
        }
    }

    /// <summary>
    /// Fills the motive with atoms in a circumsphere.
    /// </summary>
    class Stack2Query : QueryMotive
    {
        QueryMotive query1, query2;
        double minCenterDistance, maxCenterDistance, minProjectedDistance, maxProjectedDistance, minAngleRad, maxAngleRad;
        


        static void Outer(MathHelpers.ColumnMajorMatrix3x3 target, Motive m)
        {
            var center = m.Center;
            var atoms = m.Atoms.Flatten();
            target.Reset();

            for (var i = 0; i < atoms.Count; i++)
            {
                var p = atoms[i].Position - center;
                target[0, 0] += p.X * p.X; target[0, 1] += p.X * p.Y; target[0, 2] += p.X * p.Z;
                target[1, 0] += p.Y * p.X; target[1, 1] += p.Y * p.Y; target[1, 2] += p.Y * p.Z;
                target[2, 0] += p.Z * p.X; target[2, 1] += p.Z * p.Y; target[2, 2] += p.Z * p.Z;
            }

        } 

        static Plane3D GetPlane(Motive m, MathHelpers.EvdCache cache)
        {
            var center = m.Center;
            Outer(cache.Matrix, m);
            var ev = cache.Matrix;
            var sign = ev[2, 0] >= 0 ? 1 : -1;
            var d = -(center.X * ev[0, 0] + center.Y * ev[1, 0] + center.Z * ev[2, 0]);
            return new Plane3D(sign * ev[0, 0], sign * ev[1, 0], sign * ev[2, 0], sign * d);
        }

        bool AreStacked(Motive a, Motive b, MathHelpers.EvdCache cache)
        {
            if (a.Equals(b) || a.Atoms.Count < 3 || b.Atoms.Count < 3) return false;

            var centerDist = a.Center.DistanceTo(b.Center);//  p2.DistanceTo(a.Center);
            if (centerDist > maxCenterDistance || centerDist < minCenterDistance) return false;
            
            var p1 = GetPlane(a, cache);
            var p2 = GetPlane(b, cache);

            var angle = Math.Acos(Vector3D.DotProduct(p1.Normal, p2.Normal));
            if (angle > maxAngleRad || angle < minAngleRad) return false;


            var projDist = p1.ProjectToPlane(b.Center).DistanceTo(a.Center);
            if (projDist > maxProjectedDistance || projDist < minProjectedDistance) return false;
            projDist = p2.ProjectToPlane(a.Center).DistanceTo(b.Center);
            if (projDist > maxProjectedDistance || projDist < minProjectedDistance) return false;

            //Console.WriteLine("A: {0}\nB: {1}\nD <{2}, {3}>\nR {4}", p1, p2, d1, d2, angle);


            return true;
        }

        internal override IEnumerable<Motive> ExecuteMotive(ExecutionContext context)
        {
            var ctx = context.CurrentContext;
            var xs = query1.ExecuteMotive(context).AsList();
            var cache = new MathHelpers.EvdCache();

            if (query1.ToString().EqualOrdinal(query2.ToString()))
            {
                var result = new List<Motive>();

                for (var i = 0; i < xs.Count - 1; i++)
                {
                    for (var j = i + 1; j < xs.Count; j++)
                    {
                        if (AreStacked(xs[i], xs[j], cache))
                        {
                            result.Add(Motive.Merge(xs[i], xs[j], ctx));
                            //Console.WriteLine("Passed");
                        }
                        //Console.WriteLine("--------------");
                    }
                }


                return result;
            }
            else
            {

                var ys = query2.ExecuteMotive(context).AsList();

                var result = new HashSet<Motive>();

                foreach (var a in xs)
                {
                    foreach (var b in ys)
                    {
                        if (AreStacked(a, b, cache))
                        {
                            result.Add(Motive.Merge(a, b, ctx));
                            //Console.WriteLine("Passed");
                        }
                        //Console.WriteLine("--------------");
                    }
                }

                return result;
            }
        }

        protected override string ToStringInternal()
        {
            return NameHelper("Stack2", 
                new[] {
                    minCenterDistance.ToStringInvariant("0.0000"), maxCenterDistance.ToStringInvariant("0.0000"),
                    minProjectedDistance.ToStringInvariant("0.0000"), maxProjectedDistance.ToStringInvariant("0.0000"),
                    minAngleRad.ToStringInvariant("0.0000"), maxAngleRad.ToStringInvariant("0.0000") },
                new[] { query1.ToString(), query2.ToString() });
        }

        public Stack2Query(double minDistance, double maxDistance, double minProjDist, double maxProjDist, double minAngleDeg, double maxAngleDeg, QueryMotive query1, QueryMotive query2)
        {
            this.minCenterDistance = minDistance;
            this.maxCenterDistance = maxDistance;
            this.minProjectedDistance = minProjDist;
            this.maxProjectedDistance = maxProjDist;
            this.minAngleRad = minAngleDeg * Math.PI / 180.0;
            this.maxAngleRad = maxAngleDeg * Math.PI / 180.0;
            this.query1 = query1;
            this.query2 = query2;
        }
    }

    #region EVD 3x3

    namespace MathHelpers
    {
        class ColumnMajorMatrix3x3
        {
            double[] data;

            public double[] Data { get { return data; } }

            public double this[int i, int j]
            {
                get { return data[3 * j + i]; }
                set { data[3 * j + i] = value; }
            }

            public void Reset()
            {
                for (int i = 0; i < data.Length; i++) data[i] = 0.0;
            }

            public ColumnMajorMatrix3x3()
            {
                data = new double[16];
            }
        }

        class EvdCache
        {
            public ColumnMajorMatrix3x3 Matrix;
            public double[] EigenValues, D, E;

            public EvdCache()
            {
                Matrix = new ColumnMajorMatrix3x3();
                EigenValues = new double[3];
                D = new double[3];
                E = new double[3];
            }
        }

        /// <summary>
        /// Borrowed from Math.Net Numerics. Does not create a billion new matrices for each computation and instead reuses the existing one.
        /// </summary>
        class Evd3x3
        {
            public static void Compute(EvdCache cache)
            {
                SymmetricEigenDecomp(3, cache.Matrix.Data, cache.EigenValues, cache.D, cache.E);
            }

            static void SymmetricEigenDecomp(int order, double[] matrixEv, double[] vectorEv, double[] d, double[] e)
            {
                //var d = new double[order];
                //var e = new double[order];
                for (int i = 0; i < order; i++)
                {
                    e[i] = 0.0;
                }

                var om1 = order - 1;
                for (var i = 0; i < order; i++)
                {
                    d[i] = matrixEv[i * order + om1];
                }

                SymmetricTridiagonalize(matrixEv, d, e, order);
                SymmetricDiagonalize(matrixEv, d, e, order);

                for (var i = 0; i < order; i++)
                {
                    vectorEv[i] = d[i];

                    //var io = i * order;
                    //matrixD[io + i] = d[i];

                    //if (e[i] > 0)
                    //{
                    //    matrixD[io + order + i] = e[i];
                    //    matrixD[(i + 1) * order + i] = e[i];
                    //}
                    //else if (e[i] < 0)
                    //{
                    //    matrixD[io - order + i] = e[i];
                    //}
                }
            }

            internal static void SymmetricTridiagonalize(double[] a, double[] d, double[] e, int order)
            {
                // Householder reduction to tridiagonal form.
                for (var i = order - 1; i > 0; i--)
                {
                    // Scale to avoid under/overflow.
                    var scale = 0.0;
                    var h = 0.0;

                    for (var k = 0; k < i; k++)
                    {
                        scale = scale + Math.Abs(d[k]);
                    }

                    if (scale == 0.0)
                    {
                        e[i] = d[i - 1];
                        for (var j = 0; j < i; j++)
                        {
                            d[j] = a[(j * order) + i - 1];
                            a[(j * order) + i] = 0.0;
                            a[(i * order) + j] = 0.0;
                        }
                    }
                    else
                    {
                        // Generate Householder vector.
                        for (var k = 0; k < i; k++)
                        {
                            d[k] /= scale;
                            h += d[k] * d[k];
                        }

                        var f = d[i - 1];
                        var g = Math.Sqrt(h);
                        if (f > 0)
                        {
                            g = -g;
                        }

                        e[i] = scale * g;
                        h = h - (f * g);
                        d[i - 1] = f - g;

                        for (var j = 0; j < i; j++)
                        {
                            e[j] = 0.0;
                        }

                        // Apply similarity transformation to remaining columns.
                        for (var j = 0; j < i; j++)
                        {
                            f = d[j];
                            a[(i * order) + j] = f;
                            g = e[j] + (a[(j * order) + j] * f);

                            for (var k = j + 1; k <= i - 1; k++)
                            {
                                g += a[(j * order) + k] * d[k];
                                e[k] += a[(j * order) + k] * f;
                            }

                            e[j] = g;
                        }

                        f = 0.0;

                        for (var j = 0; j < i; j++)
                        {
                            e[j] /= h;
                            f += e[j] * d[j];
                        }

                        var hh = f / (h + h);

                        for (var j = 0; j < i; j++)
                        {
                            e[j] -= hh * d[j];
                        }

                        for (var j = 0; j < i; j++)
                        {
                            f = d[j];
                            g = e[j];

                            for (var k = j; k <= i - 1; k++)
                            {
                                a[(j * order) + k] -= (f * e[k]) + (g * d[k]);
                            }

                            d[j] = a[(j * order) + i - 1];
                            a[(j * order) + i] = 0.0;
                        }
                    }

                    d[i] = h;
                }

                // Accumulate transformations.
                for (var i = 0; i < order - 1; i++)
                {
                    a[(i * order) + order - 1] = a[(i * order) + i];
                    a[(i * order) + i] = 1.0;
                    var h = d[i + 1];
                    if (h != 0.0)
                    {
                        for (var k = 0; k <= i; k++)
                        {
                            d[k] = a[((i + 1) * order) + k] / h;
                        }

                        for (var j = 0; j <= i; j++)
                        {
                            var g = 0.0;
                            for (var k = 0; k <= i; k++)
                            {
                                g += a[((i + 1) * order) + k] * a[(j * order) + k];
                            }

                            for (var k = 0; k <= i; k++)
                            {
                                a[(j * order) + k] -= g * d[k];
                            }
                        }
                    }

                    for (var k = 0; k <= i; k++)
                    {
                        a[((i + 1) * order) + k] = 0.0;
                    }
                }

                for (var j = 0; j < order; j++)
                {
                    d[j] = a[(j * order) + order - 1];
                    a[(j * order) + order - 1] = 0.0;
                }

                a[(order * order) - 1] = 1.0;
                e[0] = 0.0;
            }

            internal static void SymmetricDiagonalize(double[] a, double[] d, double[] e, int order)
            {
                const int maxiter = 1000;

                for (var i = 1; i < order; i++)
                {
                    e[i - 1] = e[i];
                }

                e[order - 1] = 0.0;

                var f = 0.0;
                var tst1 = 0.0;
                var eps = Math.Pow(2, -53); // DoubleWidth = 53
                for (var l = 0; l < order; l++)
                {
                    // Find small subdiagonal element
                    tst1 = Math.Max(tst1, Math.Abs(d[l]) + Math.Abs(e[l]));
                    var m = l;
                    while (m < order)
                    {
                        if (Math.Abs(e[m]) <= eps * tst1)
                        {
                            break;
                        }

                        m++;
                    }

                    // If m == l, d[l] is an eigenvalue,
                    // otherwise, iterate.
                    if (m > l)
                    {
                        var iter = 0;
                        do
                        {
                            iter = iter + 1; // (Could check iteration count here.)

                            // Compute implicit shift
                            var g = d[l];
                            var p = (d[l + 1] - g) / (2.0 * e[l]);
                            var r = Hypotenuse(p, 1.0);
                            if (p < 0)
                            {
                                r = -r;
                            }

                            d[l] = e[l] / (p + r);
                            d[l + 1] = e[l] * (p + r);

                            var dl1 = d[l + 1];
                            var h = g - d[l];
                            for (var i = l + 2; i < order; i++)
                            {
                                d[i] -= h;
                            }

                            f = f + h;

                            // Implicit QL transformation.
                            p = d[m];
                            var c = 1.0;
                            var c2 = c;
                            var c3 = c;
                            var el1 = e[l + 1];
                            var s = 0.0;
                            var s2 = 0.0;
                            for (var i = m - 1; i >= l; i--)
                            {
                                c3 = c2;
                                c2 = c;
                                s2 = s;
                                g = c * e[i];
                                h = c * p;
                                r = Hypotenuse(p, e[i]);
                                e[i + 1] = s * r;
                                s = e[i] / r;
                                c = p / r;
                                p = (c * d[i]) - (s * g);
                                d[i + 1] = h + (s * ((c * g) + (s * d[i])));

                                // Accumulate transformation.
                                for (var k = 0; k < order; k++)
                                {
                                    h = a[((i + 1) * order) + k];
                                    a[((i + 1) * order) + k] = (s * a[(i * order) + k]) + (c * h);
                                    a[(i * order) + k] = (c * a[(i * order) + k]) - (s * h);
                                }
                            }

                            p = (-s) * s2 * c3 * el1 * e[l] / dl1;
                            e[l] = s * p;
                            d[l] = c * p;

                            // Check for convergence. If too many iterations have been performed, 
                            // throw exception that Convergence Failed
                            if (iter >= maxiter)
                            {
                                throw new InvalidOperationException("Not converging.");
                            }
                        } while (Math.Abs(e[l]) > eps * tst1);
                    }

                    d[l] = d[l] + f;
                    e[l] = 0.0;
                }

                // Sort eigenvalues and corresponding vectors.
                for (var i = 0; i < order - 1; i++)
                {
                    var k = i;
                    var p = d[i];
                    for (var j = i + 1; j < order; j++)
                    {
                        if (d[j] < p)
                        {
                            k = j;
                            p = d[j];
                        }
                    }

                    if (k != i)
                    {
                        d[k] = d[i];
                        d[i] = p;
                        for (var j = 0; j < order; j++)
                        {
                            p = a[(i * order) + j];
                            a[(i * order) + j] = a[(k * order) + j];
                            a[(k * order) + j] = p;
                        }
                    }
                }
            }

            public static double Hypotenuse(double a, double b)
            {
                if (Math.Abs(a) > Math.Abs(b))
                {
                    double r = b / a;
                    return Math.Abs(a) * Math.Sqrt(1 + (r * r));
                }

                if (b != 0.0)
                {
                    // NOTE (ruegg): not "!b.AlmostZero()" to avoid convergence issues (e.g. in SVD algorithm)
                    double r = a / b;
                    return Math.Abs(b) * Math.Sqrt(1 + (r * r));
                }

                return 0d;
            }
        }
    }

    #endregion
}
