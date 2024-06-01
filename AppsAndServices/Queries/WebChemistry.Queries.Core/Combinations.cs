namespace WebChemistry.Queries.Core
{
    using System.Collections.Generic;
    using System.Linq;

    static class Combinations
    {
        /// <summary>
        /// The T[] here is MUTABLE!!!!! => always use a IEnumerable with for each, never call ToArray on this.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        static public IEnumerable<T[]> EnumerateCombinationsUnstable<T>(IList<T> set, int k)
        {
            int len = set.Count;

            if (k <= len)
            {
                T[] buffer = new T[k];

                int[] indices = Enumerable.Range(0, k).ToArray();
                int[] reversedIndices = Enumerable.Range(0, k).Reverse().ToArray();

                for (int i = 0; i < k; i++) buffer[i] = set[i];
                yield return buffer;

                while (true)
                {
                    int i;
                    for (i = k - 1; i >= 0; i--)
                    {
                        if (indices[i] != i + len - k) break;
                    }
                    if (i < 0) break;
                    indices[i] += 1;
                    //buffer = new T[k];
                    for (int j = i + 1; j < k; j++) indices[j] = indices[j - 1] + 1;
                    for (int j = 0; j < k; j++) buffer[j] = set[indices[j]];
                    yield return buffer;
                }
            }
        }

        /// <summary>
        /// Enumerates all k-combinations of the set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        static public IEnumerable<T[]> EnumerateCombinations<T>(IList<T> set, int k)
        {
            int len = set.Count;

            if (k <= len)
            {
                T[] buffer = new T[k];

                int[] indices = Enumerable.Range(0, k).ToArray();
                int[] reversedIndices = Enumerable.Range(0, k).Reverse().ToArray();

                for (int i = 0; i < k; i++) buffer[i] = set[i];
                yield return buffer;

                while (true)
                {
                    int i;
                    for (i = k - 1; i >= 0; i--)
                    {
                        if (indices[i] != i + len - k) break;
                    }
                    if (i < 0) break;
                    indices[i] += 1;
                    buffer = new T[k];
                    for (int j = i + 1; j < k; j++) indices[j] = indices[j - 1] + 1;
                    for (int j = 0; j < k; j++) buffer[j] = set[indices[j]];
                    yield return buffer;
                }
            }
        }
    }
}
