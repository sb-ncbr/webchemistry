namespace WebChemistry.Platform.Computation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using WebChemistry.Platform.Server;
    using WebChemistry.Platform.Users;

    /// <summary>
    /// A computation scheduler.
    /// </summary>
    public class ComputationScheduler : PersistentObjectBase<ComputationScheduler>
    {
        const int DefaultMaxComputationsPerUser = 2;

        /// <summary>
        /// Entry with information about running and past computations.
        /// </summary>
        public class SchedulerEntry
        {
            public EntityId ComputationId { get; set; }
            public DateTime Timestamp { get; set; }
            public EntityId OwnerId { get; set; }
            public ComputationPriority Priority { get; set; }
            public string Name { get; set; }
            public EntityId ServiceId { get; set; }
        }

        /// <summary>
        /// View of the current scheduler state.
        /// </summary>
        public class View
        {
            public IList<SchedulerEntry> Scheduled { get; set; }
            public IList<SchedulerEntry> Pending { get; set; }
            public IList<SchedulerEntry> Running { get; set; }
            public IList<SchedulerEntry> History { get; set; }
        }

        string GetScheduledPath() { return Path.Combine(Id.GetEntityPath(), "scheduled.json"); }
        string GetPendingPath() { return Path.Combine(Id.GetEntityPath(), "pending.json"); }
        string GetRunningPath() { return Path.Combine(Id.GetEntityPath(), "running.json"); }
        string GetHistoryPath() { return Path.Combine(Id.GetEntityPath(), "history.json"); }

        Dictionary<EntityId, SchedulerEntry> GetScheduledEntries() { return JsonHelper.ReadJsonFile<Dictionary<EntityId, SchedulerEntry>>(GetScheduledPath()); }
        Dictionary<EntityId, SchedulerEntry> GetPendingEntries() { return JsonHelper.ReadJsonFile<Dictionary<EntityId, SchedulerEntry>>(GetPendingPath()); }
        Dictionary<EntityId, SchedulerEntry> GetRunningEntries() { return JsonHelper.ReadJsonFile<Dictionary<EntityId, SchedulerEntry>>(GetRunningPath()); }
        Dictionary<EntityId, SchedulerEntry> GetHistoryEntries() { return JsonHelper.ReadJsonFile<Dictionary<EntityId, SchedulerEntry>>(GetHistoryPath()); }

        public View GetView()
        {
            return Locked(() => new View
            {
                Scheduled = GetScheduledEntries().Values.ToArray(),
                Pending = GetPendingEntries().Values.ToArray(),
                Running = GetRunningEntries().Values.ToArray(),
                History = GetHistoryEntries().Values.ToArray(),
            });
        }


        /// <summary>
        /// Throws if the computation could not be or was already scheduled.
        /// </summary>
        /// <param name="computation"></param>
        public void Schedule(ComputationInfo computation)
        {
            var status = computation.GetJsonStatus();
            SchedulerEntry entry;
            if (status.State == ComputationState.New)
            {
                entry = new SchedulerEntry
                {
                    ComputationId = computation.Id,
                    Timestamp = DateTimeService.GetCurrentTime(),
                    OwnerId = computation.UserId,
                    Priority = computation.Priority,
                    Name = computation.Name,
                    ServiceId = computation.ServiceId
                };

                status.State = ComputationState.Scheduled;
                status.ScheduledTime = entry.Timestamp;                    
                status.EnsureSave();
            }
            else
            {
                throw new InvalidOperationException("Only new computations can be scheduled.");
            }

            Locked(() =>
            {
                var entries = GetScheduledEntries();
                entries.Add(entry.ComputationId, entry);
                JsonHelper.WriteJsonFile(GetScheduledPath(), entries);
            });
        }

        void Run(ComputationInfo computation)
        {
            string args = string.Format("--hosted \"{0} \" \"{1} \"", ServerManager.ConfigurationPath, computation.Id.ToString());
            var svc = computation.GetService();
            var p =
                Process.Start(new ProcessStartInfo(svc.GetExecutablePath(), args)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

            var info = new ComputationProcessInfo { ProcessId = p.Id, StartTimeUtc = p.StartTime.ToUniversalTime() };
            JsonHelper.WriteJsonFile(computation.GetProcessInfoPath(), info);
        }

        int GetComputationLimit(EntityId userId)
        {
            var user = UserInfo.TryLoad(userId);
            if (user == null) return DefaultMaxComputationsPerUser;
            var profile = user.GetProfile();
            return profile.ConcurrentComputationLimit;
        }

        /// <summary>
        /// Update the scheduler status and run pending computations
        /// </summary>
        public void Update()
        {
            // load scheduled
            // load/sync running and check if the queue is full
            //   => add to pending if full
            //   => run otherwise
            // add new computations to history
            
            Locked(() =>
            {   
                var scheduled = GetScheduledEntries();
                var running = GetRunningEntries();
                var history = GetHistoryEntries();

                bool scheduledChanged = false;
                bool runningChanged = false;

                Dictionary<EntityId, int> computationCountByUser = new Dictionary<EntityId, int>();

                foreach (var comp in running.Keys.ToArray())
                {
                    var info = ComputationInfo.TryLoad(comp);
                    if (info == null || !info.IsRunning())
                    {
                        runningChanged = true;
                        running.Remove(comp);
                    }
                    else
                    {
                        int count = 0;
                        computationCountByUser.TryGetValue(info.UserId, out count);
                        computationCountByUser[info.UserId] = count + 1;
                    }
                }

                foreach (var comp in scheduled.ToArray())
                {
                    scheduledChanged = true;
                    runningChanged = true;
                    scheduled.Remove(comp.Key);
                    
                    var info = ComputationInfo.TryLoad(comp.Key);
                    if (info == null)
                    {
                        continue;
                    }

                    int count = 0;
                    computationCountByUser.TryGetValue(info.UserId, out count);
                    var limit = GetComputationLimit(info.UserId);
                    if (count >= limit)
                    {
                        var status = info.GetJsonStatus();
                        status.Message = string.Format("Concurrent computation limit ({0}) reached. Please try again later.", limit);
                        status.State = ComputationState.New;
                        status.EnsureSave();
                        continue;
                    }

                    if (!info.IsRunning())
                    {
                        var status = info.GetJsonStatus();
                        status.Message = "Computation scheduled.";
                        status.State = ComputationState.Scheduled;
                        status.EnsureSave();

                        Run(info);
                        running[info.Id] = comp.Value;
                        history[info.Id] = comp.Value;
                    }
                }

                if (scheduledChanged)
                {
                    JsonHelper.WriteJsonFile(GetScheduledPath(), scheduled);
                    JsonHelper.WriteJsonFile(GetHistoryPath(), history);
                }
                if (runningChanged)
                {
                    JsonHelper.WriteJsonFile(GetRunningPath(), running);
                }
            });
        }

        /// <summary>
        /// Attempts to terminate the computation and set it's status to "Terminated"
        /// </summary>
        /// <param name="computation"></param>
        /// <param name="waitForExit"></param>
        public void Kill(ComputationInfo computation, bool waitForExit = false)
        {
            if (!computation.IsRunning()) return;

            var pinfoFile = computation.GetProcessInfoPath();
            if (File.Exists(pinfoFile))
            {
                var pinfo = computation.GetProcessInfo();
                var killed = false;
                try
                {
                    var p = Process.GetProcessById(pinfo.ProcessId);
                    // check if we are killing what we started
                    if (p.StartTime.ToUniversalTime().Equals(pinfo.StartTimeUtc))
                    {
                        p.Kill();
                        killed = true;
                        if (waitForExit) p.WaitForExit(5000);
                    }
                }
                catch { }

                if (File.Exists(pinfoFile)) File.Delete(pinfoFile);

                if (killed)
                {
                    var status = computation.GetJsonStatus();
                    status.State = ComputationState.Terminated;
                    status.Message = "The computation was terminated by the user.";
                    status.FinishedTime = DateTimeService.GetCurrentTime();
                    status.Save();

                    ComputationFinished(computation);
                }
            }
        }


        /// <summary>
        /// Cleanup after the computation has finished.
        /// </summary>
        /// <param name="computation"></param>
        public void ComputationFinished(ComputationInfo computation)
        {
            Locked(() =>
            {
                var running = GetRunningEntries();
                if (running.Remove(computation.Id)) JsonHelper.WriteJsonFile(GetRunningPath(), running);
            });
        }
        
        /// <summary>
        /// Opens the scheduler.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ComputationScheduler Open(EntityId id)
        {
            var ret = TryLoad(id);
            if (ret == null) ret = CreateAndSave(id, _ => { });
            return ret;
        }
    }
}
