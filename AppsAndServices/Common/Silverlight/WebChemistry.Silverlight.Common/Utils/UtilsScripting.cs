using System;
using System.Linq;
using System.Collections.Generic;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Silverlight.Common.Services;
using System.Windows;
using WebChemistry.Framework.Core.Pdb;
using System.Text;

namespace WebChemistry.Silverlight.Common.Utils
{
    /// <summary>
    /// Misc. utilities.
    /// </summary>
    public class UtilsScripting
    {
        dynamic Session;
        
        public string ResidueHierarchialClustering(string descriptorName)
        {
            try
            {
                var progress = ComputationService.Default.Start();
                List<IStructure> source = new List<IStructure>();
                foreach (var s in Session.Structures) source.Add(s.Structure as IStructure);
                var result = Utils.ResidueHierarchialClustering.Compute(source);

                var format = "{0}_{1:" + new string('0', result.Count.ToString().Length) + "}";

                var names = new List<string>();
                foreach (var cluster in result)
                {
                    var name = string.Format(format, descriptorName, cluster.Key);
                    var labelName = name + "FP";
                    names.Add(name);
                    names.Add(labelName);

                    HashSet<IStructure> visited = new HashSet<IStructure>();
                    int index = 0;
                    foreach (var xs in cluster.Value)
                    {
                        foreach (var s in xs.Item2)
                        {
                            s.Descriptors()[name] = index;
                            s.Descriptors()[labelName] = xs.Item1;
                            visited.Add(s);
                        }
                        index++;
                    }

                    foreach (var s in source)
                    {
                        if (!visited.Contains(s))
                        {
                            s.Descriptors()[name] = int.MaxValue;
                            s.Descriptors()[labelName] = "n/a";
                        }
                    }
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (var name in names)
                    {
                        Session.Descriptors.AddSynchronously(name, string.Format("Current().Descriptor(\"{0}\")", name));
                    }
                });

                return string.Format("Clustering computed in {0}. Found {1} cluster classes.", ComputationService.Default.ElapsedString, result.Count);
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

        static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey value, out bool found)
        {
            TValue result;
            found = dict.TryGetValue(value, out result);
            return result;
        }

        public string ToClusterQuery(string id, double tolerance = 0.5)
        {
            bool found;
            dynamic wrap = TryGetValue(Session.StructureMap, id, out found);
            if (!found)
            {
                return string.Format("No structure with id '{0}' found.", id);
            }
            
            var s = wrap.Structure as IStructure;

            var residues = s.PdbResidues();
            var count = residues.Count;
            double maxDist = 0.0;

            for (int i = 0; i < count - 1; i++)
            {
                var a = residues[i];
                for (int j = i + 1; j < count; j++)
                {
                    var b = residues[j];
                    var d = PdbResidue.Distance(a, b);
                    if (d > maxDist) maxDist = d;
                }
            }

            maxDist += tolerance;
            var ret = new StringBuilder();

            ret.AppendFormat("Cluster({0}, {1})", 
                maxDist.ToStringInvariant("0.000"),
                string.Join(", ", residues.ResidueCounts.Keys.Select(r => string.Format("Residues(\"{0}\")", r))));

            var filter = string.Join(" & ", residues.ResidueCounts
                .Where(r => r.Value > 1)
                .Select(r => string.Format("(m.Count(Residues(\"{0}\")) >= {1})", r.Key, r.Value)));

            if (filter.Length > 0)
            {
                ret.AppendFormat(".Filter(lambda m: {0})", filter);
            }
            
            return ret.ToString();
        }

        public UtilsScripting(dynamic session)
        {
            this.Session = session;
        }
    }
}
