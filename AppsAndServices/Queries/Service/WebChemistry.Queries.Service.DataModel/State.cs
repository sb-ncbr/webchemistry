
namespace WebChemistry.Queries.Service.DataModel
{
    using System.Collections.Generic;
    using WebChemistry.Platform;

    /// <summary>
    /// Custom state.
    /// </summary>
    public class QueriesServiceState
    {
        /// <summary>
        /// Number of motives found.
        /// </summary>
        public int MotivesFound { get; set; }

        /// <summary>
        /// Number of errors.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Was the motive limit reached.
        /// </summary>
        public bool MotiveLimitReached { get; set; }              
    }
}
