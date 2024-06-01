using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.MotiveAtlas.DataModel;
using WebChemistry.Queries.Core;
using WebChemistry.Queries.Core.Queries;
using WebChemistry.Platform;

namespace WebChemistry.MotiveAtlas.Analyzer
{
    class AtlasAnalysisDescriptor
    {
        public const int TopCount = 10;

        public static Dictionary<string, int> GetTopCounts(Dictionary<string, int> counts)
        {
            return counts.OrderByDescending(e => e.Value).ThenBy(g => g.Key).Take(AtlasAnalysisDescriptor.TopCount).ToDictionary(g => g.Key, g => g.Value, StringComparer.Ordinal);
        }

        public string DatabaseName { get; set; }
        public List<CategoryAnalysisDescriptor> Categories { get; set; }

        /// <summary>
        /// Numbers of individual residues
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> StructureResidueCounts { get; private set; }        

        /// <summary>
        /// Total residue counts by type.
        /// </summary>
        public Dictionary<string, int> StructureResidueTotalCounts { get; set; }        

        /// <summary>
        /// Total residue counts per structure, excluding water residues.
        /// </summary>
        public Dictionary<string, int> NumberOfResiduesPerStructure { get; set; }

        /// <summary>
        /// Total number of non-water residues in the database.
        /// </summary>
        public int TotalResidueCount { get; set; }
        
        public AtlasAnalysisDescriptor()
        {
            StructureResidueCounts = new Dictionary<string, Dictionary<string, int>>(100000, StringComparer.Ordinal);
        }

        public AtlasSummary Summary { get; private set; }

        public void ComputeSummary()
        {
            Summary = new AtlasSummary
            {
                DatabaseName = DatabaseName,
                DateCreatedUtc = DateTimeService.GetCurrentTime(),

                CategoryCount = Categories.Count,
                SubCategoryCount = Categories.Sum(c => c.Summary.SubCategoryCount),
                MotiveCategoryCount = Categories.Sum(c => c.Summary.MotiveCategoryCount),
                MotiveCount = Categories.Sum(c => c.Summary.MotiveCount),
                StructureCount = Categories.Sum(c => c.Summary.StructureCount)
            };
        }

        public AtlasDescriptor GetDescriptor()
        {
            return new AtlasDescriptor
            {
                DatabaseName = DatabaseName,
                Categories = Categories.Select(c => c.GetDescriptor()).ToArray(),
            };
        }

        public void CreateDirectoryStructure(string basePath)
        {
            foreach (var c in Categories)
            {
                var cDir = Path.Combine(basePath, c.Id);
                Directory.CreateDirectory(cDir);
                foreach (var s in c.SubCategories)
                {
                    var sDir = Path.Combine(cDir, s.Id);
                    Directory.CreateDirectory(cDir);
                    foreach (var m in s.Motives)
                    {
                        var mDir = Path.Combine(sDir, m.Id);
                        Directory.CreateDirectory(mDir);

                        File.Create(Path.Combine(mDir, MotiveAnalysisDescriptor.MotivesSourceFilename)).Dispose();
                    }
                }
            }
        }
    }
    
