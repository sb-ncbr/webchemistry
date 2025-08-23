namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Math;
    using System.Threading.Tasks;
    using System.Threading;
    
    public static class MultipleMatching
    {
        public static Dictionary<string, OptimalTransformation> GetTransformation(this MultipleMatching<IAtom> matching, IEnumerable<IStructure> structures)
        {
            var byId = structures.ToDictionary(s => s.Id);
            var ret = new Dictionary<string, OptimalTransformation>();

            if (matching.PivotType == PivotType.SpecificStructure)
            {
                var pivot = byId[matching.Pivot.Token];
                foreach (var m in matching.MatchingsList)
                {
                    var other = byId[m.Other.Token];
                    var t = OptimalTransformation.Find(m.PivotOrdering.Take(m.Size).Select(a => a.Vertex).ToArray(), m.OtherOrdering.Take(m.Size).Select(a => a.Vertex).ToArray(), a => a.Position);
                    ret.Add(other.Id, t);
                }
            }
            else
            {
                var pivot = matching.FinalAverage;

                for (int i = 0; i < matching.AverageMatrix.Length; i++)
                {
                    var other = byId[matching.MatchingsList[i].Other.Token];
                    var row = matching.AverageMatrix[i];
                    var t = OptimalTransformation.Find(pivot, row.Select(a => a.Vertex.Position).ToArray(), v => v);
                    ret.Add(other.Id, t);
                }
            }

            return ret;
        }

        public static void Superimpose(this MultipleMatching<IAtom> matching, IEnumerable<IStructure> structures)
        {
            var byId = structures.ToDictionary(s => s.Id);

            if (matching.PivotType == PivotType.SpecificStructure)
            {
                var pivot = byId[matching.Pivot.Token];
                foreach (var m in matching.MatchingsList)
                {
                    var other = byId[m.Other.Token];
                    var t = OptimalTransformation.Find(m.PivotOrdering.Take(m.Size).Select(a => a.Vertex).ToArray(), m.OtherOrdering.Take(m.Size).Select(a => a.Vertex).ToArray(), a => a.Position);
                    t.Apply(other);
                }
            }
            else
            {
                var pivot = matching.FinalAverage;

                for (int i = 0; i < matching.AverageMatrix.Length; i++)
                {
                    var other = byId[matching.MatchingsList[i].Other.Token];
                    var row = matching.AverageMatrix[i];
                    var t = OptimalTransformation.Find(pivot, row.Select(a => a.Vertex.Position).ToArray(), v => v);
                    t.Apply(other);
                }
            }
        }
    }

    public class AverageGraph
    {
        public Vector3D[] Vertices;
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MultipleMatching<T>
    {
        public MatchingStatistics Statistics;
        public PivotType PivotType;
        public MatchMethod PairwiseType;
        public MatchGraph<T> Pivot;        
        public IList<PairwiseMatching<T>> MatchingsList;
        
        public VertexWrap<T>[][] AverageMatrix;
        public T[] AverageVertices;
        public Vector3D[] FinalAverage;

        public double[][] PairwiseMatrix;

        double[] rmsdToPivotVector;

        Dictionary<int, int>[] FindAverageLinks()
        {
            var links = MatchingsList.Select(m =>
                {
                    var dict = new Dictionary<int, int>(m.Size);
                    for (int i = 0; i < m.Size; i++)
                    {
                        dict.Add(m.PivotOrdering[i].Index, m.OtherOrdering[i].Index);
                    }
                    return dict;
                })
                .ToArray();

            return links;
        }

        int[] FindAverageIndices(Dictionary<int, int>[] links)
        {
            List<int> ret = new List<int>();

            foreach (var v in Pivot.Vertices)
            {
                if (links.All(l => l.ContainsKey(v.Index)))
                {
                    ret.Add(v.Index);
                }
            }

            return ret.OrderBy(i => i).ToArray();
        }

        VertexWrap<T>[][] ComputeAverageMatrix(Dictionary<int, int>[] averageLinks, int[] averageIndices)
        {
            VertexWrap<T>[][] ret = new VertexWrap<T>[MatchingsList.Count][];

            int width = averageIndices.Length;
            
            for (int i = 0; i < MatchingsList.Count; i++)
            {
                var other = MatchingsList[i].Other.Vertices;
                var links = averageLinks[i];
                ret[i] = new VertexWrap<T>[width];
                for (int j = 0; j < width; j++)
                {
                    ret[i][j] = other[links[averageIndices[j]]];
                }
            }

            return ret;
        }

        double Rmsd(Vector3D[] pivot, VertexWrap<T>[] other)
        {
            var rmsd = 0.0;

            for (int i = 0; i < pivot.Length; i++)
            {
                rmsd += pivot[i].DistanceToSquared(other[i].Position);
            }

            return Math.Sqrt(rmsd / pivot.Length);
        }

        double Rmsd(VertexWrap<T>[] pivot, VertexWrap<T>[] other)
        {
            var rmsd = 0.0;

            for (int i = 0; i < pivot.Length; i++)
            {
                rmsd += pivot[i].Position.DistanceToSquared(other[i].Position);
            }

            return Math.Sqrt(rmsd / pivot.Length);
        }

        double[] RmsdToAverage(Vector3D[] average)
        {
            return AverageMatrix.Select(r => Rmsd(average, r)).ToArray();
        }

        Vector3D[] ComputeAverage()
        {
            int h = AverageMatrix.Length, w = AverageMatrix[0].Length;
            Vector3D[] ret = new Vector3D[w];


            for (int i = 0; i < w; i++)
            {
                var v = new Vector3D();
                for (int j = 0; j < h; j++)
                {
                    v += AverageMatrix[j][i].Position;
                }
                ret[i] = (1.0 / h) * v;
            }

            return ret;
        }

        double SuperimposeToAverage(Vector3D[] average)
        {
            Vector3D[] buffer = new Vector3D[average.Length];

            var totalRmsd = 0.0;

            int index = 0;
            foreach (var row in AverageMatrix)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    buffer[i] = row[i].Position;
                }

                var si = OptimalTransformation.Find(average, buffer, v => v, row.Length);
                //si.TransformOntoPivot(row);
                si.Apply(row);
                totalRmsd += si.Rmsd;
                rmsdToPivotVector[index++] = si.Rmsd;
            }

            return totalRmsd / AverageMatrix.Length;
        }

        void AveragePivotIterate(ComputationProgress progress)
        {
            // superimpose to pivot!
            
            MatchingsList.ForEach(m => m.Superimpose());

            var avg = ComputeAverage();
            var currentRmsd = SuperimposeToAverage(avg);

            int iter = 0;
            const int maxIter = 10;

            while (iter < maxIter)
            {
                progress.ThrowIfCancellationRequested();

                // superimpose;
                avg = ComputeAverage();
                var newRmsd = SuperimposeToAverage(avg);

                if (newRmsd / currentRmsd > 0.95) break;

                currentRmsd = newRmsd;
                iter++;
            }

            var center = (-1.0 / avg.Length) * avg.Aggregate(new Vector3D(), (a, v) => a + v);
            for (int i = 0; i < avg.Length; i++) avg[i] = avg[i] + center;

            FinalAverage = avg;
        }
        
        void FindStatistics(int[][] clusters)
        {
            var r = MatchingsList.Select((m, i) => new { Token = m.Other.Token, Rmsd = m.Size > 0 ? rmsdToPivotVector[i] : 0.0 })
                .ToDictionary(m => m.Token, m => m.Rmsd);

            var nMatched = MatchingsList//.Select((m, i) => new { Token = m.Other.Token, Rmsd = m.Size > 0 ? rmsdToPivotVector[i] : 0.0 })
                .ToDictionary(m => m.Other.Token, m => m.Size);

            var clusterGroups = new Dictionary<string, int>();

            for (int i = 0; i < clusters.Length; i++)
            {
                foreach (var j in clusters[i])
                {
                    clusterGroups.Add(MatchingsList[j].Other.Token, i);
                }
            }

            Statistics = new MatchingStatistics(r, nMatched, clusterGroups, clusters.Length, Pivot.Token, PivotType);
        }

        public static MultipleMatching<T> Find(IList<MatchGraph<T>> graphs, 
            PivotType pivotType = PivotType.SpecificStructure, MatchMethod pairMethod = MatchMethod.Subgraph, 
            int pivotIndex = -1, bool pairwiseMatrix = false, int kClusters = 1, int maxParallelism = 8, ComputationProgress progress = null)
        {
            if (progress == null) progress = ComputationProgress.DummyInstance;

            if (pivotIndex < 0) pivotIndex = 0;

            // make labels
            var allLabels = graphs.SelectMany(g => g.Vertices.Select(v => v.Label)).Distinct(StringComparer.Ordinal).ToArray();
            {
                var _ls = allLabels;
                graphs.ForEach(g => g.Vertices.ForEach(v => v.MakeThreeConstituentMap(_ls)));
            }

            var pivot = graphs[pivotIndex];
            var template = pivot.Clone();

            ParallelOptions parOptions = new ParallelOptions { MaxDegreeOfParallelism = maxParallelism };
            ThreadLocal<MatchGraph<T>> threadLocalPivot = new ThreadLocal<MatchGraph<T>>(() =>
            {
                var p = pivot.Clone();
                var _ls = allLabels;
                p.Vertices.ForEach(v => v.MakeThreeConstituentMap(_ls));
                return p;
            });
            Func<MatchGraph<T>> threadPivot = () => threadLocalPivot.Value;

            var pivotMatchings = new PairwiseMatching<T>[graphs.Count];

            try
            {
                int done = 0;
                object sync = new object();
                progress.Update(isIndeterminate: false, currentProgress: 0, maxProgress: graphs.Count, statusText: "Superimposing to pivot...");
                Parallel.For(0, graphs.Count, parOptions, i =>
                {
                    lock (sync)
                    {
                        progress.ThrowIfCancellationRequested();
                    }

                    if (i == pivotIndex) return;
                    var localPivot = threadPivot();
                    // reset the positions for determinism ... probably a rounding error -- or more likely the EVD Cache error. Will see in the future.
                    localPivot.Vertices.ForEach(v => v.Position = template.Vertices[v.Index].Position);
                    pivotMatchings[i] = PairwiseMatching.Find(localPivot, graphs[i], pairMethod, progress);

                    lock (sync)
                    {
                        done++;
                        progress.ThrowIfCancellationRequested();
                        progress.Update(isIndeterminate: false, currentProgress: done, maxProgress: graphs.Count, statusText: "Superimposing to pivot...");
                    }
                });
            }
            catch (System.AggregateException e)
            {
                if (e.InnerExceptions.Count > 0) throw e.InnerExceptions[0];
                throw;
            }
            finally
            {
                threadLocalPivot.Dispose();
            }

            var matchings = new List<PairwiseMatching<T>> { PairwiseMatching<T>.Identity(pivot) };
            matchings.AddRange(pivotMatchings.Where(m => m != null));

            //for (int i = 0; i < graphs.Count; i++)
            //{
            //    progress.ThrowIfCancellationRequested();
            //    progress.Update(isIndeterminate: false, currentProgress: i, maxProgress: graphs.Count, statusText: "Superimposing to pivot...");

            //    if (i == pivotIndex) continue;
            //    matchings.Add(PairwiseMatching.Find(pivot, graphs[i], pairMethod, progress));
            //}

            var ret = new MultipleMatching<T>
            {
                Pivot = pivot,
                PivotType = pivotType,
                PairwiseType = pairMethod,
                MatchingsList = matchings
            };

            //Console.WriteLine(string.Join("\n", matchings.Where(m => m.Size < 10).Select(m => m.Pivot.Token + " " + m.Other.Token)));
            //bool averageNeeded = (pairwiseMatrix || pivotType == PivotType.Average);
            bool averageNeeded = true;

            if (averageNeeded)
            {
                progress.ThrowIfCancellationRequested();
                progress.UpdateIsIndeterminate(true);
                progress.UpdateStatus("Computing average structure...");
            }

            var links = averageNeeded ? ret.FindAverageLinks() : null;
            var averageIndices = averageNeeded ? ret.FindAverageIndices(links) : null;
            ret.AverageVertices = averageIndices.Select(i => pivot.Vertices[i].Vertex).ToArray();

            if (pivotType == PivotType.Average && averageNeeded && averageIndices.Length == 0)
            {
                throw new InvalidOperationException("The average structure cannot be constructed (0 'globally' matching atoms). This problem is usually caused by 'wrong' data such as a structure with a lot of missing atoms and " + 
                    "can often be solved by superimposing the "
                    + "structures using the 'Pivot' method and using 'Matched Atom Count' grouping in the 'Result' tab to remove the bad structures.");
            }

            ret.AverageMatrix = averageIndices.Length != 0 && averageNeeded ? ret.ComputeAverageMatrix(links, averageIndices) : null;

            if (pivotType == PivotType.Average)
            {                
                ret.rmsdToPivotVector = new double[graphs.Count];
                ret.AveragePivotIterate(progress);
            }
            else
            {
                if (averageIndices.Length != 0)
                {
                    ret.rmsdToPivotVector = new double[graphs.Count];
                    ret.AveragePivotIterate(progress);
                }
                else ret.FinalAverage = new Vector3D[0];

                ret.rmsdToPivotVector = matchings.Select(m => m.Rmsd).ToArray();
            }

            int[][] clusters;

            int count = graphs.Count;
            if (pairwiseMatrix)
            {
                progress.ThrowIfCancellationRequested();
                progress.UpdateIsIndeterminate(false);
                progress.UpdateProgress(0, 100);
                progress.UpdateStatus("Computing pairwise matrix...");

                ret.PairwiseMatrix = new double[count][];
                for (int i = 0; i < count; i++) ret.PairwiseMatrix[i] = new double[count];

                var totalPairwiseSize = (count * (count - 1)) / 2;
                double pairwiseProgress = 0;
                int currentPairwiseProgress = 0;

                for (int i = 0; i < count - 1; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        if (j % 100 == 0) progress.ThrowIfCancellationRequested();

                        var rmsd = OptimalTransformation.FindRmsd(ret.AverageMatrix[i], ret.AverageMatrix[j], v => v.Position);
                        pairwiseProgress += 1;
                        ret.PairwiseMatrix[i][j] = rmsd;
                        ret.PairwiseMatrix[j][i] = rmsd;

                        var newPairwiseProgress = (int)(100 * pairwiseProgress / totalPairwiseSize);
                        if (newPairwiseProgress != currentPairwiseProgress)
                        {
                            currentPairwiseProgress = newPairwiseProgress;
                            progress.UpdateProgress(currentPairwiseProgress);
                        }
                    }

                    progress.ThrowIfCancellationRequested();
                }

                if (kClusters > 1)
                {
                    progress.ThrowIfCancellationRequested();
                    progress.UpdateIsIndeterminate(true);
                    progress.UpdateStatus("Computing k-Means clustering...");
                    clusters = KMeans.Compute(kClusters, ret.PairwiseMatrix, progress);
                }
                else clusters = new int[][] { Enumerable.Range(0, count).ToArray() };
            }
            else clusters = new int[][] { Enumerable.Range(0, count).ToArray() };

            progress.ThrowIfCancellationRequested();
            progress.UpdateIsIndeterminate(true);
            progress.UpdateStatus("Computing statistics...");
            ret.FindStatistics(clusters);

            return ret;
        }
    }
}
