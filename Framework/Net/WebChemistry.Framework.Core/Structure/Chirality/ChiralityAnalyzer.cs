namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Math;

    static class ChiralityAnalyzer
    {
        class AtomAnalysisState : IEquatable<AtomAnalysisState>
        {
            ///// <summary>
            ///// Number of visited atoms of two states that are common.
            ///// </summary>
            ///// <param name="a"></param>
            ///// <param name="b"></param>
            ///// <returns></returns>
            //public static int IntersectCount(AtomAnalysisState a, AtomAnalysisState b)
            //{
            //    var va = a.VisitedList;
            //    var vb = b.Visited;
            //    int total = 0;
            //    for (int i = 0; i < va.Count; i++)
            //    {
            //        if (vb.Contains(va[i])) total++;
            //    }

            //    return total;
            //}

            ///// <summary>
            ///// This assumes rings.
            ///// </summary>
            ///// <param name="a"></param>
            ///// <param name="b"></param>
            ///// <returns></returns>
            //public static bool AreRingsSame(AtomAnalysisState a, IAtom startA, AtomAnalysisState b, IAtom startB)
            //{
            //    if (!a.Prev.ContainsKey(startA) || !b.Prev.ContainsKey(startB)) return false;

            //    HashSet<IAtom> ringA = new HashSet<IAtom>();
            //    IAtom current = startA;
            //    while (current != a.Source)
            //    {
            //        ringA.Add(current);
            //        current = a.Prev[current];
            //    }

            //    List<IAtom> ringB = new List<IAtom>();
            //    current = startB;
            //    while (current != b.Source)
            //    {
            //        ringB.Add(current);
            //        current = b.Prev[current];
            //    }

            //    if (ringA.Count != ringB.Count) return false;

            //    for (int i = 0; i < ringB.Count; i++)
            //    {
            //        if (!ringA.Contains(ringB[i])) return false;
            //    }

            //    return true;
            //}

            IStructure Structure;

            // Helper for the counter vector.
            Dictionary<ElementSymbol, int> Indexer;

            // For each element type in the structure, Counter[Indexer[a.ElementSymbol]] = number of atoms in the current component.
            int[] Counter;

            // Source Atom
            IAtom Source;

            // Ring?
            public int ReturnedToSourceCount;
            public int ReturnedToSourceDepth;
            //public IAtom LastRingAtom;

            // Advancement Level
            public int Level;

            // Current component frontier in the breath first search.
            List<IAtom> Frontier, NewFrontier, VisitedList;

            // The set of visited/added in current step atoms in the current BFS.
            public HashSet<IAtom> Visited;
            HashSet<IAtom> Added;

            // Previous Relation
            Dictionary<IAtom, IAtom> Prev;

            void Visit(IAtom atom)
            {
                Counter[Indexer[atom.ElementSymbol]]++;
                Visited.Add(atom);
                VisitedList.Add(atom);
            }

            // Advance the state by one layer of atoms.
            public bool Advance()
            {
                if (Frontier.Count == 0) return false;

                Level++;
                Added.Clear();

                // visit the current frontier.
                for (int i = 0; i < Frontier.Count; i++)
                {
                    var a = Frontier[i];
                    if (Visited.Contains(a)) continue;
                    Visit(a);
                    Added.Add(a);
                    var bonds = Structure.Bonds[a];
                    for (int j = 0; j < bonds.Count; j++)
                    {
                        var b = bonds[j].B;

                        if (Level > 1 && Level < 7 && b == Source)
                        {
                            if (ReturnedToSourceCount == 0) ReturnedToSourceDepth = Level;
                            ReturnedToSourceCount++;
                        }

                        if (!Visited.Contains(b) && Added.Add(b))
                        {
                            Prev[b] = a;
                            NewFrontier.Add(b);
                        }
                    }
                }

                // pull the old swich a roo
                var temp = Frontier;
                Frontier = NewFrontier;
                NewFrontier = temp;
                NewFrontier.Clear();

                return true;
            }

            public AtomAnalysisState(Dictionary<ElementSymbol, int> indexer, IAtom first, IAtom source, IStructure structure)
            {
                this.Indexer = indexer;
                this.Frontier = new List<IAtom> { first };
                this.NewFrontier = new List<IAtom>();
                this.Counter = new int[indexer.Count];
                this.Source = source;
                this.Visited = new HashSet<IAtom>() { source };
                this.VisitedList = new List<IAtom>();
                this.Added = new HashSet<IAtom>();
                this.Structure = structure;
                this.Prev = new Dictionary<IAtom, IAtom>(12);
                this.Prev[first] = source;
            }

            public override string ToString()
            {
                return string.Format("[{0}][{1}][{2}]", Indexer.JoinBy(v => v.Key.ToString() + ":" + Counter[v.Value], ","), VisitedList.JoinBy(a => a.PdbName(), ","), Frontier.JoinBy(a => a.PdbName(), ","));
            }

            public override bool Equals(object obj)
            {
                var other = obj as AtomAnalysisState;
                if (other == null) return false;
                return Equals(other);
            }

            public override int GetHashCode()
            {
                int hash = 31;
                for (int i = 0; i < Counter.Length; i++)
                {
                    hash = 23 * hash + Counter[i];
                }
                return hash;
            }

            public bool Equals(AtomAnalysisState other)
            {
                for (int i = 0; i < Counter.Length; i++)
                {
                    if (this.Counter[i] != other.Counter[i]) return false;
                }
                return true;
            }
        }

        static bool IsPlanar(IAtom atom, IList<IBond> bonds)
        {
            if (bonds.Count >= 3)
            {
                double angle = 0;
                for (int i = 0; i < bonds.Count - 2; i++)
                {
                    for (int j = i + 1; j < bonds.Count - 1; j++)
                    {
                        for (int k = j + 1; k < bonds.Count; k++)
                        {
                            Vector3D a = bonds[i].B.Position, b = bonds[j].B.Position, c = bonds[k].B.Position;
                            var plane = Plane3D.FromPoints(a, b, c);
                            double localAngle = Math.Max(
                                plane.GetAngleInRadians(Line3D.Create(a, atom.Position)),
                                Math.Max(plane.GetAngleInRadians(Line3D.Create(b, atom.Position)), plane.GetAngleInRadians(Line3D.Create(c, atom.Position))));

                            angle = Math.Max(angle, localAngle);
                        }
                    }
                }

                if (angle < MathHelper.DegreesToRadians(5.0)) return true;
            }
            return false;
        }

        static bool IsChiral(IAtom a, IStructure s, Dictionary<IAtom, int> ringCounts, Dictionary<ElementSymbol, int> indexer)
        {
            var bonds = s.Bonds[a];
            var bondCount = bonds.Count;

            if (bondCount < 3) return false;

            if (bondCount == 3)
            {
                foreach (var b in bonds)
                {
                    if (b.Type != BondType.Single && b.Type != BondType.Metallic) return false;
                }

                if (IsPlanar(a, bonds)) return false;
            }

            var states = bonds.Select(b => new AtomAnalysisState(indexer, b.B, b.A, s)).ToList();

            HashSet<AtomAnalysisState> stateSet = new HashSet<AtomAnalysisState>();
            //bool wasDifferent = false;
            while (true)
            {
                stateSet.Clear();
                bool advanced = false;
                for (int i = 0; i < states.Count; i++)
                {
                    var state = states[i];
                    if (state.Advance()) advanced = true;
                    stateSet.Add(state);
                }

                if (stateSet.Count == bondCount)
                {
                    //wasDifferent = true;
                    if (bondCount == 3)
                    {
                        bool isOnRings = true;
                        //bool nonSameOrCOrH = false;
                        for (int i = 0; i < bondCount; i++)
                        {
                            //var be = bonds[i].B.ElementSymbol;
                            //if (be != ElementSymbols.C && be != ElementSymbols.H && be != a.ElementSymbol)
                            //{
                            //    nonSameOrCOrH = true;
                            //}
                            if (states[i].ReturnedToSourceCount < 2 || states[i].ReturnedToSourceDepth < 4)
                            {
                                isOnRings = false;
                            }
                        }

                        //if (a.ElementSymbol == ElementSymbols.N && nonSameOrCOrH)
                        //{
                        //    return ringCounts.ContainsKey(a);
                        //}
                        //else
                        {
                            if (isOnRings)
                            {
                                return true;

                                //bool isStable = true;
                                //for (int i = 0; i < bondCount - 1; i++)
                                //{
                                //    for (int j = i + 1; j < bondCount; j++)
                                //    {
                                //        //int ic = AtomAnalysisState.IntersectCount(states[i], states[j]);
                                //        //if (states[i].Visited.Count - ic < 4 || states[j].Visited.Count - ic < 4)
                                //        //{
                                //        //    isStable = false;
                                //        //}

                                //        if (AtomAnalysisState.AreRingsSame(states[i], bonds[j].B, states[j], bonds[i].B))
                                //        {
                                //            isStable = false;
                                //        }
                                //    }
                                //}
                                //return isStable;
                            }
                            else
                            {
                                for (int i = 0; i < bondCount; i++)
                                {
                                    if (states[i].Level > 6) return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                if (!advanced) return false;
            }
        }

        /// <summary>
        /// Find chiral atoms (atoms with at least 3 neighbors that have all outgoing components different).
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static IList<IAtom> GetChiralAtoms(IStructure structure)
        {
            // this is used to determine the component vector
            var indexer = structure.Atoms
                .GroupBy(a => a.ElementSymbol)
                .OrderBy(g => g.Key)
                .Select((g, i) => new { E = g.Key, I = i })
                .ToDictionary(e => e.E, e => e.I);

            var ringCounts = structure.Rings().SelectMany(r => r.Atoms).GroupBy(a => a).ToDictionary(g => g.Key, g => g.Count());

            return structure.Atoms.Where(a => IsChiral(a, structure, ringCounts, indexer)).ToList();
        }
    }
}
