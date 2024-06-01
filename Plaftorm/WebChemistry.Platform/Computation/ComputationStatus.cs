namespace WebChemistry.Platform.Computation
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Possible computation states.  
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ComputationState
    {
        /// <summary>
        /// The computation has been created but not yet started.
        /// </summary>
        New = 0,

        /// <summary>
        /// The computation was scheduled and its fate was not determined yet.
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// The computation was scheduled but could not be executed yet.
        /// </summary>
        Pending = 2,

        /// <summary>
        /// The computation is running.
        /// </summary>
        Running = 3,

        /// <summary>
        /// The computation has sucessfully completed.
        /// </summary>
        Success = 4,

        /// <summary>
        /// The computation has failed. Either could not create the process or the process
        /// terminated before it could set its state to Finished.
        /// </summary>
        Failed = 5,

        /// <summary>
        /// The computation process has been terminated by the user.
        /// </summary>
        Terminated = 6
    }

    /// <summary>
    /// Computation status object.
    /// </summary>
    public class ComputationStatus
    {
        /// <summary>
        /// The version of the service the computation is executed by.
        /// </summary>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// State of the computation.
        /// </summary>
        public ComputationState State { get; set; }

        /// <summary>
        /// Message. Could contain error message or whatever.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// (JSON serialized) custom computation state. Ie. number of found motives etc.
        /// </summary>
        public string CustomState { get; set; }

        /// <summary>
        /// Is the computation progress indeterminate?
        /// </summary>
        public bool IsIndeterminate { get; set; }

        /// <summary>
        /// Current computation progress.
        /// </summary>
        public int CurrentProgress { get; set; }

        /// <summary>
        /// Max computation progress.
        /// </summary>
        public int MaxProgress { get; set; }

        /// <summary>
        /// When the computation was scheduled (UTC).
        /// </summary>
        public DateTime? ScheduledTime { get; set; }

        /// <summary>
        /// When the computation started (UTC).
        /// </summary>
        public DateTime? StartedTime { get; set; }

        /// <summary>
        /// When the computation finished (UTC).
        /// </summary>
        public DateTime? FinishedTime { get; set; }

        /// <summary>
        /// Size of the result in bytes.
        /// </summary>
        public long ResultSizeInBytes { get; set; }
    }
    
    /// <summary>
    /// A "helper" class for the computation status.
    /// Mainly useful for the client implementations.
    /// </summary>
    public class JsonComputationStatus : ComputationStatus
    {
        string filename;
        
        /// <summary>
        /// Save the status object.
        /// </summary>
        /// <param name="prettyPrint"></param>
        public void Save(bool prettyPrint = true)
        {
            var json = JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);

            var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(json);
                sw.Flush();
            }
        }

        /// <summary>
        /// Save the status object.
        /// </summary>
        /// <param name="prettyPrint"></param>
        public bool TrySave(bool prettyPrint = true)
        {
            var json = JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);

            try
            {
                var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(json);
                    sw.Flush();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Save the status object. Repeats until the file is really written.
        /// </summary>
        /// <param name="prettyPrint"></param>
        public void EnsureSave(bool prettyPrint = true)
        {
            var json = JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);

            while (true)
            {
                try
                {
                    var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.Write(json);
                        sw.Flush();
                    }
                    break;
                }
                catch 
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }


        /// <summary>
        /// Opens a status object. If the file does not exist, it is created.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static JsonComputationStatus Open(string filename)
        {
            if (File.Exists(filename))
            {
                var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var sr = new StreamReader(fs))
                {
                    var ret = JsonConvert.DeserializeObject<JsonComputationStatus>(sr.ReadToEnd());
                    ret.filename = filename;
                    return ret;
                }
            }
            else
            {
                var ret = new JsonComputationStatus();
                var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(JsonConvert.SerializeObject(ret, Formatting.Indented));
                    sw.Flush();
                }
                ret.filename = filename;
                return ret;
            }
        }
        
        private JsonComputationStatus()
        {

        }
    }

    /// <summary>
    /// Helper class for the status.
    /// </summary>
    public static class ComputationStatusHelper
    {
        /// <summary>
        /// Deserialize the custom state.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="status"></param>
        /// <returns></returns>
        public static TState GetJsonCustomState<TState>(this ComputationStatus status)
            where TState : new()
        {
            if (status.CustomState == null) return new TState();
            return JsonConvert.DeserializeObject<TState>(status.CustomState);
        }

        /// <summary>
        /// Serialize the custom state as JSON.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="status"></param>
        /// <param name="value"></param>
        public static void SetJsonCustomState<TState>(this ComputationStatus status, TState value)
            where TState : new()
        {
            status.CustomState = JsonConvert.SerializeObject(value);
        }
    }
}
