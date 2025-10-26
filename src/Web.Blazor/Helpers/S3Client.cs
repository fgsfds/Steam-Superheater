namespace Web.Blazor.Helpers;

public sealed class S3Client : IDisposable
{
    private readonly string _key = Environment.GetEnvironmentVariable("S3Key")!;
    private readonly string _secretKey = Environment.GetEnvironmentVariable("S3SKey")!;

    /// <summary>
    /// Get signed URL for file uploading
    /// </summary>
    /// <param name="file">File name</param>
    /// <returns>Signed URL</returns>
    public string? GetSignedUrl(string file)
    {
        return null;
    }

    public void Dispose() { }
}

