using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Splatoon.Utility
{
    // Normally, enums are serialized as their int values in JSON.
    // Dictionaries with enum keys normally serialize the enum's name as the key.
    // This means that enums can neither be renamed nor renumbered or it will break existing configs.
    // By serializing keys as string representations of ints, enums can at least be renamed without breaking configs.
    // Code from https://stackoverflow.com/a/66088256
    public class DictionaryWithEnumKeyConverter<T, U> : JsonConverter where T : Enum
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<T, U>)value;

            writer.WriteStartObject();

            foreach (KeyValuePair<T, U> pair in dictionary)
            {
                writer.WritePropertyName(Convert.ToInt32(pair.Key).ToString());
                serializer.Serialize(writer, pair.Value);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<T, U>();
            var jObject = JObject.Load(reader);

            foreach (var x in jObject)
            {
                T key = (T)(object)int.Parse(x.Key);
                U value = (U)x.Value.ToObject(typeof(U));
                result.Add(key, value);
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<T, U>) == objectType;
        }
    }
}
