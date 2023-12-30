using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Config
{
    [JsonSourceGenerationOptions(Converters = [typeof(JsonStringEnumConverter<ThemeEnum>)])]
    [JsonSerializable(typeof(ConfigEntity))]
    public sealed partial class ConfigEntityContext : JsonSerializerContext { }
}
