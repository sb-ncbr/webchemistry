namespace WebChemistry.Platform
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using WebChemistry.Platform.Server;

    /// <summary>
    /// EntityId Json converter
    /// </summary>
    public class EntityIdConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;
            return EntityId.Parse(reader.Value as string);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var id = (EntityId)value;
            writer.WriteValue(id.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Entity Id type converter.
    /// </summary>
    public class EntityIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(EntityId)) return true;
            return base.CanConvertFrom(context, sourceType);
        } 

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
 	        if (destinationType == typeof(string) || destinationType == typeof(EntityId)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value == null) return null;
            if (value is string) return EntityId.Parse(value as string);
            if (value is EntityId) return (EntityId)value;
 	        return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string)) return value.ToString();
            if (destinationType == typeof(EntityId)) return value;
 	        return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Entity identifier.
    /// </summary>
    [JsonConverter(typeof(EntityIdConverter))]
    [TypeConverter(typeof(EntityIdTypeConverter))]
    public struct EntityId : IEquatable<EntityId>
    {
        /// <summary>
        /// Child separator.
        /// </summary>
        public const char ChildSeparator = '/';

        /// <summary>
        /// Check if the char is a level EntityId char.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsLegalChar(char c)
        {
            if (!(char.IsLetterOrDigit(c) || c == ChildSeparator || c == '-' || c == '_' || c == '@' || c == '.')) return false;
            return true;
        }
        
        /// <summary>
        /// Check if a string is a legal id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsLegal(string id)
        {
            for (int i = 0; i < id.Length; i++)
            {
                if (!IsLegalChar(id[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the string id a legal child Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsLegalChildId(string id)
        {
            for (int i = 0; i < id.Length; i++)
            {
                var c = id[i];
                if (c == ChildSeparator || !IsLegalChar(c)) return false;
            }

            return true;
        }

        /// <summary>
        /// Parse the ID from a string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static EntityId Parse(string value)
        {
            var colonIndex = value.IndexOf(':');
            return new EntityId(value.Substring(0, colonIndex), value.Substring(colonIndex + 1));
        }

        /// <summary>
        /// Get the entity path.
        /// </summary>
        /// <returns></returns>
        public string GetEntityPath()
        {
            return ServerManager.GetEntityPath(this);
        }

        /// <summary>
        /// Server Name.
        /// </summary>
        public readonly string ServerName; 

        /// <summary>
        /// The actual id.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Creates a child id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EntityId GetChildId(string id) 
        {
            if (!IsLegalChildId(id)) throw new ArgumentException(string.Format("'{0}' is not a legal child identifier.", id), "id");
            return new EntityId(ServerName, Value + "/" + id); 
        }

        /// <summary>
        /// return ServerName:Value;
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ServerName + ":" + Value;
        }

        /// <summary>
        /// Value hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Returns the id of the last child. For examples master:a/b => b
        /// </summary>
        /// <returns></returns>
        public string GetChildId()
        {
            var i = Value.LastIndexOf(ChildSeparator);
            if (i < 0) return Value;
            return Value.Substring(i + 1);
        }

        /// <summary>
        /// Creates a child id.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="parentId"></param>
        /// <param name="childId"></param>
        /// <returns></returns>
        public static EntityId CreateChildId(string serverName, string parentId, string childId)
        {
            if (!IsLegalChildId(childId)) throw new ArgumentException(string.Format("'{0}' is not a legal child identifier.", childId), "id");
            return new EntityId(serverName, parentId + "/" + childId);
        }

        /// <summary>
        /// Create the ID.
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="id"></param>
        public EntityId(string serverName, string id)
        {
            if (!IsLegal(id)) throw new ArgumentException(string.Format("{0} is not a legal entity id.", id));
            this.ServerName = serverName;
            this.Value = id;
        }

        /// <summary>
        /// Compares.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(EntityId other)
        {
            return other.ServerName.Equals(this.ServerName, StringComparison.Ordinal) && other.Value.Equals(this.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is EntityId) return Equals((EntityId)obj);
            return false;
        }

        /// <summary>
        /// Compares ids
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(EntityId a, EntityId b)
        {
            return a.ServerName.Equals(b.ServerName, StringComparison.Ordinal) && a.Value.Equals(b.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// compares ids
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(EntityId a, EntityId b)
        {
            return !a.Equals(b);
        }
    }
}
