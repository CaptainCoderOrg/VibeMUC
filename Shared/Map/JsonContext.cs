using System.Text.Json.Serialization;

namespace VibeMUC.Map
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(DungeonMapData))]
    [JsonSerializable(typeof(CellData))]
    public partial class JsonContext : JsonSerializerContext
    {
    }
} 