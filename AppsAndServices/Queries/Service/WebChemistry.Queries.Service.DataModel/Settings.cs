namespace WebChemistry.Queries.Service.DataModel
{
    using System;
    using System.ComponentModel;
    using WebChemistry.Platform;
    using WebChemistry.Platform.Services;

    /// <summary>
    /// Query info.
    /// </summary>
    [HelpDescribe, HelpTypeName("QueryInfo")]
    public class QueryInfo
    {
        /// <summary>
        /// Unique Id.
        /// </summary>
        [Description("A unique identifier of the query.")]
        public string Id { get; set; }
        
        /// <summary>
        /// Query string.
        /// </summary>
        [Description("The query expression.")]
        public string QueryString { get; set; }

        /// <summary>
        /// Create the info.
        /// </summary>
        public QueryInfo()
        {
        }
    }

    /// <summary>
    /// Service settings.
    /// </summary>
    public class QueriesServiceSettings
    {
        /// <summary>
        /// The queries.
        /// </summary>
        public QueryInfo[] Queries { get; set; }

        /// <summary>
        /// Maximum number of patterns that can be found during one run.
        /// </summary>
        public int MotiveCountLimit { get; set; }

        /// <summary>
        /// Maximum number of patterns that can be identified in a single computation.
        /// </summary>
        public long AtomCountLimit { get; set; }

        /// <summary>
        /// Maximum degree of parallelism.
        /// </summary>
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Do pattern validation?
        /// </summary>
        public bool DoValidation { get; set; }

        /// <summary>
        /// The entity ID of the validator app.
        /// </summary>
        public EntityId? ValidatorId { get; set; }

        /// <summary>
        /// Notify the user that the computation has finished?
        /// </summary>
        public bool NotifyUser { get; set; }

        /// <summary>
        /// Configuration for user notification.
        /// </summary>
        public WebChemistry.Platform.Users.UserComputationFinishedNotificationConfig NotifyUserConfig { get; set; }
        
        public QueriesServiceSettings()
        {
            MotiveCountLimit = int.MaxValue;
            AtomCountLimit = long.MaxValue;
            MaxParallelism = 8;
        }
    }

    /// <summary>
    /// Service settings.
    /// </summary>
    public class QueriesStandaloneServiceSettings
    {
        /// <summary>
        /// Database snapshot id.
        /// </summary>
        [Description("A list of folders containing the input structures.")]
        public string[] InputFolders { get; set; }

        /// <summary>
        /// The queries.
        /// </summary>
        [Description("A list of queries."), HelpDescribe(typeof(QueryInfo)), HelpTypeName("QueryInfo")]
        public QueryInfo[] Queries { get; set; }

        /// <summary>
        /// Generate only statistics?
        /// </summary>
        [Description("If `true`, the files with patterns are not exported.")]
        public bool StatisticsOnly { get; set; }

        /// <summary>
        /// Path to the CSA datafile.
        /// </summary>
        [Description("Optional path to a file with CSA database that allows the CSA() query to work. The file is a CSV file that must contain the columns `PdbID, SiteNumber, ?, ?, ChainId, ResidueNumber, ...`")]
        public string CSAPath { get; set; }

        /// <summary>
        /// Maximum degree of parallelism.
        /// </summary>
        [Description("The maximum number of structures that can be processed simultaneously.")]
        [DefaultValue(8)]
        public int MaxParallelism { get; set; }

        public QueriesStandaloneServiceSettings()
        {
            MaxParallelism = 8;
        }
    }
}
