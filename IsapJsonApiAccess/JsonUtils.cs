using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsapJsonApiAccess.Utils
{
    /// <summary>
    /// This class implements a custom JSON converter that can convert an input parent object to certain supported child objects.
    /// </summary>
    /// <typeparam name="TFromType">The parent object that has to be converted into one of its child classes.</typeparam>
    /// <typeparam name="TDynamic">The requried child object type.</typeparam>
    public class JsonTypeMapper<TFromType, TDynamic> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(TFromType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TDynamic>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public static class JsonSerialization
    {
        /// <summary>
        /// Serializes the provided object to JSON into the specified string.
        /// Make sure that the provided object is marked as 'Serializable'.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="json"></param>
        /// <returns>NULL if successful.</returns>
        public static Exception SerializeObject(object obj, out string json)
        {
            try
            {
                json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                return null;
            }
            catch (Exception ex)
            {
                json = null;
                return ex;
            }
        }

        /// <summary>
        /// Deserializes the provided object into the specified result type.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="resultType"></param>
        /// <param name="result"></param>
        /// <returns>NULL if successful.</returns>
        public static Exception DeserializeObject(string json, Type resultType, out object result)
        {
            try
            {
                result = JsonConvert.DeserializeObject(json, resultType);
                return null;
            }
            catch (Exception ex)
            {
                result = null;
                return ex;
            }
        }
    }
}
