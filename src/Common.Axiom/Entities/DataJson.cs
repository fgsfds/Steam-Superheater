using System.Text.Json.Serialization;

namespace Common.Axiom.Entities;

public sealed class DataJson
{
    public const string UploadFolder = "UploadFolder";
}


[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class DataJsonModelContext : JsonSerializerContext;
