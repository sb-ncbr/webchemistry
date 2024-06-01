namespace WebChemistry.Platform
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using System.Collections;
    using ICSharpCode.SharpZipLib.GZip;
    using ICSharpCode.SharpZipLib.Zip;
    using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

    /// <summary>
    /// Type of JSON compression.
    /// </summary>
    public enum JsonCompressionType
    {
        GZip,
        Zip
    }

    /// <summary>
    /// Helper methods for json objects.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Creates object from a JSON string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T FromJsonString<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
                
        /// <summary>
        /// Reads a json object from a file.
        /// If the file does not exist, an empty object is parsed instead.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="filename"></param>
        /// <param name="createEmptyObjectIfFileDoesNotExist"></param>
        /// <param name="isArray"></param>
        /// <returns></returns>
        public static TResult ReadJsonFile<TResult>(string filename, bool createEmptyObjectIfFileDoesNotExist = true)
            where TResult : class
        {
            if (File.Exists(filename))
            {
                var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var sr = new StreamReader(fs))
                {
                    var ret = JsonConvert.DeserializeObject<TResult>(sr.ReadToEnd());
                    return ret;
                }
            }

            if (createEmptyObjectIfFileDoesNotExist)
            {
                return JsonConvert.DeserializeObject<TResult>("{}");
            }
            else throw new FileLoadException(string.Format("File '{0}' does not exist.", filename));
        }

        /// <summary>
        /// Writes an object to a json file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <param name="obj"></param>
        /// <param name="prettyPrint"></param>
        public static void WriteJsonFile<T>(string filename, T obj, bool prettyPrint = true)
        {
            var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None));
                sw.Flush();
            }
        }

        /// <summary>
        /// Write object into a compressed file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <param name="obj"></param>
        /// <param name="prettyPrint"></param>
        /// <param name="compressionType"></param>
        public static void WriteCompressed<T>(string filename, T obj, bool prettyPrint = true, JsonCompressionType compressionType = JsonCompressionType.GZip)
        {
            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            using (DeflaterOutputStream compressed = compressionType == JsonCompressionType.GZip ? (DeflaterOutputStream)new GZipOutputStream(fs) : (DeflaterOutputStream)new ZipOutputStream(fs))
            using (var writer = new StreamWriter(compressed))
            {
                writer.Write(JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None));
                writer.Flush();
            }
        }

        /// <summary>
        /// Read a compressed JSON file.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="filename"></param>
        /// <param name="compressionType"></param>
        /// <returns></returns>
        public static TResult ReadCompressed<TResult>(string filename, JsonCompressionType compressionType = JsonCompressionType.GZip)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
            using (InflaterInputStream compressed = compressionType == JsonCompressionType.GZip ? (InflaterInputStream)new GZipInputStream(fs) : (InflaterInputStream)new ZipInputStream(fs))
            using (var reader = new StreamReader(compressed))
            {
                return JsonConvert.DeserializeObject<TResult>(reader.ReadToEnd());
            }
        }
    }
}
