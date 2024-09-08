using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Common.Helpers;

namespace Web.Blazor.Helpers;

public sealed class S3Client : IDisposable
{
    private readonly string _key = Environment.GetEnvironmentVariable("S3Key")!;
    private readonly string _secretKey = Environment.GetEnvironmentVariable("S3SKey")!;

    private readonly AmazonS3Client _s3client;

    public S3Client()
    {
        AmazonS3Config config = new()
        {
            ServiceURL = string.Format("https://s3.timeweb.cloud"),
            UseHttp = false
        };

        AWSCredentials creds = new BasicAWSCredentials(_key, _secretKey);
        _s3client = new(creds, config);
    }

    /// <summary>
    /// Get signed URL for file uploading
    /// </summary>
    /// <param name="file">File name</param>
    /// <returns>Signed URL</returns>
    public string? GetSignedUrl(string file)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = Consts.Bucket,
            Key = file,
            Expires = DateTime.Now.AddHours(1),
            Verb = HttpVerb.PUT
        };

        string? url;

        try
        {
            url = _s3client.GetPreSignedURL(request);
        }
        catch
        {
            return null;
        }

        return url;
    }

    public void Dispose() => _s3client?.Dispose();
}

