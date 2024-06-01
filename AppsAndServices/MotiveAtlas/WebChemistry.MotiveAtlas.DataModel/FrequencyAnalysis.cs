using System.Collections.Generic;

namespace WebChemistry.MotiveAtlas.DataModel
{
    /// <summary>
    /// Frequency statistics.
    /// </summary>
    public class FrequencyStats
    {
        /// <summary>
        /// Total element count == Counts.Select(c => c.Value).Sum();
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Frequencies for individual elemnts. All sum to 1.
        /// </summary>
        public Dictionary<string, double> Frequencies { get; set; }
    }

    /// <summary>
    /// Frequency analysis for a given group.
    /// </summary>
    public class FrequencyAnalysis
    {
        public FrequencyStats Database { get; set; }
        public FrequencyStats Structures { get; set; }        
        public FrequencyStats Motives { get; set; }
    }
}
