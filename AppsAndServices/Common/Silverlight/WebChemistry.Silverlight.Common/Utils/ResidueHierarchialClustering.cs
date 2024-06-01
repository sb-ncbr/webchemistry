namespace WebChemistry.Silverlight.Common.Utils
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// Computes hierarchial clustering based on the number of common residues in structures.
    /// </summary>
    public static class ResidueHierarchialClustering
    {
        /// <summary>
        /// Computes the clustering.
        /// 
        /// Output format:
        /// { MinimumNumberOfCommonResidues : ListOfClusters }
        /// </summary>
        /// <param name="structures"></param>
        /// <returns></returns>
        public static Dictionary<int, List<Tuple<string, List<IStructure>>>> Compute(IEnumerable<IStructure> structures)
        {
            var xs = structures.ToArray();

            // "Rename" residues to integers for faster comparison
            var residueArray = xs.SelectMany(x => x.PdbResidues().UniqueResidueNames)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                .Select(x => x.ToUpper())
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            // "Rename" residues to integers for faster comparison
            var residueIndex = residueArray
                .Select((x, i) => Tuple.Create(x, i))
                .ToDictionary(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);

            // Create initial clusters by grouping together structures that contain the same residues. 
            // Order the list in descending order by the number of residues they contain.
            var clusters = new LinkedList<Cluster>(xs
                .GroupBy(x => x.PdbResidues().CountedResidueString)
                .Select(x => Cluster.FromStructures(x.ToArray(), residueIndex))
                .OrderByDescending(x => x.Size));

            // While there are at least two clusters
            while (clusters.Count > 1)
            {
                LinkedListNode<Cluster> a = null, b = null;
                int maxD = int.MinValue;
                int namesCount = 0;

                // Find clusters with the largest intersection.
                for (var m = clusters.First; m != null; m = m.Next)
                {
                    var x = m.Value;
                    // If the current cluster size is smaller than some we've already found, we can stop the search 
                    // because the clusters are sorted in descending order.
                    if (x.Size <= maxD) break;
                    for (var n = m.Next; n != null; n = n.Next)
                    {
                        var y = m.Value;
                        // Same as previous comment.
                        if (y.Size <= maxD) break;
                        int nc;
                        var d = Cluster.IntersectionSize(x, y, out nc);
                        if (d > maxD)
                        {
                            a = m;
                            b = n;
                            maxD = d;
                            namesCount = nc;
                        }
                    }
                }

                // Intersect the clusters identified in the previous loop.
                var merged = Cluster.Intersection(a.Value, b.Value, namesCount);

                // Remove the old clusters.
                clusters.Remove(a);
                clusters.Remove(b);

                // Insert the the merged cluster into the correct position in the list (so it remains sorted)
                var pivot = clusters.First;
                while (pivot != null && merged.Size < pivot.Value.Size) pivot = pivot.Next;
                if (pivot == null) clusters.AddLast(merged);
                else clusters.AddBefore(pivot, merged);
            }

            // The root of the cluster tree.
            var root = clusters.First.Value;

            // Find minimum and maximum size of clusters.
            int maxSize = Cluster.Fold(root, c => c.Size, (c, l, r) => Math.Max(c.Size, Math.Max(l, r)));
            int minSize = Cluster.Fold(root, c => c.Size, (c, l, r) => Math.Min(c.Size, Math.Min(l, r)));

            var ret = new Dictionary<int, List<Tuple<string, List<IStructure>>>>();

            // Find clusters for each "minimum common residue count"
            for (int i = minSize; i <= maxSize; i++)
            {
                var cls = new List<Tuple<string, List<IStructure>>>();
                root.GetClusters(i, residueArray, cls);
                if (cls.Count > 0)
                {
                    ret.Add(i, cls.OrderByDescending(c => c.Item2.Count).ThenBy(c => c.Item1).ToList());
                }
            }

            return ret;
        }

        /// <summary>
        /// Represents cluster tree node/leaf.
        /// </summary>
        class Cluster
        {
            /// <summary>
            /// Number of elements in the cluster key == ResidueCounts.Sum(x => x.Value).
            /// </summary>
            public int Size;

            /// <summary>
            /// Unique residue names. Corresponds to ResidueCounts.Keys, but is faster to enumerate.
            /// </summary>
            public List<int> ResidueNames;

            /// <summary>
            /// Cluster "key" - represents a multiset of residues (name : numberOfOccurences)
            /// </summary>
            public Dictionary<int, int> ResidueCounts;

            /// <summary>
            /// Only used by "leaf" clusters.
            /// </summary>
            public IStructure[] Structures;

            /// <summary>
            /// Cluster tree node.
            /// </summary>
            public Cluster Left, Right;

            /// <summary>
            /// Creates a "left" from a set of structures.
            /// </summary>
            /// <param name="xs"></param>
            /// <param name="residueIndex">String keys "renamed" to integers -- faster comparison.</param>
            /// <returns></returns>
            public static Cluster FromStructures(IStructure[] xs, Dictionary<string, int> residueIndex)
            {
                var s = xs[0];
                var rs = s.PdbResidues();
                return new Cluster
                {
                    Size = rs.Count,
                    ResidueNames = rs.UniqueResidueNames.Select(r => residueIndex[r]).ToList(),
                    ResidueCounts = rs.ResidueCounts.ToDictionary(r => residueIndex[r.Key], r => r.Value),
                    Structures = xs
                };
            }

            /// <summary>
            /// Compute the intersection of two clusters by "intersecting" the ResidueCounts.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="namesCount">Used to optimize the operation.</param>
            /// <returns></returns>
            public static Cluster Intersection(Cluster a, Cluster b, int namesCount)
            {
                var names = a.ResidueNames;

                var retNames = new List<int>(namesCount);
                Dictionary<int, int> retCounts = new Dictionary<int, int>(namesCount);
                int size = 0;

                for (int i = 0; i < names.Count; i++)
                {
                    var n = names[i];
                    int cB;
                    if (b.ResidueCounts.TryGetValue(n, out cB))
                    {
                        var s = Math.Min(a.ResidueCounts[n], cB);
                        size += s;
                        retCounts.Add(n, s);
                        retNames.Add(n);
                    }
                }

                a.ResidueNames = b.ResidueNames = null;

                // Check if we need to store the "child" residue counts.
                if (a.Size == size) a.ResidueCounts = null;
                if (b.Size == size) b.ResidueCounts = null;

                return new Cluster
                {
                    Size = size,
                    ResidueNames = retNames,
                    ResidueCounts = retCounts,
                    Left = a,
                    Right = b
                };
            }

            /// <summary>
            /// Compute the size of ResidueCounts intersection.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="namesCount"></param>
            /// <returns></returns>
            public static int IntersectionSize(Cluster a, Cluster b, out int namesCount)
            {
                var names = a.ResidueNames;
                int size = 0;
                int nc = 0;
                var aRc = a.ResidueCounts;
                var bRc = b.ResidueCounts;
                var count = names.Count;
                for (int i = 0; i < count; i++)
                {
                    var n = names[i];
                    int cB;
                    if (bRc.TryGetValue(n, out cB))
                    {
                        size += Math.Min(aRc[n], cB);
                        nc++;
                    }
                }
                namesCount = nc;
                return size;
            }

            /// <summary>
            /// Tree fold.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="c"></param>
            /// <param name="onLeaf"></param>
            /// <param name="onNode"></param>
            /// <returns></returns>
            public static T Fold<T>(Cluster c, Func<Cluster, T> onLeaf, Func<Cluster, T, T, T> onNode)
            {
                if (c.Structures != null) return onLeaf(c);
                return onNode(c, Fold(c.Left, onLeaf, onNode), Fold(c.Right, onLeaf, onNode));
            }

            /// <summary>
            /// Helper fuction to perform an action tree leaves.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="onLeaf"></param>
            public static void VisitLeaves(Cluster c, Action<Cluster> onLeaf)
            {
                if (c.Structures != null) onLeaf(c);
                else
                {
                    VisitLeaves(c.Left, onLeaf);
                    VisitLeaves(c.Right, onLeaf);
                }
            }

            /// <summary>
            /// Collapses all structures from child leaves into a single list.
            /// </summary>
            /// <param name="residueArray">Used to rename integers back to strings.</param>
            /// <returns></returns>
            public Tuple<string, List<IStructure>> Collapse(string[] residueArray)
            {
                List<IStructure> ret = new List<IStructure>();
                VisitLeaves(this, x => ret.AddRange(x.Structures));

                var label = PdbResidueCollection.GetCountedShortAminoNamesString(ResidueCounts.ToDictionary(r => residueArray[r.Key], r => r.Value));

                return Tuple.Create(label, ret);
            }

            /// <summary>
            /// Get all clusters that have at least "threshold" residues in common.
            /// </summary>
            /// <param name="threshold"></param>
            /// <param name="residueArray"></param>
            /// <param name="clusters"></param>
            public void GetClusters(int threshold, string[] residueArray, List<Tuple<string, List<IStructure>>> clusters)
            {
                if (this.Size >= threshold)
                {
                    clusters.Add(Collapse(residueArray));
                }
                else if (Structures == null)
                {
                    Left.GetClusters(threshold, residueArray, clusters);
                    Right.GetClusters(threshold, residueArray, clusters);
                }
            }
        }      
    }
}
