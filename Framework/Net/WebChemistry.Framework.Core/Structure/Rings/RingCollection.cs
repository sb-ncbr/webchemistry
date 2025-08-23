namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Math;
    using System.IO;
    
    /// <summary>
    /// Represents a collection of simple rings inside a structure.
    /// </summary>
    public class RingCollection : ReadOnlyCollection<Ring>
    {
        static readonly Ring[] emptyRings = new Ring[0];

        Dictionary<string, List<Ring>> byFingerprint;

        /// <summary>
        /// Rings with a particular fingerprint.
        /// </summary>
        /// <param name="fingerprint"></param>
        /// <returns></returns>
        public IEnumerable<Ring> GetRingsByFingerprint(string fingerprint)
        {
            var ret = byFingerprint.DefaultIfNotPresent(fingerprint);
            if (ret == null) return emptyRings;
            return ret;
        }

        /// <summary>
        /// Checks if the structure contains a ring with a given fingerprint.
        /// </summary>
        /// <param name="fingerprint"></param>
        /// <returns></returns>
        public bool ContainsFingerprint(string fingerprint)
        {
            return byFingerprint.ContainsKey(fingerprint);
        }

        /// <summary>
        /// Creates a ring collection.
        /// </summary>
        /// <param name="rings"></param>
        private RingCollection(IEnumerable<Ring> rings)
            : base(rings.AsList())
        {
            byFingerprint = this.GroupBy(r => r.Fingerprint, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        /////// <summary>
        /////// Write the rings to a file.
        /////// Format:
        /////// comma separated atom ids
        /////// one ring per line
        /////// </summary>
        /////// <param name="writer"></param>
        ////public void Write(TextWriter writer)
        ////{
        ////    foreach (var ring in this)
        ////    {
        ////        writer.WriteLine(string.Join(" ", ring.Atoms.Select(a => a.Id.ToString())));
        ////    }
        ////}

        /////// <summary>
        /////// Reads rings from a file
        /////// </summary>
        /////// <param name="structure"></param>
        /////// <param name="reader"></param>
        /////// <returns></returns>
        ////public static RingCollection Read(IStructure structure, TextReader reader)
        ////{
        ////    char[] comma = new char[] { ',' };
        ////    var atoms = structure.Atoms;
        ////    List<Ring> rings = new List<Ring>();
        ////    List<IAtom> ring = new List<IAtom>(10);
        ////    while (true)
        ////    {
        ////        var l = reader.ReadLine();
        ////        if (string.IsNullOrEmpty(l)) break;

        ////        ring.Clear();

        ////        var start = 0;
        ////        int space;

        ////        while ((space = l.IndexOf(' ', start)) != -1)
        ////        {
        ////            ring.Add(atoms.GetById(NumberParser.ParseIntFast(l, start, space - start)));
        ////            start = space + 1;
        ////        }
        ////        ring.Add(atoms.GetById(NumberParser.ParseIntFast(l, start, l.Length - start)));

        ////        var r = Ring.FromOrderedAtoms(ring.ToArray());
        ////        rings.Add(r);
        ////    }

        ////    return new RingCollection(rings);
        ////}

        class AtomInfo
        {
            /// <summary>
            /// The atom this node wraps.
            /// </summary>
            public IAtom Atom;

            /// <summary>
            /// Previous node in the rooted spanning tree.
            /// </summary>
            public AtomInfo TreePrevious;

            /// <summary>
            /// Previous node in the breadth-first priority search for tagged node.
            /// </summary>
            public AtomInfo SearchPrevious;

            /// <summary>
            /// Sorted list of "tree-previous" nodes. Built incrementally as the cycles are computed.
            /// Sorted asceding by Depth.
            /// </summary>
            public List<AtomInfo> CyclePrevious = new List<AtomInfo>();

            /// <summary>
            /// Depth in the spanning tree.
            /// </summary>
            public int Depth;

            /// <summary>
            /// Depth in the tagged node search.
            /// </summary>
            public int SearchDepth;

            /// <summary>
            /// Tag. Used to identify cycles.
            /// </summary>
            public int Tag;

            /// <summary>
            /// Linked list node - useful to find spanning tree of all components.
            /// </summary>
            public LinkedListNode<IAtom> ListNode;

            /// <summary>
            /// Insert a node to a correct position in the previous list.
            /// </summary>
            /// <param name="info"></param>
            public void InsertPrevious(AtomInfo info)
            {
                // TODO: Binary insertion...
                for (int i = CyclePrevious.Count - 1; i >= 0; i--)
                {
                    if (info.Depth >= CyclePrevious[i].Depth)
                    {
                        CyclePrevious.Insert(i + 1, info);
                        return;
                    }
                }
            }

            public override string ToString()
            {
                return string.Format("Id: {0}, Depth: {1}", Atom.Id, Depth, 0);
            }
        }

        struct AtomPredPair
        {
            public readonly IAtom Atom;
            public readonly IAtom Pred;

            public AtomPredPair(IAtom atom, IAtom pred)
            {
                this.Atom = atom;
                this.Pred = pred;
            }
        }

        struct AtomInfoPredPair
        {
            public readonly AtomInfo Atom;
            public readonly AtomInfo Pred;

            public AtomInfoPredPair(AtomInfo atom, AtomInfo pred)
            {
                this.Atom = atom;
                this.Pred = pred;
            }
        }

        /// <summary>
        /// Atom depth priority based search of a tagged atom.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="tag"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        static List<IAtom> FindTag(AtomInfo start, AtomInfo end, int tag, int depth)
        {
            const double maxDistSq = 36;

            Queue<AtomInfoPredPair> queue = new Queue<AtomInfoPredPair>();
            
            queue.Enqueue(new AtomInfoPredPair(start, null));
            start.SearchDepth = 0;

            HashSet<AtomInfo> visited = new HashSet<AtomInfo>();

            var startPos = start.Atom.Position;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var info = current.Atom;

                if (visited.Add(info))
                {
                    info.SearchPrevious = current.Pred;

                    if (info.Tag == tag)
                    {
                        // we've got a hit. traceback to start and return the atoms.
                        var atoms = new List<IAtom>();
                        var ca = info;
                        while (ca != null)
                        {
                            atoms.Add(ca.Atom);
                            ca = ca.SearchPrevious;
                        }
                        return atoms;
                    }

                    if (info.SearchDepth >= depth) continue;

                    for (int i = info.CyclePrevious.Count - 1; i >= 0; i--)
                    {
                        var other = info.CyclePrevious[i];
                        if (object.ReferenceEquals(info, start) && object.ReferenceEquals(other, end)) continue;

                        other.SearchDepth = info.SearchDepth + 1;

                        var distSq = other.Atom.Position.DistanceToSquared(startPos);
                        
                        if (!visited.Contains(other) && distSq < maxDistSq) queue.Enqueue(new AtomInfoPredPair(info.CyclePrevious[i], info));
                    }
                }
            }

            return null;
        }

        static void AddRing(AtomInfo shorter, AtomInfo deeper, int tag, HashSet<Ring> rings, IBondCollection bonds)
        {
            const int maxLength = 7;

            // tag the longer tree path
            var current = deeper;
            for (int i = 0; i < maxLength; i++)
            {
                if (current == null) break;

                current.Tag = tag;
                current = current.TreePrevious;
            }
            // Do a depth-based priority search for a tagged atom
            var ringAtoms = FindTag(shorter, deeper, tag, maxLength);

            // finish the ring
            if (ringAtoms != null)
            {
                var pivot = ringAtoms[0];
                current = deeper;
                while (current.Atom != pivot)
                {
                    ringAtoms.Add(current.Atom);
                    current = current.TreePrevious;
                }

                if (ringAtoms.Count > 2)
                {
                    var pivotResidue = ringAtoms[0].ResidueIdentifier();
                    bool ok = true;
                    for (int i = 1; i < ringAtoms.Count; i++)
                    {
                        if (ringAtoms[i].ResidueIdentifier() != pivotResidue)
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok)
                    {
                        var ring = Ring.Create(ringAtoms, bonds);
                        if (ring != null) rings.Add(ring);
                    }
                }
            }

            // add shortcuts
            shorter.InsertPrevious(deeper);
            deeper.InsertPrevious(shorter);
        }
        
        /// <summary>
        /// Find all rings of length ~8 and lower.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static RingCollection Create(IStructure structure)
        {
            if (structure.Atoms.Count == 0) return new RingCollection(new List<Ring>());

            Dictionary<int, AtomInfo> atomInfos = new Dictionary<int, AtomInfo>(structure.Atoms.Count);

            LinkedList<IAtom> linkedAtoms = new LinkedList<IAtom>();
            foreach (var atom in structure.Atoms)
            {
                var info = new AtomInfo
                {
                    Atom = atom,
                    ListNode = linkedAtoms.AddLast(atom)
                };
                atomInfos.Add(atom.Id, info);
            }

            var visitQueue = new Queue<AtomPredPair>();
            visitQueue.Enqueue(new AtomPredPair(structure.Atoms[0], null));
            var bonds = structure.Bonds;

            var edges = structure.Bonds.ToHashSet();

            // Use breath first search to create a rooted tree covering of the molecule
            // Also, remember which edges were not used
            bool done = false;
            while (!done)
            {
                var current = visitQueue.Dequeue();
                
                var atom = current.Atom;
                var pred = current.Pred;

                var atomInfo = atomInfos[atom.Id];

                if (atomInfo.ListNode != null)
                {
                    linkedAtoms.Remove(atomInfo.ListNode);
                    atomInfo.ListNode = null;

                    // set the info here
                    if (pred != null)
                    {
                        var predInfo = atomInfos[pred.Id];
                        atomInfo.TreePrevious = predInfo;
                        atomInfo.CyclePrevious.Add(predInfo);
                        atomInfo.Depth = predInfo.Depth + 1;
                        
                        edges.Remove(bonds[atom, pred]);
                    }

                    var bondList = bonds[atom] as IList<IBond>;
                    //foreach (var bond in bonds[atom])
                    for (int i = 0; i < bondList.Count; i++)
                    {
                        var bond = bondList[i];
                        var other = bond.B;
                        var otherInfo = atomInfos[other.Id];
                        if (otherInfo.ListNode != null)
                        {
                            visitQueue.Enqueue(new AtomPredPair(other, atom));
                        }
                        else
                        {
                            otherInfo.CyclePrevious.Add(atomInfo);
                        }
                    }
                }

                // make sure we traverse all components
                if (visitQueue.Count == 0)
                {
                    if (linkedAtoms.Count == 0) done = true;
                    else visitQueue.Enqueue(new AtomPredPair(linkedAtoms.First.Value, null));
                }
            }
            
            // Order the edges by the depth of their atoms.
            // this ensures that the shortest rings are found.
            var rings = new HashSet<Ring>();
            int tag = 1;
            foreach (var edge in edges
                .ToArray()
                .OrderBy(e => Math.Min(atomInfos[e.A.Id].Depth, atomInfos[e.B.Id].Depth))
                .ThenBy(e => Math.Max(atomInfos[e.A.Id].Depth, atomInfos[e.B.Id].Depth)))
            {
                var ai = atomInfos[edge.A.Id];
                var bi = atomInfos[edge.B.Id];

                if (ai.Depth < bi.Depth) AddRing(ai, bi, tag, rings, bonds);
                else AddRing(bi, ai, tag, rings, bonds);
                ++tag;
            }

            return new RingCollection(rings.OrderBy(r => r.Atoms[0].Id));
        }
    }
}
