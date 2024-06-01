// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{

    /// <summary>
    /// AsyncCTP Extensions
    /// </summary>
    public static class AsyncEx
    {
        /// <summary>
        /// Allows the computation object to be awaited.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="computation"></param>
        /// <returns></returns>
        public static ComputationAwaiter<TResult> GetAwaiter<TResult>(this Computation<TResult> computation)
        {
            return new ComputationAwaiter<TResult>(computation);
        }
    }
}
