// -----------------------------------------------------------------------
// <copyright file="SimpleProcessManager.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.Platform.Computation
{
    using System.Collections.Generic;
    using System.IO;
    using WebChemistry.Platform.Services;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// Simple computation manager that builds XML index of the processes.
    /// </summary>
    public class ComputationManager : ManagerBase<ComputationManager, ComputationInfo, ComputationInfo.Index, object>
    {
        /// <summary>
        /// Returns all computations of a certain computation.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IEnumerable<ComputationInfo> GetComputationsByServiceId(EntityId id)
        {
            return GetAll(e => e.ServiceId.Equals(id));
        }

        protected override ComputationInfo LoadElement(EntityId id)
        {
            return ComputationInfo.Load(id);
        }

        /// <summary>
        /// Remove a computation.
        /// </summary>
        /// <param name="id"></param>
        public void Remove(EntityId id)
        {
            RemoveInternal(id);
            RemoveFromIndex(id);
        }

        void RemoveInternal(EntityId id)
        {
            var computation = ComputationInfo.TryLoad(id);

            if (computation == null)
            {
                RemoveFromIndex(id);
                return;
            }

            computation.KillIfRunning(true);

            foreach (var d in computation.DependentObjectIds)
            {
                Helpers.DeleteEntity(d);
            }

            try { Directory.Delete(id.GetEntityPath(), true); }
            catch { }
        }
        

        /// <summary>
        /// Removes and terminates computations...
        /// </summary>
        public void RemoveAll()
        {
            foreach (var e in ReadIndex())
            {
                RemoveInternal(e.Id);
            }
            SaveIndex(new List<IndexEntry>());
        }

        /// <summary>
        /// Creates a computation and assigns its state to 'New'. Returns an unique computation id.
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="user"></param>
        /// <param name="service"></param>
        /// <param name="name"></param>
        /// <param name="dependentObjects"></param>
        /// <param name="settings"></param>
        /// <param name="source"></param>
        /// <param name="customPriority"></param>
        /// <param name="customId"></param>
        /// <param name="customState"></param>
        /// <returns></returns>
        public ComputationInfo CreateComputation<TSettings>(UserInfo user, ServiceInfo service, 
            string name, TSettings settings, string source, IEnumerable<EntityId> dependentObjects = null, ComputationPriority? customPriority = null, string customId = null, object customState = null)
            where TSettings : class
        {
            var comp = ComputationInfo.Create(this, user, service, name, settings, source, dependentObjects: dependentObjects, customPriority: customPriority, customId: customId, customState: customState);
            AddToIndex(comp);
            return comp;
        }
    }
}
