namespace WebChemistry.Framework.Core
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.Text.RegularExpressions;

    /// <summary>
    /// EntityId Json converter
    /// </summary>
    public class AppVersionConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {            
            return AppVersion.Parse(reader.Value as string);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var version = (AppVersion)value;
            writer.WriteValue(version.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Entity Id type converter.
    /// </summary>
    public class AppVersionTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(AppVersion)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string) || destinationType == typeof(AppVersion)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string) return AppVersion.Parse(value as string);
            if (value is AppVersion) return (AppVersion)value;
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string)) return value.ToString();
            if (destinationType == typeof(AppVersion)) return value;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Handles the application version.
    /// </summary>
    [JsonConverter(typeof(AppVersionConverter))]
    [TypeConverter(typeof(AppVersionTypeConverter))]
    public class AppVersion : IEquatable<AppVersion>, IComparable<AppVersion>
    {
        /// <summary>
        /// Unknown;
        /// </summary>
        public static readonly AppVersion Unknown = new AppVersion(0, 0, 0, 0, 0);

        static Func<AppVersion, int>[] Fields = new Func<AppVersion, int>[]
        {
            x => x.Major, x => x.Minor, x => x.Year, x => x.Month, x => x.Day, x => (int)x.Revision
        };

        /// <summary>
        /// Major version.
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// Minor version.
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// Year of creation.
        /// </summary>
        public int Year { get; private set; }

        /// <summary>
        /// Month of creation.
        /// </summary>
        public int Month { get; private set; }

        /// <summary>
        /// Day of creation.
        /// </summary>
        public int Day { get; private set; }

        /// <summary>
        /// More versions per day?
        /// </summary>
        public char Revision { get; private set; }

        /// <summary>
        /// Returns text in the format Major.Minor.Year.Month.Day[Revision]
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this == Unknown) return "Unknown";
            return string.Format("{0}.{1}.{2}.{3}.{4}{5}", Major, Minor, Year, Month, Day, Revision == ' ' ? "" : Revision.ToString());
        }

        /// <summary>
        /// Compute hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 23;
            for (int i = 0; i < Fields.Length; i++)
            {
                hash = 31 * hash + Fields[i](this);
            }            
            return hash;
        }

        /// <summary>
        /// Check if the versions are equal.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(AppVersion other)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                var f = Fields[i];
                int a = f(this), b = f(other);
                if (a != b) return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the versions are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as AppVersion;
            return other != null ? Equals(other) : false;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(AppVersion a, AppVersion b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        /// <summary>
        /// Non-equality operator.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(AppVersion a, AppVersion b)
        {
            return !(a == b);
        }
        
        /// <summary>
        /// Compares two versions.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(AppVersion other)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                var f = Fields[i];
                int a = f(this), b = f(other);
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }

        /// <summary>
        /// Creates a new version.
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="revision"></param>
        public AppVersion(int major, int minor, int year, int month, int day, char revision = ' ')
        {
            if (!char.IsLetter(revision) && revision != ' ') throw new ArgumentException("Revision must be a better", "revision");
            Major = major;
            Minor = minor;
            Year = year;
            Month = month;
            Day = day;
            Revision = revision;
        }

        static Regex Parser = new Regex(@"(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<year>[0-9]+)\.(?<month>[0-9]+)\.(?<day>[0-9]+)(?<rev>[a-zA-Z]{0,1})");

        /// <summary>
        /// Parses a version string.
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        public static AppVersion Parse(string versionString)
        {
            versionString = versionString ?? "";
            if (versionString.EqualOrdinalIgnoreCase("Unknown")) return Unknown;
            var match = Parser.Match(versionString);
            if (string.IsNullOrEmpty(versionString) || !match.Success)
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid version string.", versionString), "versionString");
            }
            char rev = ' ';
            if (match.Groups["rev"].Success && match.Groups["rev"].Value.Length == 1) rev = match.Groups["rev"].Value[0];
            return new AppVersion(
                int.Parse(match.Groups["major"].Value),
                int.Parse(match.Groups["minor"].Value),
                int.Parse(match.Groups["year"].Value),
                int.Parse(match.Groups["month"].Value),
                int.Parse(match.Groups["day"].Value),
                rev);
        }

        /// <summary>
        /// Implicit to string conversion.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static implicit operator string(AppVersion version)
        {
            return version.ToString();
        }
    }
}
