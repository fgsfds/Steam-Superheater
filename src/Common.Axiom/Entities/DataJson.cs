using System.Text.Json.Serialization;

namespace Common.Axiom.Entities;

/// <summary>
/// Keys of the values stored in data.json.
/// </summary>
public sealed class DataJson
{
    /// <summary>
    /// Name of the upload folder.
    /// </summary>
    public const string UploadFolder = "UploadFolder";

    /// <summary>
    /// Name of the S3 endpoint.
    /// </summary>
    public const string S3Endpoint = "S3Endpoint";

    /// <summary>
    /// Name of the S3 bucket.
    /// </summary>
    public const string S3Bucket = "S3Bucket";

    /// <summary>
    /// Name of the S3 subfolder.
    /// </summary>
    public const string S3SubFolder = "S3SubFolder";
}


[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class DataJsonModelContext : JsonSerializerContext;
