namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// A ComputationAwaiter.
    /// </summary>
    /// <typeparam name="TResult">Result of the computation</typeparam>
    public class ComputationAwaiter<TResult>
    {
        Computation<TResult> computation;
        TResult result = default(TResult);
        Exception exception;

        /// <summary>
        /// Gets if the computation has completed ...
        /// </summary>
        public bool IsCompleted { get { return false; } }

        /// <summary>
        /// Starts the await ...
        /// </summary>
        public void OnCompleted(Action continuation)
        {
            computation
                .WhenError(e => this.exception = e)
                .WhenCompleted(r => this.result = r)
                .WhenFinished(() => continuation())
                .RunAsync();
        }

        /// <summary>
        /// Ends the await ...
        /// </summary>
        /// <returns></returns>
        public TResult GetResult()
        {
            if (this.computation.IsCancelled) throw new ComputationCancelledException();
            if (this.exception != null) throw this.exception;
            return this.result;
        }

        /// <summary>
        /// Creates the awaiter object.
        /// </summary>
        /// <param name="computation"></param>
        public ComputationAwaiter(Computation<TResult> computation)
        {
            if (computation == null) throw new ArgumentException("computation cannot be null");
            this.computation = computation;
        }
    }
}
