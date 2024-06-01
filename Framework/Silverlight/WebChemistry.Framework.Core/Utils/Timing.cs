namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Provides a simple wrapper to time computations.
    /// </summary>
    public static class Timing
    {
        /// <summary>
        /// Returns time x function result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Tuple<TimeSpan, T> Get<T>(Func<T> function)
        {
            DateTime start = DateTime.Now;
            var ret = function();
            var time = DateTime.Now - start;
            return Tuple.Create(time, ret);
        }

        /// <summary>
        /// Gets a timing of an action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TimeSpan Get<T>(Action action)
        {
            DateTime start = DateTime.Now;
            action();
            return DateTime.Now - start;
        }
    }
}
