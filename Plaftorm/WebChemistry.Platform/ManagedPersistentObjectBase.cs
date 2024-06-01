namespace WebChemistry.Platform
{
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Base class for platform persistent object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TIndex"></typeparam>
    /// <typeparam name="TUpdate"></typeparam>
    public abstract class ManagedPersistentObjectBase<T, TIndex, TUpdate> : PersistentObjectBase<T>
        where T : ManagedPersistentObjectBase<T, TIndex, TUpdate>, new()
    {
        /// <summary>
        /// Index entry.
        /// </summary>
        [JsonIgnore]
        internal abstract TIndex IndexEntry { get; }

        string shortId;
        /// <summary>
        /// Short ID. Used in WebUI.
        /// </summary>
        [JsonIgnore]
        public string ShortId { get { return shortId ?? (shortId = Id.GetChildId()); } }

        /// <summary>
        /// Updates the object based on the update model and then saves it.
        /// </summary>
        /// <param name="model"></param>
        internal abstract void UpdateAndSaveInternal(TUpdate model);

        /// <summary>
        /// Update and save the object.
        /// Exception can be thrown.
        /// </summary>
        /// <param name="model"></param>
        internal void UpdateAndSave(TUpdate model)
        {
            UpdateAndSaveInternal(model);
            this.Save();
        }
        
        /// <summary>
        /// Create the object and then save it.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="id"></param>
        /// <param name="customParams"></param>
        /// <returns></returns>
        protected static T CreateAndSave(EntityId parentId, string id, Action<T> customParams = null)
        {
            return CreateAndSave(parentId.GetChildId(id), obj =>
            {
                if (customParams != null) customParams(obj);
            });
        }
    }
}
