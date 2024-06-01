namespace WebChemistry.MotiveValidator.Database
{
    using ICSharpCode.SharpZipLib.Zip;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Platform;

    /// <summary>
    /// When using from web app, do not forget to dispose on app end.
    /// </summary>
    public class MotiveValidatorDatabaseDataInterface : IDisposable
    {
        bool Disposing;
        object Sync;
        ZipArchiveInterface Zip;
        
        /// <summary>
        /// Get names of all entries.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetEntryNames()
        {
            return Zip.GetEntryNames();
        }

        public bool HasEntry(string path)
        {
            return Zip.HasEntry(path);
        }

        public string GetEntry(string path)
        {
            return Zip.GetEntryString(path);            
        }
        
        public void Dispose()
        {
            lock (Sync)
            {
                if (Disposing) return;
                Disposing = true;
            }

            if (Zip != null)
            {
                Zip.Dispose();
                Zip = null;
            }
        }
                
        /// <summary>
        /// Creates the interface.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="inMemory"></param>
        public MotiveValidatorDatabaseDataInterface(MotiveValidatorDatabaseApp app, bool inMemory)
        {
            this.Zip = ZipArchiveInterface.FromFile(Path.Combine(app.GetCurrentDatabasePath(), "data.zip"), inMemory);
            this.Sync = new object();
        }
    }
    
    /// <summary>
    /// When using from web app, do not forget to dispose on app end.
    /// </summary>
    public class MotiveValidatorDatabaseInterfaceProvider
    {
        bool InMemory;

        /// <summary>
        /// Active version id.
        /// </summary>
        string DatabaseVersionId = "<empty>";
        MotiveValidatorDatabaseDataInterface DataInterface;
        object creationLock = new object();

        /// <summary>
        /// Get the interface to the database.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public MotiveValidatorDatabaseDataInterface GetInterface(MotiveValidatorDatabaseApp app)
        {
            lock (creationLock)
            {
                if (!app.DatabaseVersionId.Equals(DatabaseVersionId, StringComparison.Ordinal) || DataInterface == null)
                {
                    try
                    {
                        if (DataInterface != null)
                        {
                            DataInterface.Dispose();
                            DataInterface = null;
                            GC.Collect();
                        }

                        DataInterface = new MotiveValidatorDatabaseDataInterface(app, InMemory);
                        DatabaseVersionId = app.DatabaseVersionId;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Error loading MotiveValidator Database: " + e.Message, e);
                    }
                }
                return DataInterface;
            }
        }

        public void Dispose()
        {
            if (DataInterface != null)
            {
                DataInterface.Dispose();
                DataInterface = null;
            }
        }

        public MotiveValidatorDatabaseInterfaceProvider(bool inMemory)
        {
            this.InMemory = inMemory;
        }
    }
}
