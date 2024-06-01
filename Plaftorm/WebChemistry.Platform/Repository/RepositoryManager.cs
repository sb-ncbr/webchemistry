namespace WebChemistry.Platform.Repository
{
    using System;

    /// <summary>
    /// Object repository "manager"
    /// </summary>
    public class RepositoryManager
    {
        EntityId id;

        /// <summary>
        /// Get a new id from this repository.
        /// </summary>
        /// <returns></returns>
        public EntityId GetNewEntityId()
        {
            return id.GetChildId(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Get a child entity id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EntityId GetChildEntityId(string id)
        {
            return this.id.GetChildId(id);
        }

        /// <summary>
        /// Create the manager.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RepositoryManager Load(EntityId id)
        {
            return new RepositoryManager { id = id };
        }

        private RepositoryManager()
        {

        }
    }
}
