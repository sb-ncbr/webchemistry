// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Extension methods for computations.
    /// </summary>
    public static class ComputationEx
    {
        /// <summary>
        /// Wraps the function into RelayComputation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Computation<TResult> AsComputation<TResult>(this Func<TResult> function)
        {
            return Computation.Create(function);
        }

        /// <summary>
        /// Wraps the function into RelayComputation.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public static Computation<TResult> AsComputation<TResult>(this Func<ComputationProgress, TResult> function)
        {
            return Computation.Create(function);
        }

        /// <summary>
        /// Wraps the action into RelayComputation.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Computation AsComputation(this Action action)
        {
            return Computation.Create(action);
        }

        /// <summary>
        /// Wraps the action into RelayComputation.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Computation AsComputation(this Action<ComputationProgress> action)
        {
            return Computation.Create(action);
        }        
    }
}
