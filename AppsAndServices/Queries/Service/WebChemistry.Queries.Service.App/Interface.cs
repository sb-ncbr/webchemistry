namespace WebChemistry.Queries.Service.App
{
    using System;
    using System.IO;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform;

    /// <summary>
    /// Interface to the result folder.
    /// </summary>
    public class QueriesResultInterface : IDisposable
    {
        bool Initialized;

        object Sync;
        string ResultFolder;
        ZipArchiveInterface Zip;

        void Init()
        {
            lock (Sync)
            {
                if (Initialized) return;

                Zip = ZipArchiveInterface.FromFile(Path.Combine(ResultFolder, "result.zip"));
                Initialized = true;
            }
        }
        
        /// <summary>
        /// Read the query data.
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        public string GetQueryDataString(string queryId)
        {
            Init();
            return Zip.GetEntryString(Path.Combine(queryId, "data.json"));        
        }

        /// <summary>
        /// Get query result data for a particular query id.
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        public QueriesComputationQueryResult GetQueryData(string queryId)
        {
            return JsonHelper.FromJsonString<QueriesComputationQueryResult>(GetQueryDataString(queryId));
        }

        /// <summary>
        /// Get entry PDB source.
        /// </summary>
        /// <param name="queryId"></param>
        /// <param name="patternId"></param>
        /// <returns></returns>
        public string GetPatternPdbSource(string queryId, string patternId)
        {
            Init();
            return Zip.GetEntryString(Path.Combine(queryId, "patterns", patternId + ".pdb"));
        }

        /// <summary>
        /// Read the summary and return it as a string.
        /// </summary>
        /// <returns></returns>
        public string GetSummaryString()
        {
            return File.ReadAllText(Path.Combine(ResultFolder, "summary.json"));
        }

        /// <summary>
        /// Get the computation summary.
        /// </summary>
        /// <returns></returns>
        public ComputationSummary GetSummary()
        {
            return JsonHelper.FromJsonString<ComputationSummary>(GetSummaryString());
        }
        
        /// <summary>
        /// Return detailed info about all queried structures.
        /// </summary>
        /// <returns></returns>
        public ComputationStructureEntry[] GetInputInfo()
        {
            return JsonHelper.FromJsonString<ComputationStructureEntry[]>(GetInputInfoString()); ;
        }

        /// <summary>
        /// Return detailed info about all queried structures.
        /// </summary>
        /// <returns></returns>
        public string GetInputInfoString()
        {
            Init();
            return Zip.GetEntryString("structures.json");
        }

        /// <summary>
        /// Init the interface.
        /// </summary>
        /// <param name="resultFolder"></param>
        public QueriesResultInterface(string resultFolder)
        {
            this.Sync = new object();
            this.ResultFolder = resultFolder;
        }

        /// <summary>
        /// Release the zip archive.
        /// </summary>
        public void Dispose()
        {
            if (!Initialized || Zip == null) return;
            Zip.Dispose();
            Zip = null;
        }
    }
}
