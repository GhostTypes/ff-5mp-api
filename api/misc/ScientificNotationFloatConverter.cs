using System;
using Newtonsoft.Json;

namespace FiveMApi.api.misc
{
    public class ScientificNotationFloatConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is float floatValue) writer.WriteRawValue(floatValue == 0f ? "0E0" : $"{floatValue:E1}");
            else throw new JsonSerializationException("Expected float value.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Convert.ToSingle(reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(float);
        }
    }
}