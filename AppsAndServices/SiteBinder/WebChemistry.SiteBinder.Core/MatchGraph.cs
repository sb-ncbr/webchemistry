// -----------------------------------------------------------------------
// <copyright file="MatchGraph.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    //public class MatchTransform

    public class TwoLevelGrouping<T>
    {
        static int Factorial(int n)
        {
            checked
            {
                int ret = 1;
                for (int i = 1; i <= n; i++) ret *= i;
                return ret;
            }
        }

        public class VertexGroup
        {
            public string Signature { get; set; }
            public VertexWrap<T>[] Group { get; set; }
            public int Width { get { return Group.Length; } }

            Permutations<VertexWrap<T>> _permutations;
            public Permutations<VertexWrap<T>> Permutations 
            { 
                get 
                {
                    if (_permutations == null) _permutations = Permutations<VertexWrap<T>>.Create(Group);
                    return _permutations;
                }
            }

            public string ToString(int offset)
            {
                return new string(' ', offset) + Signature + Width;
            }

            public void Flatten(int offset, VertexWrap<T>[] buffer)
            {
                for (int i = 0; i < Group.Length; i++)
                {
                    buffer[offset + i] = Group[i];
                }
            }

            public int CalculateSize()
            {
                checked
                {
                    return Factorial(Width);
                }
            }
        }

        public class BottomGroup
        {
            public string Signature { get; set; }
            public VertexGroup[] Groups { get; set; }
            public int Width { get; set; }
            
            public string ToString(int offset)
            {
                return new string(' ', offset) + Signature + "\n" + String.Join("\n", Groups.Select(g => g.ToString(offset + 2)));
            }

            public void Flatten(int offset, VertexWrap<T>[] buffer)
            {
                for (int i = 0; i < Groups.Length; i++)
                {
                    Groups[i].Flatten(offset, buffer);
                    offset += Groups[i].Width;
                }
            }

            public int CalculateSize()
            {
                return 1;
                //checked
                //{
                //    var ret = Groups.Sum(g =>
                //        {
                //            var s = g.CalculateSize();
                //            return s > 1 ? s : 0;
                //        });

                //    return ret > 0 ? ret : 1;
                //}
            }
        }

        public class TopGroup
        {
            public string Signature { get; set; }
            public BottomGroup[] Groups { get; set; }
            public int Width { get; set; }

            Permutations<BottomGroup> _permutations;
            public Permutations<BottomGroup> Permutations
            {
                get
                {
                    if (_permutations == null) _permutations = Permutations<BottomGroup>.Create(Groups);
                    return _permutations;
                }
            }

            public string ToString(int offset)
            {
                return new string(' ', offset) + Signature + "\n" + String.Join("\n", Groups.Select(g => g.ToString(offset + 2)));
            }

            public void Flatten(int offset, VertexWrap<T>[] buffer)
            {
                for (int i = 0; i < Groups.Length; i++)
                {
                    Groups[i].Flatten(offset, buffer);
                    offset += Groups[i].Width;
                }
            }

            public int CalculateSize()
            {
                checked
                {
                    var size = Factorial(Groups.Length) * Groups.Aggregate(1, (a, g) => a * g.CalculateSize());
                    return size;
                }
            }
        }

        int CalculateSize()
        {
            try
            {
                checked
                {
                    var size = Groups.Aggregate(1, (a, g) => a * g.CalculateSize());
                    return size;
                }
            }
            catch
            {
                return int.MaxValue;
            }
        }

        public VertexWrap<T>[] Flatten()
        {
            var ret = new VertexWrap<T>[Width];
            int offset = 0;
            for (int i = 0; i < Groups.Length; i++)
            {
                Groups[i].Flatten(offset, ret);
                offset += Groups[i].Width;
            }
            return ret;
        }

        public string ToLeveledString()
        {
            return string.Join("\n", Groups.Select(g => g.ToString(0)));
        }

        public TopGroup[] Groups { get; private set; }
        public int Width { get; private set; }
        public int Size { get; private set; }
        
        public static TwoLevelGrouping<T> Create(IEnumerable<VertexWrap<T>> vertices, Func<VertexWrap<T>, string> topLevel, Func<VertexWrap<T>, string> bottomLevel)
        {
            var ret = vertices
                .GroupBy(v => topLevel(v))
                .Select(g => new
                    {
                        Key = g.Key,
                        Groups = g
                            .GroupBy(v => bottomLevel(v))
                            .OrderBy(h => h.Key)
                            .Select(h => new VertexGroup { Group = h.ToArray(), Signature = h.Key } )
                            .ToArray()
                    })
                .Select(g => new BottomGroup
                    {
                        Groups = g.Groups,
                        Signature = string.Concat(g.Groups.Select(v => v.Signature + v.Width.ToString())),
                        Width = g.Groups.Sum(h => h.Width)
                    })
                .GroupBy(g => g.Signature)
                .Select(g => new TopGroup
                {
                    Signature = g.Key,
                    Groups = g.ToArray(),
                    Width = g.Sum(h => h.Width)
                })
                .OrderBy(g => g.Signature)
                .ToArray();                

            var groups = new TwoLevelGrouping<T> { Groups = ret, Width = ret.Sum(g => g.Width) };
            groups.Size = groups.CalculateSize();
            return groups;
        }

        public bool IsCompatible(TwoLevelGrouping<T> grouping)
        {
            if (Groups.Length != grouping.Groups.Length) return false;

            for (int i = 0; i < Groups.Length; i++)
            {
                if (Groups[i].Signature != grouping.Groups[i].Signature) return false;
                if (Groups[i].Groups.Length != grouping.Groups[i].Groups.Length) return false;
            }

            return true;
        }
    }

    public class VertexWrap<T>
    {
        public int Index { get; internal set; }
        public T Vertex { get; internal set; }
        public string Label { get; internal set; }
        public string GroupLabel { get; internal set; }
        public int RingScore { get; internal set; }
        public Vector3D Position { get; internal set; }
        public VertexWrap<T>[] Neighbors { get; internal set; }

        public int Color { get; set; }

        public VertexWrap<T> CloneWithoutNeighbors()
        {
            return new VertexWrap<T>
            {
                Index = Index,
                Vertex = Vertex,
                Label = Label,
                GroupLabel = GroupLabel,
                RingScore = RingScore,
                Position = new Vector3D(Position.X, Position.Y, Position.Z),
                Color = 0
            };
        }
        
        string _shortSignatureString;
        public string ShortSignatureString
        {
            get
            {
                if (_shortSignatureString == null) ComputeSignature();
                return _shortSignatureString;
            }
        }

        int? _shortSignatureSize;
        public int ShortSignatureSize
        {
            get
            {
                if (!_shortSignatureSize.HasValue) ComputeSignature();
                return _shortSignatureSize.Value;
            }
        }

        string _longSignatureString;
        public string LongSignatureString
        {
            get
            {
                if (_longSignatureString == null) ComputeSignature();
                return _longSignatureString;
            }
        }

        int? _longSignatureSize;
        public int LongSignatureSize
        {
            get
            {
                if (!_longSignatureSize.HasValue) ComputeSignature();
                return _longSignatureSize.Value;
            }
        }

        //public string[] NeighborLabels { get; private set; }
        //public string[] DistinctNeighborLabels { get; private set; }
        //Dictionary<string, int> NeighborCounts;

        //public static int CommonNeighborCount(VertexWrap<T> a, VertexWrap<T> b)
        //{
        //    int count = 0;

        //    int lenA = a.DistinctNeighborLabels.Length;

        //    for (int i = 0; i < lenA; i++)
        //    {
        //        string pivot = a.DistinctNeighborLabels[i];
        //        int cb;
        //        if (b.NeighborCounts.TryGetValue(pivot, out cb))
        //        {
        //            count += Math.Min(cb, a.NeighborCounts[pivot]);
        //        }
        //    }
        //    return count;
        //}
        
        List<string> TwoConstituentLabels;
        Dictionary<string, int> TwoConstituentCounts;
        public int TwoConstituentSize { get; private set; }
        void ComputeTwoConstituent()
        {
            if (TwoConstituentCounts != null) return;

            TwoConstituentLabels = new List<string>();
            TwoConstituentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            TwoConstituentSize = 0;
            
            this.Neighbors
                .SelectMany(n => n.Neighbors)
                .Concat(this.Neighbors)
                .Distinct(n => n.Index)
                .Where(n => n.Index != this.Index /*&& n.Neighbors.Length > 1*/)
                .GroupBy(n => n.Label, StringComparer.Ordinal)
                .ForEach(g =>
                {
                    TwoConstituentLabels.Add(g.Key);
                    var count = g.Count();
                    TwoConstituentCounts.Add(g.Key, count);
                    TwoConstituentSize += count;
                });
        }

        List<string> ThreeConstituentLabels;
        Dictionary<string, int> ThreeConstituentCounts;
        public int ThreeConstituentSize { get; private set; }
        void ComputeThreeConstituent()
        {
            if (ThreeConstituentCounts != null) return;

            ThreeConstituentLabels = new List<string>();
            ThreeConstituentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            ThreeConstituentSize = 0;

            var pivots = this.Neighbors
                .SelectMany(n => n.Neighbors)
                .Concat(this.Neighbors)
                .Distinct(n => n.Index)
                .ToArray();

            pivots
                .SelectMany(n => n.Neighbors)
                .Concat(pivots)
                .Distinct(n => n.Index)
                .Where(n => n.Index != this.Index /*&& n.Neighbors.Length > 1*/)
                .GroupBy(n => n.Label, StringComparer.Ordinal)
                .ForEach(g =>
                {
                    ThreeConstituentLabels.Add(g.Key);
                    var count = g.Count();
                    ThreeConstituentCounts.Add(g.Key, count);
                    ThreeConstituentSize += count;
                });
        }

        int[] ThreeConstituentMap;
        public void MakeThreeConstituentMap(string[] labels)
        {
            ComputeThreeConstituent();
            ThreeConstituentMap = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int val;
                if (ThreeConstituentCounts.TryGetValue(labels[i], out val))
                {
                    ThreeConstituentMap[i] = val;
                }
            }
        }
        
        public static int CommonTwoConstituentCount(VertexWrap<T> a, VertexWrap<T> b)
        {
            int count = 0;
            int lenA = a.TwoConstituentLabels.Count;

            for (int i = 0; i < lenA; i++)
            {
                string pivot = a.TwoConstituentLabels[i];
                int cb;
                if (b.TwoConstituentCounts.TryGetValue(pivot, out cb))
                {
                    count += Math.Min(cb, a.TwoConstituentCounts[pivot]);
                }
            }
            return count;
        }

        public static int CommonThreeConstituentCount(VertexWrap<T> a, VertexWrap<T> b)
        {
            var ma = a.ThreeConstituentMap;
            var mb = b.ThreeConstituentMap;
            int score = 0;
            for (int i = 0; i < ma.Length; i++)
            {
                score += Math.Min(ma[i], mb[i]);
            }
            return score;

            //int count = 0;
            //int lenA = a.ThreeConstituentLabels.Count;

            //for (int i = 0; i < lenA; i++)
            //{
            //    string pivot = a.ThreeConstituentLabels[i];
            //    int cb;
            //    if (b.ThreeConstituentCounts.TryGetValue(pivot, out cb))
            //    {
            //        count += Math.Min(cb, a.ThreeConstituentCounts[pivot]);
            //    }
            //}
            //return count;
        }

        class ConstituentCountComparer : IComparer<VertexWrap<T>>
        {
            Vector3D center;

            public int Compare(VertexWrap<T> x, VertexWrap<T> y)
            {
                var comp = y.ThreeConstituentSize.CompareTo(x.ThreeConstituentSize);
                if (comp == 0)
                {
                    Vector3D a = x.Position, b = y.Position;
                    var ca = a.DistanceToSquared(center);
                    var cb = b.DistanceToSquared(center);
                    var d = ca.CompareTo(cb);

                    if (d == 0)
                    {
                        if (a.X == b.X)
                        {
                            if (a.Y == b.Y)
                            {
                                return a.Z.CompareTo(b.Z);
                            }
                            return a.Y.CompareTo(b.Y);
                        }
                        return a.X.CompareTo(b.X);
                    }
                    return d;
                }
                return comp;
            }

            public ConstituentCountComparer(Vector3D center)
            {
                this.center = center;
            }
        }

        void ComputeSignature()
        {
            var sa = new List<string>(Neighbors.Length);
            var la = new List<string>(Neighbors.Length);

            for (int i = 0; i < Neighbors.Length; i++)
            {
                if (Neighbors[i].Neighbors.Length > 1)
                {
                    sa.Add(Neighbors[i].Label);
                }

                la.Add(Neighbors[i].Label);
            }

            _shortSignatureSize = sa.Count;
            _longSignatureSize = la.Count;
            //sa.Sort(StringComparer.Ordinal);
            sa.Sort(StringComparer.Ordinal);
            la.Sort(StringComparer.Ordinal);

            //NeighborLabels = sa;
            //DistinctNeighborLabels = sa.Distinct(StringComparer.Ordinal).ToArray();
            //Array.Sort(DistinctNeighborLabels, StringComparer.Ordinal);
            //NeighborCounts = sa.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

            _shortSignatureString = Label + " " + string.Concat(sa);
            _longSignatureString = Label + " " + string.Concat(la);

            ComputeTwoConstituent();
            ComputeThreeConstituent();

            Array.Sort(Neighbors, new ConstituentCountComparer(Position));
        }
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MatchGraph<T>
    {
        TwoLevelGrouping<T> _groupLevelGrouping, _labelLevelGrouping;

        public VertexWrap<T>[] Vertices { get; private set; }

        public TwoLevelGrouping<T> GroupLevelGrouping 
        {
            get
            {
                if (_groupLevelGrouping == null) _groupLevelGrouping = TwoLevelGrouping<T>.Create(Vertices, v => v.GroupLabel, v => v.Label);
                return _groupLevelGrouping;
            }
        }

        public TwoLevelGrouping<T> LabelLevelGrouping
        {
            get
            {
                if (_labelLevelGrouping == null) _labelLevelGrouping = TwoLevelGrouping<T>.Create(Vertices, v => v.Label, v => v.Label);
                return _labelLevelGrouping;
            }
        }

        bool? _isConnected = null;
        public bool IsConnected()
        {
            if (_isConnected.HasValue) return _isConnected.Value;

            Stack<VertexWrap<T>> visitStack = new Stack<VertexWrap<T>>(Vertices.Length);
            visitStack.Push(Vertices[0]);
            Vertices[0].Color = 1;
            int markedCount = 0;

            while (visitStack.Count > 0)
            {
                var current = visitStack.Pop();
                markedCount++;
                for (int i = 0; i < current.Neighbors.Length; i++)
                {
                    var n = current.Neighbors[i];
                    if (n.Color == 0)
                    {
                        n.Color = 1;
                        visitStack.Push(n);
                    }
                }
            }

            ResetColor(0);
            
            _isConnected = Vertices.Length == markedCount;
            return _isConnected.Value;
        }

        public string Token { get; private set; }

        public void ToCentroidCoordinates()
        {
            var center = new Vector3D();
            for (int i = 0; i < Vertices.Length; i++) center += Vertices[i].Position;
            center *= (-1.0 / Vertices.Length);

            for (int i = 0; i < Vertices.Length; i++)Vertices[i].Position += center;
        }

        class ReferenceEqualityComparer : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }

            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        }

        public static MatchGraph<T> Create(
            IEnumerable<T> elements, 
            Dictionary<T, int> ringScores,
            Func<T, IEnumerable<T>> neighbors, 
            Func<T, Vector3D> position, Func<T, string> label, 
            string token, 
            Func<T, string> groupLabel)
        {
            var es = new Dictionary<T, VertexWrap<T>>(ReferenceEqualityComparer.Instance);
            var re = new List<VertexWrap<T>>();
            int i = 0;

            foreach (var e in elements)
            {
                var w = new VertexWrap<T> { Vertex = e, Index = i, Position = position(e), Label = label(e), GroupLabel = groupLabel(e) };
                es.Add(e, w);
                re.Add(w);
                i++;
            }
            
            if (re.Count == 0) throw new InvalidOperationException(string.Format("Cannot create a graph of '{0}'. No valid vertices. This is probably caused by bad input.", token));

            for (i = 0; i < re.Count; i++)
            {
                var e = re[i];
                e.Neighbors = neighbors(e.Vertex).Where(n => es.ContainsKey(n)).Select(n => es[n]).ToArray();
            }

            if (MatchGraph.UseRingHeuristic)
            {
                foreach (var e in re)
                {
                    int score;
                    if (ringScores.TryGetValue(e.Vertex, out score))
                    {
                        e.RingScore = score;
                        //LogService.Default.Message("rs: {0} {1} = {2}", e.Label, e.Index, score);
                    }
                }
            }

            return new MatchGraph<T> { Vertices = re.ToArray(), Token = token };
        }

        public void ResetColor(int color = 0)
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Color = color;
            }
        }

        public MatchGraph<T> Clone()
        {
            var vs = Vertices.Select(v => v.CloneWithoutNeighbors()).ToArray();
            vs.ForEach(v => v.Neighbors = Vertices[v.Index].Neighbors.Select(n => vs[n.Index]).ToArray());
            return new MatchGraph<T>
            {
                Vertices = vs,
                Token = Token
            };
        }
    }

    public static class MatchGraph
    {
        /// <summary>
        /// I am going to hell for this :/
        /// </summary>
        public static bool UseRingHeuristic { get; set; }

        public static MatchGraph<IAtom> Create(IStructure structure, bool onlySelection = false, bool ignoreHydrogens = false)
        {
            IEnumerable<IAtom> atoms = structure.Atoms;
            if (onlySelection) atoms = atoms.Where(a => a.IsSelected);
            if (ignoreHydrogens) atoms = atoms.Where(a => a.ElementSymbol != ElementSymbols.H);

            var ringScores = new Dictionary<IAtom, int>(structure.Atoms.Count);
            
            foreach (var r in structure.Rings())
            {
                if (onlySelection && !r.Atoms.All(a => a.IsSelected)) continue;
                foreach (var a in r.Atoms)
                {
                    int score;
                    ringScores.TryGetValue(a, out score);
                    ringScores[a] = score + 1;
                }
            }

            return Create<IAtom>(atoms, ringScores, a => structure.Bonds[a].Select(b => b.B), a => a.Position, a => a.ElementSymbol.ToString(), a => a.PdbResidueSequenceNumber().ToString(), structure.Id);
        }

        public static MatchGraph<T> Create<T>(IEnumerable<T> elements, Dictionary<T, int> ringScores, Func<T, IEnumerable<T>> neighbors, Func<T, Vector3D> position, Func<T, string> label, Func<T, string> groupLabel, string token = "Empty")
        {
            return MatchGraph<T>.Create(elements, ringScores, neighbors, position, label, token, groupLabel);
        }

        static MatchGraph()
        {
            UseRingHeuristic = true;
        }
    }
}
