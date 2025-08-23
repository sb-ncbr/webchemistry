// ------------------------------------------
// Simple Computation API
// Created by David Sehnal (c) 2011
//
// THE CODE IS PROVIDED AS IS bla bla bla...
// ------------------------------------------

namespace WebChemistry.Framework.Core
{
    /// <summary>
    /// Represents a ComputationProgress's tick.
    /// </summary>
    public sealed class ProgressTick
    {
        /// <summary>
        /// Status of the computation. For example "Fetching Data..."
        /// </summary>
        public string StatusText { get; private set; }

        /// <summary>
        /// Determines if the computation can track progress.
        /// </summary>
        public bool IsIndeterminate { get; private set; }

        /// <summary>
        /// Current computation progress.
        /// </summary>
        public int Current { get; private set; }

        /// <summary>
        /// Computation length.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Can cancel.
        /// </summary>
        public bool CanCancel { get; private set; }

        /// <summary>
        /// Creates an instance of progress tick.
        /// </summary>
        internal ProgressTick(string status, bool isIndetermiate, int current, int length, bool canCancel)
        {
            this.StatusText = status;
            this.IsIndeterminate = isIndetermiate;
            this.Current = current;
            this.Length = length;
            this.CanCancel = canCancel;
        }
    }
}