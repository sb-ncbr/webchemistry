// -----------------------------------------------------------------------
// <copyright file="Statistics.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;

    public static class KMeans
    {
        public static LinkedList<T>[] Split<T>(List<T> items, int groupCount)
        {
            List<LinkedList<T>> allGroups = new List<LinkedList<T>>();

            int startIndex = 0;
            int groupLength = (int)Math.Round((double)items.Count / (double)groupCount, 0);

            while (startIndex < items.Count)
            {
                LinkedList<T> group = new LinkedList<T>();

                for (int i = 0; i < groupLength; i++)
                {
                    group.AddLast(items[startIndex + i]);
                }

                startIndex += groupLength;
                if (startIndex + groupLength > items.Count)
                {
                    groupLength = items.Count - startIndex;
                }

                allGroups.Add(group);
            }

            if (allGroups.Count > groupCount && allGroups.Count > 2)
            {
                var lastGroup = allGroups[allGroups.Count - 2];
                
                foreach (var item in allGroups.Last())
                {
                    lastGroup.AddLast(item);
                }
                allGroups.RemoveAt(allGroups.Count - 1);
            }

            return allGroups.ToArray();
        }

        //static double GetMinDistance(int i, LinkedList<int> group, double[][] distanceMatrix)
        //{
        //    if (group.Count == 1) return distanceMatrix[i][group.First.Value];

        //    var row = distanceMatrix[i];
        //    double minDistance = double.MaxValue;
        //    for (var item = group.First; item != null; item = item.Next)
        //    {
        //        var j = item.Value;
        //        if (i != j) minDistance = Math.Min(row[j], minDistance);
        //    }
        //    return minDistance;
        //}

        static double GetClusterDistance(int i, LinkedList<int> group, double[][] distanceMatrix)
        {
            double distance;
            if (group.Count == 1) 
            {
                distance = distanceMatrix[i][group.First.Value];
            }
            else 
            {
                var row = distanceMatrix[i];
                distance = 0.0;
                int count = 0;
                for (var item = group.First; item != null; item = item.Next)
                {
                    var j = item.Value;
                    if (i != j)
                    {
                        count++;
                        distance += row[j];
                    }
                }
                distance = distance / count;
            }

            if (distance < 0.00001) distance = 0;
            return distance;
        }

        static int GetNearestClusterIndex(int index, LinkedList<int>[] clusters, double[][] distanceMatrix)
        {
            double nearestDistance = double.MaxValue;
            int ret = 0;

            for (int i = 0; i < clusters.Length; i++)
            {
                var d = GetClusterDistance(index, clusters[i], distanceMatrix);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    ret = i;
                }
            }

            return ret;
        }

        public static int[][] Compute(int k, double[][] distanceMatrix, ComputationProgress progress)
        {
            var clusters = Split(Enumerable.Range(0, distanceMatrix.Length).ToList(), k);

            int movements = 1;
            while (movements > 0)
            {
                movements = 0;

                int currentClusterIndex = 0;
                foreach (var cluster in clusters)
                {
                    progress.ThrowIfCancellationRequested();

                    var index = cluster.First;

                    while (index != null)
                    {
                        int nearestClusterIndex = GetNearestClusterIndex(index.Value, clusters, distanceMatrix);
                        var next = index.Next;
                        if (nearestClusterIndex != currentClusterIndex && cluster.Count > 1)
                        {
                            cluster.Remove(index);
                            clusters[nearestClusterIndex].AddLast(index);
                            movements++;
                        }
                        index = next;
                    }

                    currentClusterIndex++;
                }
            }

            return clusters.Select(c => c.ToArray()).OrderByDescending(c => c.Length).ToArray();
        }
    }

    /// <summary>
    /// Stats ...
    /// </summary>
    public class MatchingStatistics
    {
        public static readonly int InvalidSigmaGroup = 1000;

        public Dictionary<string, double> RmsdToPivot { get; private set; }
        public Dictionary<string, int> MatchedCounts { get; private set; }
        public double AverageRmsd { get; private set; }
        public double Sigma { get; private set; }
        public Dictionary<string, int> SigmaGroups { get; private set; }
        public Dictionary<string, int> ClusterGroups { get; private set; }
        public int NumClusters { get; private set; }

        string pivotToken;
        PivotType pivotType;

        void Compute()
        {
            double[] relevantData;

            if (pivotType == PivotType.Average) relevantData = RmsdToPivot.Where(p => MatchedCounts[p.Key] > 0).Select(p => p.Value).ToArray();
            else relevantData = RmsdToPivot.Where(p => MatchedCounts[p.Key] > 0 && !p.Key.Equals(pivotToken, StringComparison.Ordinal)).Select(p => p.Value).ToArray();

            if (relevantData.Length == 0) relevantData = new double[1] { 0.0 };

            AverageRmsd = relevantData.Average();

            var std = relevantData.Sum(x => x * x) / relevantData.Length - AverageRmsd * AverageRmsd;
            if (std < 0) std = 0.0;
            std = Math.Sqrt(std);
            Sigma = std;

            SigmaGroups = new Dictionary<string, int>();

            foreach (var p in RmsdToPivot)
            {
                if (MatchedCounts[p.Key] > 0)
                {
                    //double diff = System.Math.Abs(p.Value - averageRmsd);
                    double diff = p.Value - AverageRmsd;

                    if (std < 0.0000001) { SigmaGroups[p.Key] = 0; continue; }

                    if (diff < std) { SigmaGroups[p.Key] = 0; }
                    else if (diff < 2 * std) { SigmaGroups[p.Key] = 1; }
                    else if (diff < 3 * std) { SigmaGroups[p.Key] = 2; }
                    else { SigmaGroups[p.Key] = 3; }
                }
                else
                {
                    SigmaGroups[p.Key] = InvalidSigmaGroup;
                }
            }
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Avg. Rmsd = {0:0.000}, Sigma = {1:0.000}", AverageRmsd, Sigma);
        }

        public MatchingStatistics(Dictionary<string, double> rmsdToPivot, Dictionary<string, int> matchedCounts, Dictionary<string, int> clusterGroups, int numClusters, string pivotToken, PivotType pivotType)
        {
            this.RmsdToPivot = rmsdToPivot;
            this.MatchedCounts = matchedCounts;
            this.pivotToken = pivotToken;
            this.pivotType = pivotType;
            this.ClusterGroups = clusterGroups;
            this.NumClusters = numClusters;
            Compute();
        }
    }
}
