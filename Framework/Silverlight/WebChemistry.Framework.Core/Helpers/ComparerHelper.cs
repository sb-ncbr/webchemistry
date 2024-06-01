namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An utility class to quickly create comparers from lambdas
    /// </summary>
    public static class ComparerHelper
    {
        class EqualityComparer<T> : IEqualityComparer<T>
        {
            Func<T, T, bool> comp;
            Func<T, int> hash;

            public bool Equals(T x, T y)
            {
                return comp(x, y);
            }

            public int GetHashCode(T obj)
            {
                return hash(obj);
            }

            public EqualityComparer(Func<T, T, bool> comp, Func<T, int> hash)
            {
                this.comp = comp;
                this.hash = hash;
            }
        }

        class AnonymousComparer<T> : IComparer<T>
        {
            Func<T, T, int> comp;

            public int Compare(T x, T y)
            {
                return comp(x, y);
            }

            public AnonymousComparer(Func<T, T, int> comp)
            {
                this.comp = comp;
            }
        }
        
        /// <summary>
        /// Creates an equality comparer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="comp"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static IEqualityComparer<T> GetEqualityComparer<T>(Func<T, T, bool> comp, Func<T, int> hash)
        {
            return new EqualityComparer<T>(comp, hash);
        }
        
        /// <summary>
        /// Creates a comparer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static IComparer<T> GetComparer<T>(Func<T, T, int> comp)
        {
            return new AnonymousComparer<T>(comp);
        }
    }
}
