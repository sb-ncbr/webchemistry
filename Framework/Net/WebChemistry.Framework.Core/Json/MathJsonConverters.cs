namespace WebChemistry.Framework.Core.Json
{
    using Newtonsoft.Json;
    using System;
    using WebChemistry.Framework.Math;

    /// <summary>
    /// Vector3D JSON converter
    /// </summary>
    public class Vector3DJsonConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            double x, y, z;
            reader.Read();
            x = (double)reader.Value;
            reader.Read();
            y = (double)reader.Value;
            reader.Read();
            z = (double)reader.Value;
            reader.Read();
            return new Vector3D(x, y, z);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector3D)value;
            writer.WriteStartArray();
            writer.WriteValue(v.X);
            writer.WriteValue(v.Y);
            writer.WriteValue(v.Z);
            writer.WriteEndArray();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}
