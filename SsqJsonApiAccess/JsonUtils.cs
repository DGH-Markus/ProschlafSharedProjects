using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  SqsJsonApiAccess.Utils
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
}
