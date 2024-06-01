namespace WebChemistry.Platform.Server
{
    using System;
    using System.IO;
    using WebChemistry.Platform.Computation;
    using WebChemistry.Platform.MoleculeDatabase;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// Server base.
    /// </summary>
    public abstract class ServerBase
    {
        /// <summary>
        /// Server name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Base path prefix for all platform services.
        /// Should be an absolute path.
        /// Always ends with \ or /.
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        /// Get current server entity id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EntityId GetServerEntityId(string id)
        {
            return new EntityId(Name, id);
        }

        /// <summary>
        /// Get nested entity ID.
        /// </summary>
        /// <param name="id0"></param>
        /// <param name="id1"></param>
        /// <returns></returns>
        public EntityId GetServerChildId(string parent, string child)
        {
            return EntityId.CreateChildId(Name, parent, child);
        }
         
        protected ServerBase(string name, string root)
        {
            this.Name = name;
            this.Root = root;
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
        }
    }
}
