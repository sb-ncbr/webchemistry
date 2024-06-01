using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.Queries.Core.Queries
{
    /// <summary>
    /// Query cache container.
    /// </summary>
    public class MotiveCache
    {
        Dictionary<string, MotiveProximityTree> motiveProximityTrees = new Dictionary<string,MotiveProximityTree>(StringComparer.Ordinal);
        Dictionary<string, List<Motive>> motiveCache = new Dictionary<string,List<Motive>>(StringComparer.Ordinal);
        Dictionary<string, int> executionCounts = new Dictionary<string,int>(StringComparer.Ordinal);
        
        /// <summary>
        /// Increments the execution count by one.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal int UpdateExecutionCount(string name)
        {
            int count;
            if (executionCounts.TryGetValue(name, out count))
            {
                executionCounts[name] = count + 1;
                return count + 1;
            }

            executionCounts.Add(name, 1);
            return 1;
        }

        /// <summary>
        /// If the execution count was at least 2, the executing query has the responsility to create the cache entry.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal List<Motive> GetCachedResult(string name)
        {
            List<Motive> result;
            if (motiveCache.TryGetValue(name, out result)) return result;
            throw new ArgumentException(string.Format("The cache entry for '{0}' does not exist (but should).", name));
        }
        
        /// <summary>
        /// Cache the result.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        internal void CacheResult(string name, List<Motive> result)
        {
            motiveCache[name] = result;
        }

        /// <summary>
        /// Same thing but lazy.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="matches"></param>
        /// <returns></returns>
        internal MotiveProximityTree GetOrCreateProximityTree(QueryMotive q, Func<IEnumerable<Motive>> matches)
        {
            MotiveProximityTree tree;
            if (motiveProximityTrees.TryGetValue(q.ToString(), out tree)) return tree;
            tree = new MotiveProximityTree(matches());
            motiveProximityTrees.Add(q.ToString(), tree);
            return tree;
        }

        /// <summary>
        /// From seq.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="matches"></param>
        /// <returns></returns>
        internal MotiveProximityTree GetOrCreateProximityTree(QueryMotive q, IEnumerable<Motive> matches)
        {
            MotiveProximityTree tree;
            if (motiveProximityTrees.TryGetValue(q.ToString(), out tree)) return tree;
            tree = new MotiveProximityTree(matches);
            motiveProximityTrees.Add(q.ToString(), tree);
            return tree;
        }

        /// <summary>
        /// Resets the cache.
        /// </summary>
        internal void Reset()
        {
            motiveProximityTrees.Clear();
            motiveCache.Clear();
            executionCounts.Clear();
        }

        internal MotiveCache()
        {

        }
    }
}
