namespace WebChemistry.Platform.MoleculeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
using WebChemistry.Platform.MoleculeDatabase.Filtering;

    /// <summary>
    /// Database view manager.
    /// </summary>
    public class DatabaseViewManager : ManagerBase<DatabaseViewManager, DatabaseView, DatabaseView.Index, DatabaseView.Update>
    {
        /// <summary>
        /// Check if the view exists.
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public bool Exists(string viewName)
        {
            return ReadIndex().FirstOrDefault(e => e.Entry.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase)) != null;
        }
                  
        /// <summary>
        /// Returns a view with a given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DatabaseView GetViewByName(string name)
        {
            return ReadIndex()
                .Where(e => e.Entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(e => DatabaseView.Load(e.Id))
                .FirstOrDefault();
        }

        /// <summary>
        /// Delete database.
        /// </summary>
        /// <param name="id"></param>
        public void Delete(EntityId id)
        {
            if (!DatabaseView.Exists(id)) throw new ArgumentException("There is no view with id " + id + ".");

            Directory.Delete(id.GetEntityPath(), true);
            RemoveFromIndex(id);
        }

        protected override DatabaseView LoadElement(EntityId id)
        {
            return DatabaseView.Load(id);
        }

        /// <summary>
        /// Create a view.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        public DatabaseView CreateView(DatabaseInfo database, string name, string description = null, /*bool includeObsolete = false, */IEnumerable<EntryFilter> filters = null)
        {
            if (Exists(name)) throw new InvalidOperationException(string.Format("The view with the name '{0}' already exists.", name));

            var view = DatabaseView.Create(database, this, name, description: description, filters: filters);
            AddToIndex(view);
            return view;
        }

        void EnsureConsistent()
        {

            ////// use the database ID from INDEX!!!!!!!
            foreach (var view in ReadIndex())
            {
                if (!DatabaseInfo.Exists(view.Entry.DatabaseId)) Delete(view.Id);
            }
        }

        protected override void OnManagerLoaded()
        {
            EnsureConsistent();
        }
    }
}
