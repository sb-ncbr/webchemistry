using System;
using System.Collections.Generic;
using System.Linq;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.MotiveAtlas.DataModel;

namespace WebChemistry.MotiveAtlas.Analyzer
{

    /// <summary>
    /// A helper class for counting frequencies.
    /// </summary>
    static class FrequencyCounter
    {
        static void Sum2A(Dictionary<string, int> a, Dictionary<string, int> b, int sign)
        {
            foreach (var e in b)
            {
                int count;
                bool contains = a.TryGetValue(e.Key, out count);
                count += sign * e.Value;
                if (count != 0) a[e.Key] = count;
                else if (contains) a.Remove(e.Key);
            }
        }

        static void Sum2AOver(Dictionary<string, int> a, Dictionary<string, int> b, string[] over)
        {
            for (int i = 0; i < over.Length; i++)
            {
                var key = over[i];
                int x, y;
                var contains = a.TryGetValue(key, out x);
                b.TryGetValue(key, out y);
                var val = x + y;
                if (val > 0) a[key] = x + y;
                else if (contains) a.Remove(key);
            }
        }

        /// <summary>
        /// Add a and b and store to a.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void AddIntoA(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            Sum2A(a, b, 1);
        }

        /// <summary>
        /// Subtract b from a. Store into a.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SubtractIntoA(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            Sum2A(a, b, -1);
        }

        /// <summary>
        /// Add two counters.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Dictionary<string, int> Add(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            Dictionary<string, int> ret = a.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
            Sum2A(ret, b, 1);
            return ret;
        }

        /// <summary>
        /// Subtract two counters.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Dictionary<string, int> Subtract(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            Dictionary<string, int> ret = a.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
            Sum2A(ret, b, -1);
            return ret;
        }

        /// <summary>
        /// Sum many counters.
        /// </summary>
        /// <param name="xs"></param>
        /// <returns></returns>
        public static Dictionary<string, int> Sum(IEnumerable<Dictionary<string, int>> xs)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var x in xs) Sum2A(ret, x, 1);
            return ret;
        }

        /// <summary>
        /// Sum over given names.
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="over"></param>
        /// <returns></returns>
        public static Dictionary<string, int> Sum(IEnumerable<Dictionary<string, int>> xs, string[] over)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var x in xs) Sum2AOver(ret, x, over);
            return ret;
        }
        
        public static Dictionary<string, int> Trim(Dictionary<string, int> counts, IList<string> over)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>(over.Count, StringComparer.Ordinal);
            foreach (var n in over)
            {
                int c;
                counts.TryGetValue(n, out c);
                if (c > 0)
                {
                    ret.Add(n, c);
                }
            }
            return ret;
        }

        /// <summary>
        /// Group the data using residue type.
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        public static Dictionary<string, int> ToResidueChargeTypeCounts(Dictionary<string, int> counts)
        {
            return counts
                .GroupBy(c => PdbResidue.GetChargeType(c.Key))
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(x => x.Value), StringComparer.Ordinal);
        }

        /// <summary>
        /// Convert the counts to percentages.
        /// Does not count waters.
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        public static FrequencyStats ToFreqStats(Dictionary<string, int> counts)
        {
            var total = counts.Sum(c => PdbResidue.IsWaterName(c.Key) ? 0 : c.Value);
            double td = total;

            return new FrequencyStats
            {
                TotalCount = total,
                Frequencies = counts.Where(c => !PdbResidue.IsWaterName(c.Key)).ToDictionary(c => c.Key, c => (double)c.Value / td)
            };
        }

        /// <summary>
        /// Freq counter over a set of residues.
        /// Bool does not count water residues.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="over"></param>
        /// <returns></returns>
        public static FrequencyStats ToFreqStats(Dictionary<string, int> counts, IList<string> over)
        {
            var total = counts.Sum(c => PdbResidue.IsWaterName(c.Key) ? 0 : c.Value);
            double td = total;

            Dictionary<string, double> ret = new Dictionary<string, double>(over.Count, StringComparer.Ordinal);
            foreach (var n in over)
            {
                int c;
                if (counts.TryGetValue(n, out c) && c > 0)
                {
                    ret.Add(n, (double)c / td);
                }
            }

            return new FrequencyStats
            {
                TotalCount = total,
                Frequencies = ret
            };
        }

        /// <summary>
        /// now it's getting complicated :)
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="over"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static FrequencyStats ToFreqStats(Dictionary<string, int> counts, IList<string> over, int total)
        {
            double td = total;

            Dictionary<string, double> ret = new Dictionary<string, double>(over.Count, StringComparer.Ordinal);
            foreach (var n in over)
            {
                int c;
                if (counts.TryGetValue(n, out c) && c > 0)
                {
                    ret.Add(n, (double)c / td);
                }
            }

            return new FrequencyStats
            {
                TotalCount = total,
                Frequencies = ret
            };
        }

        /// <summary>
        /// Including total.
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static FrequencyStats ToFreqStats(Dictionary<string, int> counts, int total)
        {
            double td = total;

            return new FrequencyStats
            {
                TotalCount = total,
                Frequencies = counts.Where(c => !PdbResidue.IsWaterName(c.Key)).ToDictionary(c => c.Key, c => (double)c.Value / td)
            };
        }
    }
}
