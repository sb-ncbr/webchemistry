using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebChemistry.MotiveAtlas.DataModel
{
    public class AtlasSummary
    {
        public string DatabaseName { get; set; }

        public int CategoryCount { get; set; }
        public int SubCategoryCount { get; set; }
        public int MotiveCategoryCount { get; set; }

        public int StructureCount { get; set; }
        public int MotiveCount { get; set; }

        public DateTime DateCreatedUtc { get; set; }
    }

    public abstract class CommonSummary
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public int StructureCount { get; set; }
        public int MotiveCount { get; set; }
        
        public Dictionary<string, int> TopCountedResidueSurroundings { get; set; }
        public Dictionary<string, int> TopUniqueResidueSurroundings { get; set; }
    }

    public class CategorySummary : CommonSummary
    {
        public int SubCategoryCount { get; set; }
        public int MotiveCategoryCount { get; set; }
    }

    public class SubCategorySummary : CommonSummary
    {
        public string CategoryName { get; set; }
        public int MotiveCategoryCount { get; set; }
    }

    public class MotiveSummary : CommonSummary
    {
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }                
        public string PivotPdbString { get; set; }
        public Dictionary<string, int> TopStructures { get; set; }

        public FrequencyAnalysis ResidueFrequencyAnalysis { get; set; }
        public FrequencyAnalysis ResidueChargeTypeFrequencyAnalysis { get; set; }
    }
}
