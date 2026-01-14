namespace Common.Axiom.Helpers;

public static class CommonConstants
{
    public const string S3Endpoint = "https://s3.firstvds.ru/";

    /// <summary>
    /// Path to the files repository
    /// </summary>
    public const string BucketAddress = $"{S3Endpoint}superheater/";

    /// <summary>
    /// Path to the uploads repository
    /// </summary>
    public const string UploadsFolder = $"{S3Endpoint}uploads/superheater/";

    public const string FixesJsonUrl = "https://raw.githubusercontent.com/fgsfds/Steam-Superheater/refs/heads/master/db/fixes.json";

    public const string NewsJsonUrl = "https://raw.githubusercontent.com/fgsfds/Steam-Superheater/refs/heads/master/db/news.json";
}
