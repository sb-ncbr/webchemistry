namespace WebChemistry.Platform
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Base class for platform persistent object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PersistentObjectBase<T> : IEquatable<T>
        where T : PersistentObjectBase<T>, new()
    {
        const string objectFilename = "_object.json";

        string currentDirectory = null;
        /// <summary>
        /// Current absolute directory path of the object.
        /// </summary>
        [JsonIgnore]
        protected string CurrentDirectory 
        {
            get
            {
                return currentDirectory = currentDirectory ?? Id.GetEntityPath();
            }
        }
        
        /// <summary>
        /// Object id.
        /// </summary>
        public EntityId Id { get; set; }
        
        /// <summary>
        /// The object type name.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// When the object was created. In universal time.
        /// </summary>
        public DateTime DateCreated { get; set; }
                
        /// <summary>
        /// Save the object info.
        /// </summary>
        protected void Save()
        {
            JsonHelper.WriteJsonFile(Path.Combine(CurrentDirectory, objectFilename), this);
        }

        /// <summary>
        /// What to call on load.
        /// </summary>
        protected virtual void OnObjectLoaded()
        {
        }

        /// <summary>
        /// Load the object.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T Load(EntityId id)
        {
            var dir = id.GetEntityPath();
            var fn = Path.Combine(dir, objectFilename);
            
            if (!File.Exists(fn)) throw new ArgumentException(string.Format("Info file missing in folder '{0}'", dir));
            
            var ret = JsonHelper.ReadJsonFile<T>(fn);
            //var ret = JsonConvert.DeserializeObject<T>(PersistentObjectStringCache.GetContent(fn));
            if (ret.Type != typeof(T).Name) throw new TypeAccessException(string.Format("This is not the object you are looking for (you were looking for {0} and found {1}).", typeof(T).Name, ret.Type));
            ret.OnObjectLoaded();
            return ret;
        }

        /// <summary>
        /// Load the object if it exists. If not, return null.
        /// Type validation can still throw.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T TryLoad(EntityId id)
        {
            var dir = id.GetEntityPath();
            var fn = Path.Combine(dir, objectFilename);

            if (!File.Exists(fn)) return null;

            var ret = JsonHelper.ReadJsonFile<T>(fn);
            //var ret = JsonConvert.DeserializeObject<T>(PersistentObjectStringCache.GetContent(fn));
            if (ret.Type != typeof(T).Name) throw new TypeAccessException(string.Format("This is not the object you are looking for (you were looking for {0} and found {1}).", typeof(T).Name, ret.Type));
            ret.OnObjectLoaded();
            return ret;
        }

        /// <summary>
        /// Check if an object exist. (does not do a typecheck!)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Exists(EntityId id)
        {
            return Directory.Exists(id.GetEntityPath());
        }

        /// <summary>
        /// Create the object and then save it.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="customParams"></param>
        /// <returns></returns>
        protected static T CreateAndSave(EntityId id, Action<T> customParams = null)
        {
            var ret = new T()
            {
                Id = id,
                Type = typeof(T).Name,
                DateCreated = DateTimeService.GetCurrentTime()
            };
            if (!Directory.Exists(ret.CurrentDirectory)) Directory.CreateDirectory(ret.CurrentDirectory);
            if (customParams != null) customParams(ret);
            ret.Save();
            return ret;
        }
                
        /// <summary>
        /// Use a mutex to lock the action.
        /// </summary>
        /// <param name="action"></param>
        public void Locked(Action action)
        {
            Mutex mutex = null;

            try
            {
                mutex = new Mutex(false, Id.ToString());
                mutex.WaitOne();
                action();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Use a mutex to lock the action.
        /// </summary>
        /// <param name="action"></param>
        public TAction Locked<TAction>(Func<TAction> action)
        {
            Mutex mutex = null;

            try
            {
                mutex = new Mutex(false, Id.ToString());
                mutex.WaitOne();
                return action();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Equals...
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(T other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as T;
            if (other == null) return false;
            return Equals(other);
        }

        /// <summary>
        /// Id.GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
