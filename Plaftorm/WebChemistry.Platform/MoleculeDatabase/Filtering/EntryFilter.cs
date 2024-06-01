namespace WebChemistry.Platform.MoleculeDatabase.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Property type.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FilterPropertyType
    {
        Int,
        Double,
        Date,
        String,
        StringArray
    }

    /// <summary>
    /// Type of comparison to perform.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FilterComparisonType
    {
        NumberEqual,
        NumberLess,
        NumberLessEqual,
        NumberGreater,
        NumberGreaterEqual,

        StringEqual,
        StringContainsWord,
        StringRegex
    }

    /// <summary>
    /// Uses string filters to filter entries.
    /// </summary>
    public class EntryFilter
    {        
        #region Filters
        static bool ContainsWord(string[] xs, string word)
        {
            for (int j = 0; j < xs.Length; j++)
            {
                var p = xs[j];
                var o = p.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                if (o < 0) continue;

                if ((p.Length <= o + word.Length || !char.IsLetterOrDigit(p[o + word.Length]))
                    && (o == 0 || !char.IsLetterOrDigit(p[o - 1])))
                {
                    return true;
                }
            }
            return false;
        }

        static bool RegexMatch(string[] xs, Dictionary<string,Regex> cache, string word)
        {
            Regex regex;
            if (!cache.TryGetValue(word, out regex))
            {
                regex = new Regex(word, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                cache.Add(word, regex);                
            }

            for (int j = 0; j < xs.Length; j++)
            {
                var p = xs[j];
                if (regex.IsMatch(p)) return true;
            }

            return false;
        }

        static CultureInfo InvCulture = CultureInfo.InvariantCulture;
        static DateTime? ParseDate(string value)
        {
            if (value == null) return null;
            DateTime date;
            if (DateTime.TryParseExact(value, "yyyy-M-d", InvCulture, System.Globalization.DateTimeStyles.None, out date)) return date;
            return null;
        }

        bool CompareInt(DatabaseIndexEntry e, Func<int, int, bool> comp)
        {
            var prop = e.GetInt(PropertyName);
            if (prop == null) return false;
            return comp(prop.Value, IntValue);
        }

        bool CompareDouble(DatabaseIndexEntry e, Func<double, double, bool> comp)
        {
            var prop = e.GetDouble(PropertyName);
            if (prop == null) return false;
            return comp(prop.Value, DoubleValue);
        }

        bool CompareDate(DatabaseIndexEntry e, Func<DateTime, DateTime, bool> comp)
        {
            var prop = ParseDate(e.GetString(PropertyName));
            if (prop == null) return false;
            return comp(prop.Value, DateValue);
        }

        Func<DatabaseIndexEntry, bool> MakeNumberCondition()
        {
            Func<int, int, bool> intComp = null;
            Func<double, double, bool> doubleComp = null;
            Func<DateTime, DateTime, bool> dateComp = null;

            switch (ComparisonType)
            {
                case FilterComparisonType.NumberEqual:
                    if (PropertyType == FilterPropertyType.Int) intComp = (a, b) => a == b;
                    else if (PropertyType == FilterPropertyType.Date) dateComp = (a, b) => a == b;
                    else doubleComp = (a, b) => Math.Abs(a - b) < 1e-8;
                    break;
                case FilterComparisonType.NumberLess:
                    if (PropertyType == FilterPropertyType.Int) intComp = (a, b) => a < b;
                    else if (PropertyType == FilterPropertyType.Date) dateComp = (a, b) => a < b;
                    else doubleComp = (a, b) => a < b;
                    break;
                case FilterComparisonType.NumberLessEqual:
                    if (PropertyType == FilterPropertyType.Int) intComp = (a, b) => a <= b;
                    else if (PropertyType == FilterPropertyType.Date) dateComp = (a, b) => a <= b;
                    else doubleComp = (a, b) => a <= b;
                    break;
                case FilterComparisonType.NumberGreater:
                    if (PropertyType == FilterPropertyType.Int) intComp = (a, b) => a > b;
                    else if (PropertyType == FilterPropertyType.Date) dateComp = (a, b) => a > b;
                    else doubleComp = (a, b) => a > b;
                    break;
                case FilterComparisonType.NumberGreaterEqual:
                    if (PropertyType == FilterPropertyType.Int) intComp = (a, b) => a >= b;
                    else if (PropertyType == FilterPropertyType.Date) dateComp = (a, b) => a >= b;
                    else doubleComp = (a, b) => a > b;
                    break;

                default:
                    throw new NotSupportedException(string.Format("{0} comparison is not supported for type {1}.", ComparisonType, PropertyType));
            }

            if (PropertyType == FilterPropertyType.Int)
            {
                var comp = intComp;
                return e => CompareInt(e, comp);
            }
            else if (PropertyType == FilterPropertyType.Date)
            {
                var comp = dateComp;
                return e => CompareDate(e, comp);
            }
            else
            {
                var comp = doubleComp;
                return e => CompareDouble(e, comp);
            }
        }

        Func<DatabaseIndexEntry, Func<string, bool>> MakeStringCondition()
        {
            switch (ComparisonType)
            {
                case FilterComparisonType.StringEqual:
                    return entry =>
                    {
                        var xs = entry.GetStringArray(PropertyName);
                        if (xs == null) return _ => false;
                        var set = xs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        return s => set.Contains(s);
                    };
                case FilterComparisonType.StringContainsWord:
                    return entry =>
                    {
                        var xs = entry.GetStringArray(PropertyName);
                        if (xs == null) return _ => false;
                        return s => ContainsWord(xs, s);
                    };
                case FilterComparisonType.StringRegex:
                    {
                        var values = Filter.GetUniqueElementValues();
                        var regexCache = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

                        foreach (var v in values)
                        {
                            try
                            {
                                regexCache[v] = new Regex(v, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                            }
                            catch (Exception e)
                            {
                                throw new InvalidOperationException(string.Format("'{0}' is not a valid regular expression: {1}", v, e.Message));
                            }
                        }

                        return entry =>
                        {
                            var cache = regexCache;
                            var xs = entry.GetStringArray(PropertyName);
                            if (xs == null) return _ => false;
                            return s => RegexMatch(xs, cache, s);
                        };
                    }
                default:
                    throw new NotSupportedException(string.Format("{0} comparison is not supported for type {1}.", ComparisonType, PropertyType));
            }
        }
        #endregion

        /// <summary>
        /// Name of the property to filter.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Filter type.
        /// </summary>
        public FilterPropertyType PropertyType { get; set; }

        /// <summary>
        /// Type of the comparison.
        /// </summary>
        public FilterComparisonType ComparisonType { get; set; }

        /// <summary>
        /// Value.
        /// </summary>
        public string Value { get; set; }

        bool Initialized;
        int IntValue;
        double DoubleValue;
        DateTime DateValue;
        StringFilter Filter;
        Func<DatabaseIndexEntry, Func<string, bool>> StringCondition;
        Func<DatabaseIndexEntry, bool> PassFunc;
               
        void Init()
        {
            if (Initialized) return;

            switch (PropertyType)
            {
                case FilterPropertyType.Int: 
                    IntValue = int.Parse(Value);
                    PassFunc = MakeNumberCondition();
                    break;
                case FilterPropertyType.Double: 
                    DoubleValue = double.Parse(Value, CultureInfo.InvariantCulture);
                    PassFunc = MakeNumberCondition();
                    break;
                case FilterPropertyType.Date:
                    var date = ParseDate(Value);
                    if (!date.HasValue) throw new ArgumentException(string.Format("'{0}' is not a valid date format. The expected format is yyyy-m-d, for example 2007-5-27.", Value));
                    DateValue = date.Value;
                    PassFunc = MakeNumberCondition();
                    break;
                default:
                    Filter = new StringFilter(Value, ComparisonType == FilterComparisonType.StringRegex);
                    StringCondition = MakeStringCondition();
                    PassFunc = e => Filter.Passes(StringCondition(e));
                    break;
            }

            Initialized = true;
        }

        /// <summary>
        /// Check if the filter is valid.
        /// </summary>
        public string CheckError()
        {
            try
            {
                Init();
                return null;
            } 
            catch (Exception e)
            {
                return e.Message;
            }
        }

        /// <summary>
        /// Throws if the entry is not valid.
        /// </summary>
        /// <returns></returns>
        public void CheckValid()
        {
            Init();
        }

        /// <summary>
        /// Check if the entry passes.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool Passes(DatabaseIndexEntry entry)
        {
            Init();
            return PassFunc(entry);                        
        }

        /// <summary>
        /// Creates the filter. If it's invalid, throws.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <param name="comparisonType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static EntryFilter Create(string propertyName, FilterPropertyType propertyType, FilterComparisonType comparisonType, string value)
        {
            var filter = new EntryFilter
            {
                PropertyName = propertyName,
                PropertyType = propertyType,
                ComparisonType = comparisonType,
                Value = value
            };

            filter.CheckValid();

            return filter;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as EntryFilter;
            if (other == null) return false;
            return other.PropertyName == this.PropertyName
                && other.Value.Equals(this.Value, StringComparison.Ordinal);
        }
    }
}
