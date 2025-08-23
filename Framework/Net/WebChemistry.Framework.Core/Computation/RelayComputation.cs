// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Represents a computation that invokes a single action.
    /// </summary>
    sealed class RelayComputation : Computation
    {
        Action<ComputationProgress> action;

        /// <summary>
        /// OnRun override. Calls the function.
        /// </summary>
        /// <returns></returns>
        protected override Unit OnRun()        
        {
            action(Progress);
            return new Unit();
        }

        /// <summary>
        /// ComputationProgress object is passed to the action.
        /// </summary>
        /// <param name="action">Action to be invoked.</param>
        public RelayComputation(Action<ComputationProgress> action)
        {
            this.action = action;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function">Action to be invoked.</param>
        public RelayComputation(Action function)
            : this(_ => function())
        {
            
        }
    }

    /// <summary>
    /// Represents a computation that calls a single function.
    /// </summary>
    /// <typeparam name="TResult">Result of the computation</typeparam>
    sealed class RelayComputation<TResult> : Computation<TResult>
    {
        Func<ComputationProgress, TResult> function;

        /// <summary>
        /// OnRun override. Calls the function.
        /// </summary>
        /// <returns></returns>
        protected override TResult OnRun()
        {
            return function(Progress);
        }

        /// <summary>
        /// ComputationProgress object is passed to the function.
        /// </summary>
        /// <param name="function">Function to be computed.</param>
        public RelayComputation(Func<ComputationProgress, TResult> function)
        {
            this.function = function;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function">Function to be computed.</param>
        public RelayComputation(Func<TResult> function)
            : this(_ => function())
        {
            
        }
    }
}
