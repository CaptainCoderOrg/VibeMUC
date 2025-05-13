using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VibeMUC.Map
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.None,
            Converters = new JsonConverter[]
            {
                new StringEnumConverter()
            }
        };

        public static string ToJson<T>(this T obj) where T : class
        {
            return JsonConvert.SerializeObject(obj, Settings);
        }

        public static T? FromJson<T>(this string json) where T : class
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
    }
} 