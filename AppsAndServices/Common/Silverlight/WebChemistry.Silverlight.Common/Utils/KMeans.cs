namespace WebChemistry.Silverlight.Common.Utils
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using WebChemistry.Queries.Core.Queries;
    using WebChemistry.Queries.Core;
    using WebChemistry.Silverlight.Common.DataModel;

    internal class QueryKMeans
    {
        ExecutionContext context;
        LambdaQuery distanceFunction;

        double GetDistance(Motive a, Motive b, object[] argsBuffer)
        {
            argsBuffer[0] = a;
            argsBuffer[1] = b;
            return (double)distanceFunction.Execute(context, argsBuffer);
        }

        LinkedList<Motive>[] Split(IList<IStructure> items, int groupCount)
        {
            List<LinkedList<Motive>> allGroups = new List<LinkedList<Motive>>();

            int startIndex = 0;
            int groupLength = (int)Math.Round((double)items.Count / (double)groupCount, 0);

            var motives = items.Select(s => s.MotiveContext().StructureMotive).ToArray();

            while (startIndex < items.Count)
            {
                LinkedList<Motive> group = new LinkedList<Motive>();

                for (int i = 0; i < groupLength; i++)
                {
                    group.AddLast(motives[startIndex + i]);
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
        
        double GetClusterDistance(Motive pivot, LinkedList<Motive> group)
        {
            var argsBuffer = new object[2];
            double distance;
            if (group.Count == 1)
            {

                distance = GetDistance(pivot, group.First.Value, argsBuffer);
            }
            else
            {
                distance = 0.0;
                int count = 0;
                for (var item = group.First; item != null; item = item.Next)
                {
                    var j = item.Value;
                    if (!object.ReferenceEquals(pivot, j))
                    {
                        count++;
                        distance += GetDistance(pivot, j, argsBuffer);
                    }
                }
                distance = distance / count;
            }

            if (distance < 0.00001) distance = 0;
            return distance;
        }

        int GetNearestClusterIndex(Motive pivot, LinkedList<Motive>[] clusters)
        {
            double nearestDistance = double.MaxValue;
            int ret = 0;

            for (int i = 0; i < clusters.Length; i++)
            {
                var d = GetClusterDistance(pivot, clusters[i]);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    ret = i;
                }
            }

            return ret;
        }

        public IStructure[][] Compute(int k, IList<IStructure> structures, ExecutionContext context, Func<dynamic, dynamic, dynamic> dist, ComputationProgress progress)
        {
            LambdaQuery lambda;
            try
            {
                lambda = new QueryBuilderLambda2 { Lambda = dist }.ToMetaQuery().Compile() as LambdaQuery;
            }
            catch (Exception)
            {
                throw new ArgumentException("Could not compile the distance function.");
            }

            this.distanceFunction = lambda;
            this.context = context;

            var clusters = Split(structures.ToArray(), k);

            progress.Update(isIndeterminate: true, canCancel: true);

            int movements = 1;
            int iteration = 1;
            while (movements > 0)
            {
                progress.Update(statusText: string.Format("KMeans iteration {0}...", iteration++));

                movements = 0;

                int currentClusterIndex = 0;
                foreach (var cluster in clusters)
                {
                    var pivot = cluster.First;

                    while (pivot != null)
                    {
                        progress.ThrowIfCancellationRequested();
                        int nearestClusterIndex = GetNearestClusterIndex(pivot.Value, clusters);
                        var next = pivot.Next;
                        if (nearestClusterIndex != currentClusterIndex && cluster.Count > 1)
                        {
                            cluster.Remove(pivot);
                            clusters[nearestClusterIndex].AddLast(pivot);
                            movements++;
                        }
                        pivot = next;
                    }

                    currentClusterIndex++;
                }
            }

            return clusters.Select(c => c.Select(m => m.Context.Structure).ToArray()).OrderByDescending(c => c.Length).ToArray();
        }
    }

    internal class StructureKMeans
    {
        Func<IStructure, IStructure, double> GetDistance;


        LinkedList<IStructure>[] Split(IList<IStructure> items, int groupCount)
        {
            List<LinkedList<IStructure>> allGroups = new List<LinkedList<IStructure>>();

            int startIndex = 0;
            int groupLength = (int)Math.Round((double)items.Count / (double)groupCount, 0);
            
            while (startIndex < items.Count)
            {
                LinkedList<IStructure> group = new LinkedList<IStructure>();

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

        double GetClusterDistance(IStructure pivot, LinkedList<IStructure> group)
        {
            double distance;
            if (group.Count == 1)
            {

                distance = GetDistance(pivot, group.First.Value);
            }
            else
            {
                distance = 0.0;
                int count = 0;
                for (var item = group.First; item != null; item = item.Next)
                {
                    var j = item.Value;
                    if (!object.ReferenceEquals(pivot, j))
                    {
                        count++;
                        distance += GetDistance(pivot, j);
                    }
                }
                distance = distance / count;
            }

            if (distance < 0.00001) distance = 0;
            return distance;
        }

        int GetNearestClusterIndex(IStructure pivot, LinkedList<IStructure>[] clusters)
        {
            double nearestDistance = double.MaxValue;
            int ret = 0;

            for (int i = 0; i < clusters.Length; i++)
            {
                var d = GetClusterDistance(pivot, clusters[i]);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    ret = i;
                }
            }

            return ret;
        }

        public IStructure[][] Compute(int k, IList<IStructure> structures, ExecutionContext context, Func<IStructure, IStructure, double> dist, ComputationProgress progress)
        {
            this.GetDistance = dist;

            var clusters = Split(structures.ToArray(), k);

            progress.Update(isIndeterminate: true, canCancel: true);

            int movements = 1;
            int iteration = 1;
            while (movements > 0)
            {
                //if (iteration > 100) break;

                progress.Update(statusText: string.Format("KMeans iteration {0}...", iteration++));
                
                movements = 0;

                int currentClusterIndex = 0;
                foreach (var cluster in clusters)
                {
                    var pivot = cluster.First;

                    while (pivot != null)
                    {
                        progress.ThrowIfCancellationRequested();
                        int nearestClusterIndex = GetNearestClusterIndex(pivot.Value, clusters);
                        var next = pivot.Next;
                        if (nearestClusterIndex != currentClusterIndex && cluster.Count > 1)
                        {
                            cluster.Remove(pivot);
                            clusters[nearestClusterIndex].AddLast(pivot);
                            movements++;
                        }
                        pivot = next;
                    }

                    currentClusterIndex++;
                }
            }

            return clusters.Select(c => c.ToArray()).OrderByDescending(c => c.Length).ToArray();
        }
    }
}