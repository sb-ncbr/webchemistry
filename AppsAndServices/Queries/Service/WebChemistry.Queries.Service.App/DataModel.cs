namespace WebChemistry.Queries.Service.App
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using WebChemistry.Queries.Service.DataModel;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.MoleculeDatabase.Filtering;
    
    public class QueriesDatabaseFilterResult
    {
        public string[] Errors { get; set; }
        public string[] MissingEntries { get; set; }
        public IList<DatabaseIndexEntry> Entries { get; set; }

        public QueriesDatabaseFilterResult()
        {
            Errors = new string[0];
            MissingEntries = new string[0];
            Entries = new DatabaseIndexEntry[0];
        }
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum QueriesComputationDataSource
    {
        Db = 0,
        List,
        Filtered
    }

    public class QueriesComputationConfig
    {
        public QueryInfo[] Queries { get; set; }
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public QueriesComputationDataSource DataSource { get; set; }
        public bool DoValidation { get; set; }
        public EntryFilter[] Filters { get; set; }
        public string[] EntryList { get; set; }
        public bool NotifyUser { get; set; }
        public string UserEmail { get; set; }
    }

    public class QueriesCreateComputationResult
    {
        public string[] Errors { get; set; }
        public string[] MissingEntries { get; set; }
        public bool HasEmptyInput { get; set; }
        public ComputationInfo Computation { get; set; }

        public QueriesCreateComputationResult()
        {
            Errors = new string[0];
            MissingEntries = new string[0];
        }
    }
}
