namespace WebChemistry.Platform.Users
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Repository;

    /// <summary>
    /// User profile.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Max amount of concurrent computations.
        /// </summary>
        public int ConcurrentComputationLimit { get; set; }

        /// <summary>
        /// Maximum amount of motifs that can be computed using the Queries service.
        /// </summary>
        public int QueriesPatternLimit { get; set; }
    }

    /// <summary>
    /// User information.
    /// </summary>
    public class UserInfo : ManagedPersistentObjectBase<UserInfo, UserInfo.Index, object>
    {
        /// <summary>
        /// User index entry.
        /// </summary>
        public class Index
        {
            public string Name { get; set; }
            public string Token { get; set; }
        }

        string GetProfilePath() { return Path.Combine(CurrentDirectory, "profile.json"); }

        /// <summary>
        /// Get the index entry.
        /// </summary>
        internal override Index IndexEntry
        {
            get { return new Index { Name = Name, Token = Token }; }
        }

        /// <summary>
        /// User token. Guid which is also used as the root folder.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A unique user token (guid).
        /// </summary>
        public string Token { get; set; }
        
        internal override void UpdateAndSaveInternal(object model)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the user profile.
        /// </summary>
        /// <returns></returns>
        public UserProfile GetProfile()
        {
            return JsonHelper.ReadJsonFile<UserProfile>(GetProfilePath());
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        public void SaveProfile(UserProfile profile)
        {
            JsonHelper.WriteJsonFile(GetProfilePath(), profile);
        }

        /// <summary>
        /// Return the database manager for the given user.
        /// </summary>
        [JsonIgnore]
        public DatabaseManager Databases
        {
            get { return DatabaseManager.Load(Id.GetChildId("databases")); }
        }

        /// <summary>
        /// Database view manager for the given user.
        /// </summary>
        [JsonIgnore]
        public DatabaseViewManager DatabaseViews
        {
            get
            {
                return DatabaseViewManager.Load(Id.GetChildId("dbviews"));
            }
        }

        /////// <summary>
        /////// Molecule property manager.
        /////// </summary>
        ////[JsonIgnore]
        ////public MoleculePropertyManager Properties
        ////{
        ////    get { return MoleculePropertyManager.Load(Id.GetChildId("props")); }
        ////}

        /// <summary>
        /// Return the repository manager for the given user.
        /// </summary>
        [JsonIgnore]
        public RepositoryManager Repository
        {
            get { return RepositoryManager.Load(Id.GetChildId("repository")); }
        }

        /// <summary>
        /// Returns the computation manager for the given user.
        /// </summary>
        [JsonIgnore]
        public ComputationManager Computations
        {
            get { return ComputationManager.Load(Id.GetChildId("computations")); }
        }

        /// <summary>
        /// Calcute the profile size in bytes.
        /// </summary>
        /// <returns></returns>
        public long CalculateSizeInBytes()
        {
            // maybe case the result and only recalculate max every x hours?
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create user.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        /// <param name="concurrentComputationLimit"></param>
        /// <returns></returns>
        internal static UserInfo Create(UserManager manager, string name, int concurrentComputationLimit = 2)
        {
            var user = CreateAndSave(manager.Id, UserManager.GetIdFromName(name), usr =>
            {
                usr.Name = name;
                usr.Token = Guid.NewGuid().ToString();
            });
            user.SaveProfile(new UserProfile { ConcurrentComputationLimit = concurrentComputationLimit, QueriesPatternLimit = 1000000 });
            return user;
        }
    }
}
