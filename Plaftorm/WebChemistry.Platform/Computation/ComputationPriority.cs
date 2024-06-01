using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.Platform.Computation
{
    /// <summary>
    /// Computation type determines which queue is used by the scheduler.
    /// </summary>
    public enum ComputationPriority
    {
        /// <summary>
        /// Default type. Standard computations like SiteBidner or Queries.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Database operations. Moving a repository to a database and the sort.
        /// </summary>
        Data = 1,

        /// <summary>
        /// Is always scheduled no matter what.
        /// </summary>
        Divine = 100
    }
}