    abstract class AnalysisDescriptorBase
    {
        public AtlasAnalysisDescriptor Atlas { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        
        public HashSet<string> AffectedStructures { get; private set; }

        protected AnalysisDescriptorBase()
        {
            AffectedStructures = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    class CategoryAnalysisDescriptor : AnalysisDescriptorBase
    {
        public List<SubCategoryAnalysisDescriptor> SubCategories { get; set; }

        public CategorySummary Summary { get; set; }

        public string GetFolder(string baseFolder)
        {
            return Path.Combine(baseFolder, Id);
        }

        public void ComputeSummary()
        {
            foreach (var c in SubCategories)
            {
                AffectedStructures.UnionWith(c.AffectedStructures);
            }
            var topUnique = AtlasAnalysisDescriptor.GetTopCounts(FrequencyCounter.Sum(SubCategories.Select(c => c.Summary.TopUniqueResidueSurroundings)));
            var topCounted = AtlasAnalysisDescriptor.GetTopCounts(FrequencyCounter.Sum(SubCategories.Select(c => c.Summary.TopCountedResidueSurroundings)));

            Summary = new CategorySummary
            {
                Name = Name,
                Description = Description,
                               
                StructureCount = AffectedStructures.Count,
                SubCategoryCount = SubCategories.Count,
                MotiveCategoryCount = SubCategories.Sum(c => c.Summary.MotiveCategoryCount),
                MotiveCount = SubCategories.Sum(c => c.Summary.MotiveCount),
                                
                TopCountedResidueSurroundings = topCounted,
                TopUniqueResidueSurroundings = topUnique,
            };
        }

        public CategoryDescriptor GetDescriptor()
        {
            return new CategoryDescriptor
            {
                Id = Id,
                Name = Name,
                Description = Description,
                SubCategories = SubCategories.Select(c => c.GetDescriptor()).ToArray()
            };
        }
    }

    /// <summary>
    /// Represents a subcategory of motifs.
    /// </summary>
    class SubCategoryAnalysisDescriptor : AnalysisDescriptorBase
    {
        /// <summary>
        /// The parent category.
        /// </summary>
        public CategoryAnalysisDescriptor Category { get; set; }        

        /// <summary>
        /// The child motifs.
        /// </summary>
        public List<MotiveAnalysisDescriptor> Motives { get; set; }

        /// <summary>
        /// Summary generated by ComputeSummary function.
        /// </summary>
        public SubCategorySummary Summary { get; private set; }

        /// <summary>
        /// Computes the summary.
        /// </summary>
        public void ComputeSummary()
        {
            foreach (var m in Motives) 
            {
                AffectedStructures.UnionWith(m.AffectedStructures);                
            }
            var topUnique = AtlasAnalysisDescriptor.GetTopCounts(FrequencyCounter.Sum(Motives.Select(m => m.Summary.TopUniqueResidueSurroundings)));
            var topCounted = AtlasAnalysisDescriptor.GetTopCounts(FrequencyCounter.Sum(Motives.Select(m => m.Summary.TopCountedResidueSurroundings)));

            Summary = new SubCategorySummary
            {
                Name = Name,
                Description = Description,
                CategoryName = Category.Name,
                
                MotiveCategoryCount = Motives.Count,
                MotiveCount = Motives.Sum(m => m.MotiveCount),
                                
                StructureCount = AffectedStructures.Count,

                TopCountedResidueSurroundings = topCounted,
                TopUniqueResidueSurroundings = topUnique,
            };
        }

        /// <summary>
        /// Data folder for this subcat.
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        public string GetFolder(string baseFolder)
        {
            return Path.Combine(baseFolder, Category.Id, Id);
        }

        /// <summary>
        /// A descriptor for UI purposes.
        /// </summary>
        /// <returns></returns>
        public SubCategoryDescriptor GetDescriptor()
        {
            return new SubCategoryDescriptor
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Motives = Motives.Select(c => c.GetDescriptor()).ToArray()
            };
        }
    }

    /// <summary>
    /// Represents the analysis of this motif.
    /// </summary>
    class MotiveAnalysisDescriptor : AnalysisDescriptorBase
    {
        public const string AmbientQueryGroup = "amb5";
        public const string ConnectedQueryGroup = "conn2";

        public const string MotivesSourceFilename = "motives.src";

        /// <summary>
        /// Parent subcateogry.
        /// </summary>
        public SubCategoryAnalysisDescriptor SubCategory { get; set; }

        /// <summary>
        /// The base query.
        /// </summary>
        public QueryMotive BaseQuery { get; set; }

        /// <summary>
        /// The query groups to be executed.
        /// </summary>
        public Dictionary<string, QueryMotive> QueryGroups { get; set; }
        
        /// <summary>
        /// A simple heuristic to check if it's worth to visit this structure.
        /// </summary>
        public Func<IStructure, bool> ShouldVisitStructure { get; set; }

        /// <summary>
        /// Motif counts per structure.
        /// </summary>
        public Dictionary<string, int> StructureCounts { get; private set; }

        /// <summary>
        /// Total number of motifs.
        /// </summary>
        public int MotiveCount { get; set; }

        /// <summary>
        /// Pivot base motif with the most atoms.
        /// </summary>
        public IStructure Pivot { get; set; }

        /// <summary>
        /// The entries.
        /// </summary>
        public List<MotiveAnalysisEntry> Motives { get; private set; }

        /// <summary>
        /// Residue counts for motifs in this set.
        /// </summary>
        public Dictionary<string, int> MotiveAmbientResidueCounts { get; private set; }

        /// <summary>
        /// A summary for this motif.
        /// </summary>
        public MotiveSummary Summary { get; private set; }

        public void ComputeSummary()
        {
            var topUnique = Motives.GroupBy(m => m.AmbientUniqueResidueString).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(AtlasAnalysisDescriptor.TopCount).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            var topSignatures = Motives.GroupBy(m => m.AmbientCountedResidueString).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(AtlasAnalysisDescriptor.TopCount).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            var rNames = MotiveAmbientResidueCounts.Keys.ToArray();
            var src = SubCategory.Category.Atlas.StructureResidueCounts;
            var resnums = SubCategory.Category.Atlas.NumberOfResiduesPerStructure;
            var structureCounts = FrequencyCounter.Sum(AffectedStructures.Select(s => src[s]));
            var structureTotalCount = AffectedStructures.Sum(s => resnums[s]);

            var resFreqs = new FrequencyAnalysis
            {
                Motives = FrequencyCounter.ToFreqStats(MotiveAmbientResidueCounts),
                Structures = FrequencyCounter.ToFreqStats(structureCounts, rNames, structureTotalCount),
                Database = FrequencyCounter.ToFreqStats(SubCategory.Category.Atlas.StructureResidueTotalCounts, rNames, SubCategory.Category.Atlas.TotalResidueCount),
            };

            
            var resChargeTypesFreqs = new FrequencyAnalysis
            {
                Motives = FrequencyCounter.ToFreqStats(FrequencyCounter.ToResidueChargeTypeCounts(MotiveAmbientResidueCounts)),
                Structures = FrequencyCounter.ToFreqStats(FrequencyCounter.ToResidueChargeTypeCounts(FrequencyCounter.Trim(structureCounts, rNames)), structureTotalCount),
                Database = FrequencyCounter.ToFreqStats(FrequencyCounter.ToResidueChargeTypeCounts(FrequencyCounter.Trim(SubCategory.Category.Atlas.StructureResidueTotalCounts, rNames)), SubCategory.Category.Atlas.TotalResidueCount),
            };
            
            Summary = new MotiveSummary
            {                
                Name = Name,
                Description = Description,
                CategoryName = SubCategory.Category.Name,
                SubCategoryName = SubCategory.Name,

                StructureCount = StructureCounts.Count,
                MotiveCount = MotiveCount,

                PivotPdbString = Pivot == null ? "" : Pivot.ToPdbString(),

                TopStructures = StructureCounts.OrderByDescending(c => c.Value).Take(AtlasAnalysisDescriptor.TopCount).ToDictionary(k => k.Key, k => k.Value, StringComparer.Ordinal),

                ResidueFrequencyAnalysis = resFreqs,
                ResidueChargeTypeFrequencyAnalysis = resChargeTypesFreqs,

                TopUniqueResidueSurroundings = topUnique,
                TopCountedResidueSurroundings = topSignatures
            };
        }

        /// <summary>
        /// Creates the descriptors for UI view.
        /// </summary>
        /// <returns></returns>
        public MotiveDescriptor GetDescriptor()
        {
            return new MotiveDescriptor
            {
                Id = Id,
                Name = Name,
                Description = Description,
                MotiveCount = Motives.Count
            };
        }

        /// <summary>
        /// Get the data folder for this motif.
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        public string GetFolder(string baseFolder)
        {
            return Path.Combine(baseFolder, SubCategory.Category.Id, SubCategory.Id, Id);
        }
                
        public MotiveAnalysisDescriptor()
        {
            StructureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            MotiveAmbientResidueCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            Motives = new List<MotiveAnalysisEntry>();
        }
    }

    /// <summary>
    /// Ambient residues do not contain the base motif.
    /// </summary>
    class MotiveAnalysisEntry
    {
        public string Id { get; set; }
        public string ParentId { get; set; }

        public string MotiveId { get; set; }
        public string SubCategoryId { get; set; }
        public string CategoryId { get; set; }

        public int BaseAtomCount { get; set; }
        public string BaseAtomString { get; set; }
        public string BaseUniqueResidueString { get; set; }
        public string BaseCountedResidueString { get; set; }
        public string BaseResidueIndentifiers { get; set; }
        
        public int AmbientAtomCount { get; set; }
        public string AmbientAtomString { get; set; }
        public string AmbientUniqueResidueString { get; set; }
        public string AmbientCountedResidueString { get; set; }
        public string AmbientResidueIndentifiers { get; set; }

        /// <summary>
        /// Create an exporter for this.
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static ListExporter GetExporter(IEnumerable<MotiveAnalysisEntry> xs, string separator)
        {
            return xs.GetExporter(separator)
                .AddExportableColumn(x => x.Id, ColumnType.String, "Id")
                .AddExportableColumn(x => x.ParentId, ColumnType.String, "ParentId")
                .AddExportableColumn(x => x.CategoryId, ColumnType.String, "CategoryId")
                .AddExportableColumn(x => x.SubCategoryId, ColumnType.String, "SubCategoryId")
                .AddExportableColumn(x => x.MotiveId, ColumnType.String, "MotiveId")

                .AddExportableColumn(x => x.BaseAtomCount, ColumnType.Number, "BaseAtomCount")
                .AddExportableColumn(x => x.BaseAtomString, ColumnType.String, "BaseAtomString")
                .AddExportableColumn(x => x.BaseCountedResidueString, ColumnType.String, "BaseCountedResidueString")
                .AddExportableColumn(x => x.BaseUniqueResidueString, ColumnType.String, "BaseUniqueResidueString")
                .AddExportableColumn(x => x.BaseResidueIndentifiers, ColumnType.String, "BaseResidueIndentifiers")

                .AddExportableColumn(x => x.AmbientAtomCount, ColumnType.Number, "AmbientAtomCount")
                .AddExportableColumn(x => x.AmbientAtomString, ColumnType.String, "AmbientAtomString")
                .AddExportableColumn(x => x.AmbientCountedResidueString, ColumnType.String, "AmbientCountedResidueString")
                .AddExportableColumn(x => x.AmbientUniqueResidueString, ColumnType.String, "AmbientUniqueResidueString")
                .AddExportableColumn(x => x.AmbientResidueIndentifiers, ColumnType.String, "AmbientResidueIndentifiers");
        }
    }
}
