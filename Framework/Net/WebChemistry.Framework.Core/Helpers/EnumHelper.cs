namespace WebChemistry.Framework.Core
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// A helper class for faster enumeration operations.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public static class EnumHelper<TEnum>
    {
        static Dictionary<string, TEnum> parserDict;
        static Dictionary<TEnum, string> stringValues;

        /// <summary>
        /// Converts the value to string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToStringFast(TEnum value)
        {
            return stringValues[value];
        }

        /// <summary>
        /// Parses the value, ignores case.
        /// 
        /// Can throw if value not present.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TEnum ParseFast(string value)
        {
            TEnum ret;
            if (parserDict.TryGetValue(value, out ret)) return ret;
            throw new ArgumentException(string.Format("Cannot parse '{0}' to type '{1}'.", value, typeof(TEnum).Name));
        }

        /// <summary>
        /// Parses the value, ignores case.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static bool TryParseFast(string value, out TEnum ret)
        {
            return parserDict.TryGetValue(value, out ret);
        }
        
        static EnumHelper()
        {
            var type = typeof(TEnum);
            var names = Enum.GetNames(type);
            parserDict = names.ToDictionary(n => n, n => (TEnum)Enum.Parse(type, n, true), StringComparer.OrdinalIgnoreCase);
            stringValues = parserDict.ToDictionary(v => v.Value, v => v.Value.ToString());
        }
    }

    /// <summary>
    /// A helper class for handling enums.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Return names of the enum fields.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static string[] GetNames<TEnum>()
        {
            return Enum.GetNames(typeof(TEnum));
        }

        /// <summary>
        /// Parse an enum.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TEnum Parse<TEnum>(string value)
        {
            return EnumHelper<TEnum>.ParseFast(value);
        }

        /// <summary>
        /// Converts the enum to string using a cache.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString<TEnum>(TEnum value)
        {
            return EnumHelper<TEnum>.ToStringFast(value);
        }
    }
}
