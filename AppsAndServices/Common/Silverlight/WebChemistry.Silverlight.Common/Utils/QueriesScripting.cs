using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Silverlight.Common.Services;

namespace WebChemistry.Silverlight.Common.Utils
{
    public class QueriesScripting
    {
        public class ContextResult
        {
            public string StructureId { get; set; }
            public object Result { get; set; }
        }

        dynamic Session;
        
        IList<Motive> Find(dynamic query, IEnumerable<dynamic> sources)
        {
            var qm = ((Query)query) as QueryMotive;

            var src = sources == null ? new dynamic[0] : sources.ToArray();

            dynamic source = src.Length > 0 ? sources : Session.Structures;
            var context = Session.QueryExecutionContext as ExecutionContext;

            List<Motive> motives = new List<Motive>();

            foreach (var s in source)
            {
                context.CurrentContext = MotiveExtensions.MotiveContext(s.Structure as IStructure);
                context.CurrentMotive = context.CurrentContext.StructureMotive;
                motives.AddRange(qm.ExecuteDynamic(context));
            }

            return motives;
        }

        public object Execute(dynamic query)
        {
            return Execute(query, null);
        }

        public object Execute(dynamic query, IEnumerable<dynamic> sources)
        {
            try
            {
                var q = ((Query)query);

                if (q is QueryMotive)
                {
                    return Find(q, sources);
                }

                var src = sources == null ? new dynamic[0] : sources.ToArray();
                var context = Session.QueryExecutionContext as ExecutionContext;
                context.CurrentContext = null;

                if (src.Length == 0)
                {
                    return q.ExecuteObject(context);
                }

                foreach (var s in src)
                {
                    try
                    {
                        var x = s.Structure as IStructure;
                    }
                    catch
                    {
                        return "Invalid source.";
                    }
                }

                List<ContextResult> ret = new List<ContextResult>();
                foreach (var s in src)
                {
                    var x = s.Structure as IStructure;
                    context.CurrentContext = MotiveExtensions.MotiveContext(x);
                    context.CurrentMotive = context.CurrentContext.StructureMotive;
                    var result = q.ExecuteObject(context);
                    ret.Add(new ContextResult { StructureId = x.Id, Result = result });
                }
                return ret;
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        public string KMeans(string descriptorName, int k, Func<dynamic, dynamic, dynamic> dist)
        {
            try
            {
                var kmeans = new QueryKMeans();

                var progress = ComputationService.Default.Start();
                List<IStructure> source = new List<IStructure>();
                foreach (var s in Session.Structures) source.Add(s.Structure as IStructure);
                var context = Session.QueryExecutionContext as ExecutionContext;
                var result = kmeans.Compute(k, source, context, dist, progress);

                int index = 0;
                foreach (var xs in result)
                {
                    foreach (var s in xs)
                    {
                        s.Descriptors()[descriptorName] = index;
                    }
                    index++;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => Session.Descriptors.AddSynchronously(descriptorName, string.Format("Current().Descriptor(\"{0}\")", descriptorName)));

                return "Clustering computed.";
            }
            catch (ComputationCancelledException)
            {
                return "Cancelled.";
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
            finally
            {
                ComputationService.Default.End();
            }
        }

        public static double ResidueDistance(IStructure a, IStructure b)
        {
            var xs = a.PdbResidues().ResidueCounts;
            var ys = b.PdbResidues().ResidueCounts;
            int common = 0;

            foreach (var p in xs)
            {
                int count;
                if (ys.TryGetValue(p.Key, out count))
                {
                    common += Math.Min(p.Value, count);
                }
            }

            return 1.0 - ((double)common) / ((double)(a.PdbResidues().Count + b.PdbResidues().Count - common));
        }

        public string KMeansFast(string descriptorName, int k, Func<IStructure, IStructure, double> dist)
        {
            try
            {
                var kmeans = new StructureKMeans();

                var progress = ComputationService.Default.Start();
                List<IStructure> source = new List<IStructure>();
                foreach (var s in Session.Structures) source.Add(s.Structure as IStructure);
                var context = Session.QueryExecutionContext as ExecutionContext;
                var result = kmeans.Compute(k, source, context, dist, progress);

                int index = 0;
                foreach (var xs in result)
                {
                    foreach (var s in xs)
                    {
                        s.Descriptors()[descriptorName] = index;
                    }
                    index++;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => Session.Descriptors.AddSynchronously(descriptorName, string.Format("Current().Descriptor(\"{0}\")", descriptorName)));

                return "Clustering computed.";
            }
            catch (ComputationCancelledException)
            {
                return "Cancelled.";
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
            finally
            {
                ComputationService.Default.End();
            }
        }
        
        public QueriesScripting(dynamic session)
        {
            this.Session = session;
        }
    }
}
