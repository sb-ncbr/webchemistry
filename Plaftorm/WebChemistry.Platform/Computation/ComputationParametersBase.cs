using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace WebChemistry.Platform.Computation
{
    /// <summary>
    /// Base class for computation parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ComputationParametersBase<T>
        where T : ComputationParametersBase<T>, new()
    {
        string filename;
        
        /// <summary>
        /// Save the object.
        /// </summary>
        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(filename, json);
        }

        /// <summary>
        /// Open new.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static T Open(string filename)
        {
            if (File.Exists(filename))
            {
                var ret = JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
                ret.filename = filename;
                return ret;
            }
            else
            {
                var ret = new T();
                File.WriteAllText(filename, JsonConvert.SerializeObject(ret));
                ret.filename = filename;
                return ret;
            }
        }
    }
}
