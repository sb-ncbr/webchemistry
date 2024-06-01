using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebChemistry.Platform;

namespace WebChemistry.MotiveAtlas.DataModel
{
    public class MotiveAtlasObject : PersistentObjectBase<MotiveAtlasObject>
    {
        public const string DescriptorFilename = "descriptor.json";
        public const string SummaryFilename = "summary.json";
        public const string StructureListFilename = "structures.txt";
        public const string StructureIndexFilename = "structure_index.csv";
        public const string StructureIndexCompressedFilename = StructureIndexFilename + ".zip";
        public const string MotivesFilename = "motifs.zip";

        public string GetObjectPath(string a)
        {
            return CurrentDirectory + "/" + a;
        }

        public string GetObjectPath(string a, string b)
        {
            return CurrentDirectory + "/" + Path.Combine(a, b);
        }

        public string GetObjectPath(string a, string b, string c)
        {
            return CurrentDirectory + "/" + Path.Combine(a, b, c);
        }

        public string GetObjectPath(params string[] xs)
        {
            return CurrentDirectory + "/" + Path.Combine(xs);
        }

        public AtlasDescriptor GetDescriptor()
        {
            return JsonHelper.ReadJsonFile<AtlasDescriptor>(GetObjectPath(DescriptorFilename), createEmptyObjectIfFileDoesNotExist: false);
        }

        public string GetDescriptorFilename()
        {
            return GetObjectPath(DescriptorFilename);
        }

        public AtlasSummary GetAtlasSummary()
        {
            return JsonHelper.ReadJsonFile<AtlasSummary>(GetObjectPath(SummaryFilename), createEmptyObjectIfFileDoesNotExist: false);
        }
        
        public string GetAtlasSummaryFilename()
        {
            return GetObjectPath(SummaryFilename);
        }

        public CategorySummary GetCategorySummary(string categoryId)
        {
            return JsonHelper.ReadJsonFile<CategorySummary>(GetObjectPath(categoryId, SummaryFilename), createEmptyObjectIfFileDoesNotExist: false);
        }

        public string GetCategorySummaryFilename(string categoryId)
        {
            return GetObjectPath(categoryId, SummaryFilename);
        }

        public SubCategorySummary GetSubCategorySummary(string categoryId, string subcategoryId)
        {
            return JsonHelper.ReadJsonFile<SubCategorySummary>(GetObjectPath(categoryId, subcategoryId, SummaryFilename), createEmptyObjectIfFileDoesNotExist: false);
        }

        public string GetSubCategorySummaryFilename(string categoryId, string subcategoryId)
        {
            return GetObjectPath(categoryId, subcategoryId, SummaryFilename);
        }

        public MotiveSummary GetMotiveSummary(string categoryId, string subcategoryId, string motiveId)
        {
            return JsonHelper.ReadJsonFile<MotiveSummary>(GetObjectPath(categoryId, subcategoryId, motiveId, SummaryFilename), createEmptyObjectIfFileDoesNotExist: false);
        }

        public string GetMotiveSummaryFilename(string categoryId, string subcategoryId, string motiveId)
        {
            return GetObjectPath(categoryId, subcategoryId, motiveId, SummaryFilename);
        }
        
        public string GetStructureListFilename(string categoryId)
        {
            return GetObjectPath(categoryId, StructureListFilename);
        }

        public string GetStructureListFilename(string categoryId, string subcategoryId)
        {
            return GetObjectPath(categoryId, subcategoryId, StructureListFilename);
        }

        public string GetStructureListFilename(string categoryId, string subcategoryId, string motiveId)
        {
            return GetObjectPath(categoryId, subcategoryId, motiveId, StructureListFilename);
        }

        public string GetStructureIndexFilename(string categoryId)
        {
            return GetObjectPath(categoryId, StructureIndexFilename);
        }

        public string GetStructureIndexFilename(string categoryId, string subcategoryId)
        {
            return GetObjectPath(categoryId, subcategoryId, StructureIndexFilename);
        }

        public string GetStructureIndexFilename(string categoryId, string subcategoryId, string motiveId)
        {
            return GetObjectPath(categoryId, subcategoryId, motiveId, StructureIndexFilename);
        }

        public string GetMotivesFilename(string categoryId)
        {
            return GetObjectPath(categoryId, MotivesFilename);
        }

        public string GetMotivesFilename(string categoryId, string subcategoryId)
        {
            return GetObjectPath(categoryId, subcategoryId, MotivesFilename);
        }

        public string GetMotivesFilename(string categoryId, string subcategoryId, string motiveId)
        {
            return GetObjectPath(categoryId, subcategoryId, motiveId, MotivesFilename);
        }

        public static MotiveAtlasObject Create(EntityId id)
        {
            return CreateAndSave(id, x => { });
        }
    }

    public class MotiveAtlasApp : PersistentObjectBase<MotiveAtlasApp>
    {
        /// <summary>
        /// Increments the atlas counter and creates new atlas object to be used by the analyzer.
        /// </summary>
        /// <returns></returns>
        public MotiveAtlasObject CreateNewAtlas()
        {
            AtlasCounter++;
            Save();
            return MotiveAtlasObject.Create(Id.GetChildId(string.Format("{0:X}", AtlasCounter)));
        }

        /// <summary>
        /// Updates the current atlas.
        /// </summary>
        /// <param name="atlas"></param>
        public void SetCurrentAtlas(MotiveAtlasObject atlas)
        {
            this.AtlasId = atlas.Id;
            this.Save();
        }

        /// <summary>
        /// Reads the atlas object.
        /// </summary>
        /// <returns></returns>
        public MotiveAtlasObject GetAtlas()
        {
            return MotiveAtlasObject.Load(AtlasId);
        }

        /// <summary>
        /// Number of atlases created.
        /// </summary>
        public int AtlasCounter { get; set; }

        /// <summary>
        /// The actual atlas object id.
        /// </summary>
        public EntityId AtlasId { get; set; }

        /// <summary>
        /// Create new instance of the app.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static MotiveAtlasApp Create(EntityId id)
        {
            return CreateAndSave(id, _ => { });
        }
    }
}
