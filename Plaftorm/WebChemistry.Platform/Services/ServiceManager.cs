using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WebChemistry.Platform.Computation;

namespace WebChemistry.Platform.Services
{
    /// <summary>
    /// Service manager.
    /// </summary>
    public class ServiceManager : ManagerBase<ServiceManager, ServiceInfo, ServiceInfo.Index, object>
    {
        /// <summary>
        /// Get a service id from it's name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetIdFromName(string name)
        {
            return new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }
        
        /// <summary>
        /// Returns a service from name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ServiceInfo GetService(string name)
        {
            return ServiceInfo.Load(Id.GetChildId(GetIdFromName(name)));
        }

        protected override ServiceInfo LoadElement(EntityId id)
        {
            return ServiceInfo.Load(id);
        }

        /// <summary>
        /// Register or update service.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultComputationPriority"></param>
        /// <param name="executableName"></param>
        /// <param name="sourceFolder"></param>
        /// <returns></returns>
        public ServiceInfo RegisterOrUpdateService(string name, string executableName, 
            ComputationPriority defaultComputationPriority, string sourceFolder)
        {
            var id = GetIdFromName(name);

            if (string.IsNullOrEmpty(id)) throw new ArgumentException(string.Format("Name '{0}' results in an empty id.", name));

            var svc = ServiceInfo.Create(this, name, executableName, defaultComputationPriority, sourceFolder);
            AddToIndex(svc);
            return svc;
        }
    }
}
