namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Database manager.
    /// </summary>
    public class DatabaseManager : ManagerBase<DatabaseManager, DatabaseInfo, DatabaseInfo.Index, DatabaseInfo.Update>
    {        
        /// <summary>
        /// Check if the database exists.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public bool Exists(string databaseName)
        {
            return ReadIndex().FirstOrDefault(e => e.Entry.Name.Equals(databaseName, StringComparison.Ordinal)) != null;
        }
         
        /// <summary>
        /// Returns a database with a given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DatabaseInfo GetDatabaseByName(string name)
        {
            return ReadIndex()
                .Where(e => e.Entry.Name.Equals(name, StringComparison.Ordinal))
                .Select(e => DatabaseInfo.Load(e.Id))
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Delete database.
        /// </summary>
        /// <param name="id"></param>
        public void Delete(EntityId id)
        {
            if (!DatabaseInfo.Exists(id)) throw new ArgumentException("There is no database with id " + id + ".");

            Directory.Delete(id.GetEntityPath(), true);
            RemoveFromIndex(id);
        }
        
        protected override DatabaseInfo LoadElement(EntityId id)
        {
            return DatabaseInfo.Load(id);
        }
        
        /// <summary>
        /// Creates an empty database.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="customId">useful for public databases</param>
        /// <returns></returns>
        public DatabaseInfo CreateDatabase(string name, string customId = null, string description = null)
        {
            if (Exists(name)) throw new InvalidOperationException(string.Format("The database with the name '{0}' already exists.", name));

            var db = DatabaseInfo.Create(this, name, customId: customId, description: description);
            AddToIndex(db);
            return db;
        }
    }
}
