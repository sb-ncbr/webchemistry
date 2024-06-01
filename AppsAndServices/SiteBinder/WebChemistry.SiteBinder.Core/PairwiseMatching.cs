// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MatchState<T>
    {
        public int Level;
        public MatchGraph<T> Pivot, Other;
        public VertexWrap<T>[] CurrentMatchingPivot, CurrentMatchingOther;
        public int CurrentMatchedCount;
        public int MaxMatchedCount;
        
        public void Reset()
        {
            CurrentMatchedCount = 0;
            Level = 0;
        }
        
        private MatchState()
        {
        }

        public MatchState(int atomCount)
        {
            CurrentMatchedCount = 0;
            MaxMatchedCount = atomCount;
            CurrentMatchingOther = new VertexWrap<T>[atomCount];
            CurrentMatchingPivot = new VertexWrap<T>[atomCount];
        }
    }

    public static class PairwiseMatching
    {
        public static void Superimpose(this PairwiseMatching<IAtom> m, IStructure pivot, IStructure other)
        {
            var si = OptimalTransformation.Find(
                        m.PivotOrdering.Take(m.Size).Select(a => pivot.Atoms.GetById(a.Vertex.Id)).ToArray(),
                        m.OtherOrdering.Take(m.Size).Select(a => other.Atoms.GetById(a.Vertex.Id)).ToArray(),
                        v => v.Position);

            si.Apply(other);
        }
        
        public static PairwiseMatching<T> Find<T>(MatchGraph<T> pivot, MatchGraph<T> other, MatchMethod method = MatchMethod.Subgraph, ComputationProgress progress = null)
        {
            if (method == MatchMethod.Subgraph) return PairwiseMatching<T>.FindSubgraph(pivot, other, progress);
            else return PairwiseMatching<T>.FindCombinatorial(pivot, other, progress);
        }
    }

    public class PairwiseMatching<T>
    {
        public int Size;
        public VertexWrap<T>[] PivotOrdering, OtherOrdering;
        
        //public double Score = double.MaxValue;
        public double Rmsd = double.MaxValue;
        
        double _TopologyScore = double.MinValue;
        double _RmsdScore = double.MaxValue;

        public MatchGraph<T> Pivot, Other;

        const long MaxTraverseCount = 5000000;

        class ComputationState
        {
            public long TraverseCount;
        }


        //double _Score = double.MaxValue;
        //const int TopologyScoreSize = 16;
        //int[] TopologyScore = new int[TopologyScoreSize];
        //int[] TempTopologyScore = new int[TopologyScoreSize];

        //void CalcTempTopologyScore(MatchState<T> state)
        //{
        //    for (int i = 0; i < TopologyScoreSize; i++) TempTopologyScore[i] = 0;

        //    for (int i = 0; i < state.CurrentMatchedCount; i++)
        //    {
        //        var index = Math.Max(0, TopologyScoreSize - VertexWrap<T>.CommonTwoConstituentCount(state.CurrentMatchingPivot[i], state.CurrentMatchingOther[i]));
        //        TempTopologyScore[index] = TempTopologyScore[index] + 1;
        //    }
        //}

        //int UpdateTopologyScore()
        //{
        //    bool isTempBetter = false;
        //    bool areSame = true;
        //    for (int i = 0; i < TopologyScoreSize; i++)
        //    {
        //        int temp = TempTopologyScore[i];
        //        int s = TopologyScore[i];
        //        if (temp != s) areSame = false;
        //        if (temp > s)
        //        {
        //            isTempBetter = true;
        //            break;
        //        }
        //    }

        //    if (areSame) return 0;

        //    if (isTempBetter)
        //    {
        //        for (int i = 0; i < TopologyScoreSize; i++) TopologyScore[i] = TempTopologyScore[i];
        //    }

        //    return isTempBetter ? 1 : -1;
        //}

        static bool IsFirstBetterPairings(PairwiseMatching<T> a, PairwiseMatching<T> b)
        {
            //bool isABetter = false;
            //bool areSame = true;
            //for (int i = 0; i < TopologyScoreSize; i++)
            //{
            //    int aS = a.TopologyScore[i];
            //    int bS = b.TopologyScore[i];
            //    if (aS != bS) areSame = false;
            //    if (aS > bS)
            //    {
            //        isABetter = true;
            //        break;
            //    }
            //}

            //if (areSame) return a._Score <= b._Score;

            //return isABetter;

            if (a._TopologyScore > b._TopologyScore) return true;
            if (a._TopologyScore == b._TopologyScore) return a._RmsdScore <= b._RmsdScore;
            return false;

            //return a._Score <= b._Score;

        }

        //public static double CalcScore(double rmsd, double matched, double size)
        //{
        //    var t = 1 + matched / size;
        //    return Math.Exp(- t * t * t) * rmsd;
        //}

        internal void Trim()
        {
            if (Size != PivotOrdering.Length)
            {
                Array.Resize(ref PivotOrdering, Size);
                Array.Resize(ref OtherOrdering, Size);
            }
        }

        public static double CalcTopologyScore(MatchState<T> state)
        {
            double score = 0;
            int ringAtoms = 0;
            int nonRingAtoms = 0;

            int count = state.CurrentMatchedCount;
            for (int i = 0; i < count; i++)
            {
                var a = state.CurrentMatchingPivot[i];
                var b = state.CurrentMatchingOther[i];
                if (a.RingScore == b.RingScore) ringAtoms += a.RingScore;
                else /*if (a.IsOnRing || b.IsOnRing)*/ nonRingAtoms += Math.Max(a.RingScore, b.RingScore);
                score += VertexWrap<T>.CommonThreeConstituentCount(a, b);
            }

            double rd = Math.Max(ringAtoms - nonRingAtoms, 1.0);
            //double ringScore = Math.Pow(2.0, ringAtoms - nonRingAtoms);
            double ringScore = rd * rd;
            return score * ringScore;
        }


        public void Update(double rmsd, MatchState<T> state)
        {

            var newScore = CalcTopologyScore(state);

            if (newScore > _TopologyScore)
            {
                _RmsdScore = rmsd;
                _TopologyScore = newScore;
                Size = state.CurrentMatchedCount;
                Rmsd = rmsd;
                for (int i = 0; i < Size; i++)
                {
                    PivotOrdering[i] = state.CurrentMatchingPivot[i];
                    OtherOrdering[i] = state.CurrentMatchingOther[i];
                }
            }
            else if (newScore == _TopologyScore && _RmsdScore > rmsd)
            {
                _RmsdScore = rmsd;
                Size = state.CurrentMatchedCount;
                Rmsd = rmsd;
                for (int i = 0; i < Size; i++)
                {
                    PivotOrdering[i] = state.CurrentMatchingPivot[i];
                    OtherOrdering[i] = state.CurrentMatchingOther[i];
                }
            }
        }

        public void Superimpose()
        {
            var si = OptimalTransformation.Find(
                        PivotOrdering.Take(Size).ToArray(),
                        OtherOrdering.Take(Size).ToArray(),
                        v => v.Position);
            si.Apply(Other);
        }
        
        public PairwiseMatching(MatchGraph<T> pivot, MatchGraph<T> other)
        {
            this.Pivot = pivot;
            this.Other = other;
            var size = Math.Min(pivot.Vertices.Length, other.Vertices.Length);

            PivotOrdering = new VertexWrap<T>[size];
            OtherOrdering = new VertexWrap<T>[size];
        }

        public PairwiseMatching<T> Swap()
        {
            return new PairwiseMatching<T>(Other, Pivot)
            {
                OtherOrdering = PivotOrdering.ToArray(),
                PivotOrdering = OtherOrdering.ToArray(),
                //_Score = _Score,
                Rmsd = Rmsd,
                Size = Size
            };
        }

        public static PairwiseMatching<T> Identity(MatchGraph<T> graph)
        {
            return new PairwiseMatching<T>(graph, graph)
            {
                Rmsd = 0.0,
                PivotOrdering = graph.Vertices.ToArray(),
                OtherOrdering = graph.Vertices.ToArray(),
                Size = graph.Vertices.Length
            };
        }



        public static PairwiseMatching<T> FindSubgraph(MatchGraph<T> pivot, MatchGraph<T> other, ComputationProgress progress = null)
        {
            if (progress == null) progress = ComputationProgress.DummyInstance;

            if (!pivot.IsConnected()) throw new InvalidOperationException("The graph must be connected in order to use subgraph matching (" + pivot.Token + ").");
            if (!other.IsConnected()) throw new InvalidOperationException("The graph must be connected in order to use subgraph matching (" + other.Token + ").");
            //Console.WriteLine("Maching {0} and {1}", pivot.Token, other.Token);

            if (pivot.Vertices.Length < 3 || other.Vertices.Length < 3)
            {
                if (pivot.Vertices.Length <= other.Vertices.Length)
                {
                    return FindDegenerateSubgraph(pivot, other);
                }
                else
                {
                    var m = FindDegenerateSubgraph(other, pivot);
                    var t = m.OtherOrdering;
                    var tg = m.Other;

                    m.OtherOrdering = m.PivotOrdering;
                    m.PivotOrdering = t;

                    m.Other = m.Pivot;
                    m.Pivot = tg;
                    return m;
                }
            }


            var m1 = Match(pivot, other, progress);
            var m2 = Match(other, pivot, progress);

            if (IsFirstBetterPairings(m1, m2)) return m1;
            else
            {
                var t = m2.OtherOrdering;
                var tg = m2.Other;

                m2.OtherOrdering = m2.PivotOrdering;
                m2.PivotOrdering = t;

                m2.Other = m2.Pivot;
                m2.Pivot = tg;
                return m2;
            }
        }

        public static PairwiseMatching<T> FindCombinatorial(MatchGraph<T> pivot, MatchGraph<T> other, ComputationProgress progress = null)
        {
            if (progress == null) progress = ComputationProgress.DummyInstance;

            return MatchCombinatorial(pivot, other, progress);
        }

        static PairwiseMatching<T> FindDegenerateSubgraph(MatchGraph<T> smaller, MatchGraph<T> bigger)
        {
            if (smaller.Vertices.Length == 1)
            {
                var bestMatch = new PairwiseMatching<T>(smaller, bigger);

                var label = smaller.Vertices[0].Label;
                var other = bigger.Vertices.FirstOrDefault(a => a.Label.Equals(label, StringComparison.Ordinal));

                if (other == null) 
                {
                    bestMatch.Trim();
                    return bestMatch;
                }

                return new PairwiseMatching<T>(smaller, bigger)
                {
                    PivotOrdering = smaller.Vertices,
                    OtherOrdering = new VertexWrap<T>[] { other },
                    Rmsd = 0,
                    Size = 1
                };
            }
            
            if (smaller.Vertices.Length == 2)
            {
                double minRmds = double.MaxValue;
                VertexWrap<T> x = null, y = null;

                var a = smaller.Vertices[0];
                var b = smaller.Vertices[1];

                foreach (var v in bigger.Vertices)
                {
                    if (!a.Label.Equals(v.Label, StringComparison.Ordinal)) continue;

                    var da = v.Position.DistanceToSquared(a.Position);

                    foreach (var w in v.Neighbors)
                    {
                        if (!b.Label.Equals(w.Label, StringComparison.Ordinal)) continue;

                        var db = w.Position.DistanceToSquared(b.Position);

                        var rmsd = Math.Sqrt(0.5 * (da + db));
                        if (rmsd < minRmds)
                        {
                            minRmds = rmsd;
                            x = v;
                            y = w;
                        }
                    }
                }

                if (x != null)
                {
                    return new PairwiseMatching<T>(smaller, bigger)
                    {
                        PivotOrdering = smaller.Vertices,
                        OtherOrdering = new VertexWrap<T>[] { x, y },
                        Rmsd = 0,
                        Size = 1
                    };
                }
            }

            var ret = new PairwiseMatching<T>(smaller, bigger);
            ret.Trim();
            return ret;
        }

        /// <summary>
        /// (size, usedShort, pivots)
        /// </summary>
        /// <param name="xs"></param>
        /// <returns></returns>
        static Tuple<int, bool, VertexWrap<T>[]> BuildSignatures(VertexWrap<T>[] xs)
        {
            var sig = xs.OrderByDescending(v => v.ShortSignatureSize).ThenBy(v => v.ShortSignatureString).ToArray();
            if (sig[0].ShortSignatureSize < 2)
            {
                sig = xs.OrderByDescending(v => v.LongSignatureSize).ThenBy(v => v.LongSignatureString).ToArray();
                if (sig[0].LongSignatureSize < 2) return Tuple.Create(0, false, new VertexWrap<T>[0]);
                return Tuple.Create(sig[0].LongSignatureSize, false, sig);
            }
            return Tuple.Create(sig[0].ShortSignatureSize, true, sig);
        }

        #region Subgraph Matching
        static PairwiseMatching<T> Match(MatchGraph<T> pivot, MatchGraph<T> other, ComputationProgress progress)
        {
            ComputationState compState = new ComputationState();

            var sigPivot = BuildSignatures(pivot.Vertices);
            var sigOther = BuildSignatures(other.Vertices);

            var bestMatch = new PairwiseMatching<T>(pivot, other);

            if (sigPivot.Item1 < 2 || sigOther.Item1 < 2)
            {
                bestMatch.Trim();
                return bestMatch;
            }

            var sSize = Math.Min(sigPivot.Item1, sigOther.Item1);
            //var size = Math.Min(pivot.Vertices.Length, other.Vertices.Length);

            var sPivot = sigPivot.Item3
                .TakeWhile(s => (sigPivot.Item2 ? s.ShortSignatureSize : s.LongSignatureSize) >= sSize)
                .OrderByDescending(s => s.ThreeConstituentSize)
                .ToArray();
            var sOther = sigOther.Item3
                //.TakeWhile(s => (sigOther.Item2 ? s.ShortSignatureSize : s.LongSignatureSize) >= sSize)
                .OrderByDescending(s => s.ThreeConstituentSize)
                //.Take(Math.Min(pivot.Vertices.Length - other.Vertices.Length + 6, 15))
                .ToArray();

            //if (other.Token == "ARW_model") sOther.ForEach(s => LogService.Default.Message("Size: {0}", s.ThreeConstituentSize));

            //LogService.Default.Message("confirm");
            var evdCache = new EvdCache();
            for (int i = 0; i < sPivot.Length; i++)
            {
                bool done = false;

                var vPivot = sPivot[i];
                for (int j = 0; j < sOther.Length; j++)
                {
                    progress.ThrowIfCancellationRequested();
                    var vOther = sOther[j];
                    Match(compState, vPivot, pivot, vOther, other, bestMatch, evdCache);

                    if (compState.TraverseCount > MaxTraverseCount)
                    {
                        done = true;
                        break;
                    }
                }
                
                if (done) break;
            }
            
            bestMatch.Trim();
            ////if (bestMatch.Size >= 2)
            ////{
            ////    LogService.Default.Message("BMS ({7} {8}): {0}, Pivot: {1}, {2}, {3}, {4}, {5}, {6} ", bestMatch._TopologyScore,
            ////        (bestMatch.PivotOrdering[0].Vertex as IAtom).Id, (bestMatch.OtherOrdering[0].Vertex as IAtom).Id,
            ////        (bestMatch.PivotOrdering[1].Vertex as IAtom).Id, (bestMatch.OtherOrdering[1].Vertex as IAtom).Id,
            ////        (bestMatch.PivotOrdering[2].Vertex as IAtom).Id, (bestMatch.OtherOrdering[2].Vertex as IAtom).Id,
            ////        pivot.Token, other.Token);
            ////}
            //LogService.Default.Message("Traverse {0}", TraverseCount);
            return bestMatch;
        }

        static void Match(ComputationState compState, VertexWrap<T> vPivot, MatchGraph<T> pivot, VertexWrap<T> vOther, MatchGraph<T> other, PairwiseMatching<T> bestMatch, EvdCache evdCache)
        {
            if (vPivot.LongSignatureSize < 2) return;

            var nPivot = vPivot.Neighbors;
            var nOther = vOther.Neighbors;


            if (!vPivot.Label.Equals(vOther.Label, StringComparison.Ordinal) || vPivot.RingScore != vOther.RingScore) return;

            //VertexWrap<T> pX = null, pY = null;

            ////// fix this??
            ////// other the neighbotrs by 3-constituent signature!!
            //int pXIndex;
            //for (pXIndex = 0; pXIndex < nPivot.Length; pXIndex++)
            //{
            //    var x = nPivot[pXIndex];

            //    bool ok = false;
            //    for (int k = 0; k < vOther.Neighbors.Length; k++)
            //    {
            //        if (vOther.Neighbors[k].Label.Equals(x.Label, StringComparison.Ordinal))
            //        {
            //            ok = true;
            //            break;
            //        }
            //    }

            //    if (!ok) continue;
            //    pX = x;
            //    pXIndex++;
            //    break;
            //}

            //for (; pXIndex < nPivot.Length; pXIndex++)
            //{
            //    var y = nPivot[pXIndex];

            //    bool ok = false;
            //    for (int k = 0; k < vOther.Neighbors.Length; k++)
            //    {
            //        if (vOther.Neighbors[k].Label.Equals(y.Label, StringComparison.Ordinal))
            //        {
            //            ok = true;
            //            break;
            //        }
            //    }

            //    if (!ok) continue;
            //    pY = y;
            //    break;
            //}

            //if (pX == null || pY == null) return;

            var size = Math.Min(pivot.Vertices.Length, other.Vertices.Length);
            MatchState<T> state = new MatchState<T>(size) { Pivot = pivot, Other = other };

            for (int iX = 0; iX < nPivot.Length - 1; iX++)
            {
                var pX = nPivot[iX];

                for (int iY = iX + 1; iY < nPivot.Length; iY++)
                {
                    var pY = nPivot[iY];

                    for (int i = 0; i < nOther.Length - 1; i++)
                    {
                        var x = nOther[i];

                        for (int j = i + 1; j < nOther.Length; j++)
                        {
                            var y = nOther[j];

                            if (x.RingScore != y.RingScore) continue;
                            if (compState.TraverseCount > MaxTraverseCount) return;
                            //if (x.LongSignatureString != y.LongSignatureString) continue;

                            if (x.Label.Equals(pX.Label, StringComparison.Ordinal) && y.Label.Equals(pY.Label, StringComparison.Ordinal))
                            {
                                var si = OptimalTransformation.FindFast(evdCache, new Vector3D[] { pX.Position, pY.Position, vPivot.Position }, new Vector3D[] { x.Position, y.Position, vOther.Position }, v => v);
                                si.Apply(other);
                                state.Reset();
                                state.CurrentMatchingPivot[0] = vPivot;
                                state.CurrentMatchingOther[0] = vOther;
                                state.CurrentMatchedCount = 1;
                                pivot.ResetColor(0);
                                other.ResetColor(0);
                                Traverse(compState, vPivot, vOther, state, int.MaxValue, evdCache);
                                var rmsd = OptimalTransformation.FindRmsdFast(evdCache,
                                    state.CurrentMatchingPivot.Take(state.CurrentMatchedCount).ToArray(),
                                    state.CurrentMatchingOther.Take(state.CurrentMatchedCount).ToArray(),
                                    v => v.Position);
                                // Console.WriteLine("Size {0}, Rmsd {1}, Score {2}", state.CurrentMatchedCount, si.Rmsd, PairwiseMatching<T>.CalcScore(si.Rmsd, state.CurrentMatchedCount, state.MaxMatchedCount));
                                bestMatch.Update(rmsd, state);
                            }

                            if (x.Label.Equals(pY.Label, StringComparison.Ordinal) && y.Label.Equals(pX.Label, StringComparison.Ordinal))
                            {
                                var si = OptimalTransformation.FindFast(evdCache, new Vector3D[] { pY.Position, pX.Position, vPivot.Position }, new Vector3D[] { x.Position, y.Position, vOther.Position }, v => v);
                                si.Apply(other);

                                state.Reset();
                                state.CurrentMatchingPivot[0] = vPivot;
                                state.CurrentMatchingOther[0] = vOther;
                                state.CurrentMatchedCount = 1;
                                pivot.ResetColor(0);
                                other.ResetColor(0);
                                Traverse(compState, vPivot, vOther, state, int.MaxValue, evdCache);
                                var rmsd = OptimalTransformation.FindRmsdFast(evdCache,
                                    state.CurrentMatchingPivot.Take(state.CurrentMatchedCount).ToArray(),
                                    state.CurrentMatchingOther.Take(state.CurrentMatchedCount).ToArray(),
                                    v => v.Position);
                                // Console.WriteLine("Size {0}, Rmsd {1}, Score {2}", state.CurrentMatchedCount, si.Rmsd, PairwiseMatching<T>.CalcScore(si.Rmsd, state.CurrentMatchedCount, state.MaxMatchedCount));
                                bestMatch.Update(rmsd, state);
                            }
                        }

                        ////if (bestMatch.Size == size)
                        ////{
                        ////    done = true;
                        ////    break;
                        ////}
                    }
                }
            }
        }

        static void Traverse(ComputationState compState, VertexWrap<T> vPivot, VertexWrap<T> vOther, MatchState<T> state, int depth, EvdCache evdCache)
        {
            compState.TraverseCount++;
            if (compState.TraverseCount > MaxTraverseCount)
            {
                return;
                //throw new InvalidOperationException(string.Format("Cannot match graphs '{0}' and '{1}'. The graphs are too complicated. Consider removing these structures from your data set.", 
                //    state.Pivot.Token, state.Other.Token));
            }
            ////if (vPivot.Label == "N" && vOther.Label == "N")
            ////{
            ////    var n1 = (vPivot.Vertex as IAtom).PdbName();
            ////    var n2 = (vOther.Vertex as IAtom).PdbName();
            ////    LogService.Default.Message("{0} : {1}", n1, n2);
            ////    if (n1 == n2)
            ////    {
            ////        LogService.Default.Message("HIT");
            ////    }
            ////}
            //if ((vPivot as VertexWrap<IAtom>).Vertex.PdbName() == "C4'"
            //            && (vOther as VertexWrap<IAtom>).Vertex.PdbName() == "C4'")
            //{
            //    int stop = 1;
            //}

            //if ((vPivot as VertexWrap<IAtom>).Vertex.Id == 43
            //            && (vOther as VertexWrap<IAtom>).Vertex.Id == 13059)
            //{
            //    int stop = 1;
            //    Console.Write("1");
            //}


            //if (vPivot.SignatureString == "C CCN")
            //{
            //    var b = 1;
            //}
           // Console.WriteLine("Visiting {0} and {1}", vPivot.Label + ":" + vPivot.SignatureString, vOther.Label + ":" + vOther.SignatureString);
            vPivot.Color = 1;
            vOther.Color = 1;
            
            int matchCount = state.CurrentMatchedCount;

            if (depth == 0) return;

            //bool sup4 = false;

            var topTopologyScore = CalcTopologyScore(state);
            for (int i = 0; i < vPivot.Neighbors.Length; i++)
            {
                var na = vPivot.Neighbors[i];
                if (na.Color > 0) continue;
                
                bool isNaEnding = na.Neighbors.Length == 1;

                VertexWrap<T> paired = null;

                //double minDistance = double.MaxValue;
                bool isNbEnding = false;
                int count2 = int.MinValue;
                int count3 = int.MinValue;
                double topologyScore = topTopologyScore;

                double? pairedScore = null;
                double pairedRmsd = 0.0;

                double minDist = double.MaxValue;

                ////if ((na as VertexWrap<IAtom>).Vertex.PdbName() == "O1" && vOther.Neighbors.Any(xx => (xx as VertexWrap<IAtom>).Vertex.PdbName() == "O5"))
                ////{
                ////    var stop = true;
                ////}

                ///
                /// Make a primary decision based on the 3-constituent. Only if more identical options are presented, use RMSD.
                /// 
                for (int j = 0; j < vOther.Neighbors.Length; j++)
                {
                    var nb = vOther.Neighbors[j];
                    if (nb.Color > 0 || !na.Label.Equals(nb.Label, StringComparison.Ordinal)/* || na.IsOnRing != nb.IsOnRing*/) continue;

                    if (matchCount == 1)
                    {
                        var dist = na.Position.DistanceToSquared(nb.Position);
                        if (dist < minDist)
                        {
                            paired = nb;
                            minDist = dist;
                            isNbEnding = nb.Neighbors.Length == 1;
                        }
                        continue;
                    }

                    var commonCount2 = VertexWrap<T>.CommonTwoConstituentCount(na, nb);
                    var commonCount3 = VertexWrap<T>.CommonThreeConstituentCount(na, nb);

                    if (commonCount3 > count3)
                    {
                        //minDistance = na.Position.DistanceToSquared(nb.Position);
                        count2 = commonCount2;
                        count3 = commonCount3;
                        isNbEnding = nb.Neighbors.Length == 1;
                        paired = nb;
                    }
                    else if (commonCount3 == count3)
                    {
                        if (commonCount2 > count2)
                        {
                           // minDistance = na.Position.DistanceToSquared(nb.Position);
                            count2 = commonCount2;
                            paired = nb;
                            isNbEnding = nb.Neighbors.Length == 1;
                        }
                        else if (commonCount2 == count2)
                        {
                            if (isNaEnding && isNbEnding)
                            {
                                // a current matches are both endings and new one isnt -- continue
                                if (nb.Neighbors.Length != 1)
                                {
                                    continue;
                                }
                                //else // the new one is ending as well -- decide based on distance.
                                //{
                                //    var dist = na.Position.DistanceToSquared(nb.Position);
                                //    if (dist < minDist)
                                //    {
                                //        paired = nb;
                                //        minDist = dist;
                                //    }
                                //    continue;
                                //}
                            }
                            //if (matchCount == 1)
                            //{
                            //    var dist = na.Position.DistanceToSquared(nb.Position);
                            //    if (dist < minDist)
                            //    {
                            //        paired = nb;
                            //        minDist = dist;
                            //    }
                            //    continue;
                            //}

                            var level = state.Level;

                            int currentMatchCount = state.CurrentMatchedCount;

                            state.CurrentMatchedCount = currentMatchCount + 1;
                            state.CurrentMatchingPivot[currentMatchCount] = na;
                            state.CurrentMatchingOther[currentMatchCount] = nb;
                            Traverse(compState, na, nb, state, 6, evdCache);
                            var scoreNew = CalcTopologyScore(state);
                            var transformNew = OptimalTransformation.FindFast(evdCache, state.CurrentMatchingPivot, state.CurrentMatchingOther, v => v.Position, state.CurrentMatchedCount);
                            state.Pivot.ResetColor(); state.Other.ResetColor();
                            state.CurrentMatchedCount = currentMatchCount;
                            state.Level = level;
                            for (int z = 0; z < currentMatchCount; z++) { state.CurrentMatchingOther[z].Color = 1; state.CurrentMatchingPivot[z].Color = 1; }

                            double scoreOld;
                            OptimalTransformation transformOld;

                            if (pairedScore.HasValue) scoreOld = pairedScore.Value;
                            else 
                            {
                                state.CurrentMatchedCount = currentMatchCount + 1;
                                state.CurrentMatchingOther[currentMatchCount] = paired;
                                Traverse(compState, na, paired, state, 6, evdCache);
                                transformOld = OptimalTransformation.FindFast(evdCache, state.CurrentMatchingPivot, state.CurrentMatchingOther, v => v.Position, state.CurrentMatchedCount);
                                pairedRmsd = transformOld.Rmsd;
                                scoreOld = CalcTopologyScore(state);
                                pairedScore = scoreOld;
                                state.Pivot.ResetColor(); state.Other.ResetColor();
                                state.CurrentMatchedCount = currentMatchCount;
                                state.Level = level;
                                for (int z = 0; z < currentMatchCount; z++) { state.CurrentMatchingOther[z].Color = 1; state.CurrentMatchingPivot[z].Color = 1; }
                            }

                            if (scoreNew > scoreOld)
                            {
                             //  minDistance = na.Position.DistanceToSquared(nb.Position);
                                paired = nb;
                                pairedScore = scoreNew;
                                pairedRmsd = transformNew.Rmsd;
                                isNbEnding = nb.Neighbors.Length == 1;
                            }
                            else if (scoreNew == scoreOld)
                            {
                                if (transformNew.Rmsd <= pairedRmsd)
                                {
                                    pairedRmsd = transformNew.Rmsd;
                                    paired = nb;
                                    isNbEnding = nb.Neighbors.Length == 1;
                                }

                                //var dist = na.Position.DistanceToSquared(nb.Position);
                                //if (dist < minDistance)
                                //{
                                //    paired = nb;
                                //    minDistance = dist;
                                //}
                            }
                        }


                        //var d = na.Position.DistanceToSquared(nb.Position);
                        //if (d < minScore)
                        //{
                        //    minScore = d;
                        //    paired = nb;
                        //}
                    }

                    //var d = na.Position.DistanceToSquared(nb.Position) / weight;
                    //if (d < minScore)
                    //{
                    //    minScore = d;
                    //    paired = nb;
                    //}
                }

                //if (minDist > 2.0 * state.PartialRmsd * state.PartialRmsd)
                //{
                //    // superimpose again
                //}

                if (paired != null)
                {
                    state.CurrentMatchingPivot[state.CurrentMatchedCount] = na;
                    state.CurrentMatchingOther[state.CurrentMatchedCount] = paired;
                    state.CurrentMatchedCount++;

                    //var si = OptimalTransformation.Find(state.CurrentMatchingPivot, state.CurrentMatchingOther, v => v.Position, state.CurrentMatchedCount);
                    //si.Apply(state.Other);

                    na.Color = 1;
                    paired.Color = 1;
                }

                //if (state.CurrentMatchedCount % 4 == 0)
                //{
                //    sup4 = true;

                //    var si = OptimalTransformation.Find(
                //             state.CurrentMatchingPivot,
                //             state.CurrentMatchingOther,
                //             v => v.Position,
                //             state.CurrentMatchedCount);
                //    si.Apply(state.Other);
                //}
                    
            }

            //if (matchCount < state.CurrentMatchedCount && state.Level % 9 == 8)
            //{
            //    var si = OptimalTransformation.Find(
            //                 state.CurrentMatchingPivot,
            //                 state.CurrentMatchingOther,
            //                 v => v.Position,
            //                 state.CurrentMatchedCount);
            //    si.Apply(state.Other);
            //}
            
            if (matchCount < state.CurrentMatchedCount) state.Level++;
            //if (state.Level % 3 == 1)
            //{
            //    var si = OptimalTransformation.Find(
            //             state.CurrentMatchingPivot,
            //             state.CurrentMatchingOther,
            //             v => v.Position,
            //             state.CurrentMatchedCount);
            //    si.Apply(state.Other);
            //}

            for (int i = matchCount; i < state.CurrentMatchedCount; i++) Traverse(compState, state.CurrentMatchingPivot[i], state.CurrentMatchingOther[i], state, depth - 1, evdCache);
        }

        #endregion

        #region Permutation matching

        class CombinatorialState
        {
            public EvdCache EvdCache;

            public bool ReportProgress;
            public ComputationProgress Progress;

            public int PermutationNumber;

            public MatchGraph<T> Pivot;
            public MatchGraph<T> Other;
            public VertexWrap<T>[] OtherOrdering, PivotOrdering;
            public TwoLevelGrouping<T> OtherGrouping, PivotGrouping;

            public double Rmsd = double.MaxValue;
            public VertexWrap<T>[] BestOtherOrdering;

            public void Update(double newRmsd)
            {
                if (newRmsd < Rmsd)
                {
                    Rmsd = newRmsd;
                    for (int i = 0; i < OtherOrdering.Length; i++)
                    {
                        BestOtherOrdering[i] = OtherOrdering[i];
                    }
                }
            }

            public CombinatorialState()
            {
                EvdCache = new EvdCache();
            }
        }

        static PairwiseMatching<T> MatchCombinatorial(MatchGraph<T> pivot, MatchGraph<T> other, ComputationProgress progress)
        {
            pivot.ToCentroidCoordinates();
            other.ToCentroidCoordinates();

            var state = new CombinatorialState
            {
                Progress = progress,
                Pivot = pivot,
                Other = other
            };

            if (pivot.GroupLevelGrouping.IsCompatible(other.GroupLevelGrouping)) 
            {
                state.PivotGrouping = pivot.GroupLevelGrouping;
                state.OtherGrouping = other.GroupLevelGrouping;
            }
            else if (pivot.LabelLevelGrouping.IsCompatible(other.LabelLevelGrouping))
            {
                state.PivotGrouping = pivot.LabelLevelGrouping;
                state.OtherGrouping = other.LabelLevelGrouping;
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot match graphs '{0}' and '{1}' using combinatorial method. The graphs have to contain compatible vertices.", pivot.Token, other.Token));
            }

            state.PivotOrdering = state.PivotGrouping.Flatten();
            state.OtherOrdering = new VertexWrap<T>[state.PivotOrdering.Length];
            state.BestOtherOrdering = new VertexWrap<T>[state.PivotOrdering.Length];

            if (state.OtherGrouping.Size > 1 << 15)
            {
                //state.ReportProgress = true;
                //progress.UpdateIsIndeterminate(false);
                if (state.OtherGrouping.Size > 1 << 20) progress.UpdateStatus(string.Format("Superimposing '{0}' and '{1}'... seems like you are gonne be here for a while (size = {2}).", pivot.Token, other.Token, state.OtherGrouping.Size));
                else progress.UpdateStatus(string.Format("Superimposing '{0}' and '{1}'...", pivot.Token, other.Token));
                //progress.UpdateProgress(0, state.OtherGrouping.Size);
            }

            VisitTopLevel(state, 0, 0);

            return new PairwiseMatching<T>(pivot, other)
            {
                Rmsd = state.Rmsd,
                Size = pivot.Vertices.Length,
                PivotOrdering = state.PivotOrdering,
                OtherOrdering = state.BestOtherOrdering
            };
        }

        static void VisitTopLevel(CombinatorialState state, int index, int offset)
        {
            var groups = state.OtherGrouping.Groups;
            if (index == groups.Length)
            {
                var rmsd = OptimalTransformation.FindRmsd(state.PivotOrdering, state.OtherOrdering, v => v.Position);
                state.Update(rmsd);

                state.PermutationNumber++;
                if (state.PermutationNumber % (1 << 10) == 0)
                {
                    state.Progress.ThrowIfCancellationRequested();
                    //state.Progress.UpdateProgress(state.PermutationNumber);
                }

                return;
            }

            groups[index].Permutations.Visit(g => VisitBottomLevel(state, 0, offset, g, () => VisitTopLevel(state, index + 1, offset + groups[index].Width)));
        }

        static void VisitBottomLevel(CombinatorialState state, int index, int offset, TwoLevelGrouping<T>.BottomGroup[] permutation, Action onDone)
        {
            int pos = offset;
            for (int i = 0; i < permutation.Length; i++)
            {
                var p = permutation[i];
                for (int j = 0; j < p.Groups.Length; j++)
                {
                    var vertexGroup = p.Groups[j];

                    if (vertexGroup.Width == 1) state.OtherOrdering[pos] = vertexGroup.Group[0];
                    else MatchVertexGroup(vertexGroup, state, pos);

                    pos += vertexGroup.Width;
                }
            }

            onDone();
            return;
        }

        static void MatchVertexGroup(TwoLevelGrouping<T>.VertexGroup group, CombinatorialState state, int offset)
        {
            var count = group.Width;
            var bestRmsd = double.MaxValue;
            int pCount = 0;

            if (group.Width > 7)
            {
                state.Progress.UpdateStatus(string.Format("Superimposing large group of {0} atoms ({1}!). This might take a while.", group.Group[0].Label, group.Width));
            }

            group.Permutations.Visit(p =>
                {
                    pCount++;
                    if (pCount % (1 << 10) == 0)
                    {
                        state.Progress.ThrowIfCancellationRequested();
                    }
                    var rmsd = OptimalTransformation.FindRmsdWithoutTranslating(state.EvdCache, state.PivotOrdering, offset, p, v => v.Position, count);
                    if (rmsd < bestRmsd)
                    {
                        bestRmsd = rmsd;
                        for (int i = 0; i < count; i++) state.OtherOrdering[offset + i] = p[i];
                    }
                });

            if (group.Width > 7) state.Progress.UpdateStatus(string.Format("Superimposing '{0}' and '{1}'...", state.Pivot.Token, state.Other.Token));
        }

        #endregion
    }
}

