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
    /// Computation base to support "anonymous" observing and cancellation.
    /// </summary>
    public interface IComputationBase
    {
        /// <summary>
        /// Current computation's progress.
        /// </summary>
        ComputationProgress Progress { get; }

        /// <summary>
        /// Cancels the task. default(TResult) is returned.
        /// </summary>
        void Cancel();
    }


    /// <summary>
    /// Interface for computation encapsulation.
    /// </summary>
    /// <typeparam name="TResult">Result of the computation.</typeparam>
    /// <typeparam name="TProgress">Type of computation's progress report.</typeparam>
    public interface IComputation<TResult, TProgress> : IComputationBase
    {
        /// <summary>
        /// Creates a task representing a future result. This is pretty much equivalent to the TaskEx.Run function.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This is thrown if RunAsync is called more than once.</exception>
        /// <returns>Task representing a result.</returns>       
        void RunAsync(Action<TResult> onCompleted, Action onCancelled, Action<Exception> onError);

        /// <summary>
        /// Runs the computation synchronously.
        /// </summary>
        ///  <exception cref="System.InvalidOperationException">This is thrown if RunSynchronously is called more than once.</exception>
        /// <returns>Result of the computation</returns>
        TResult RunSynchronously();
    }
}
