namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Helpers for lazy type.
    /// </summary>
    public class Lazy
    {
        /// <summary>
        /// Creates an instance of a Lazy value type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="creator"></param>
        /// <param name="isThreadSafe"></param>
        /// <returns></returns>
        public static Lazy<T> Create<T>(Func<T> creator, bool isThreadSafe = true)
        {
            return new Lazy<T>(creator, isThreadSafe);
        }
    }
}
