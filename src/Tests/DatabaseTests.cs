using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Client;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;
using Common.Axiom.Providers;
using Common.Client;
using Common.Client.FilesTools;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Moq;
using Xunit.Abstractions;

namespace Tests;

public sealed class DatabaseTests
{
    private readonly ITestOutputHelper _output;

    public DatabaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact, Trait("Category", "Database")]
    public async Task DatabaseFilesIntegrityTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "BuildLauncher");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string fixesJsonString;

        if (File.Exists("../../../../db/fixes.json"))
        {
            fixesJsonString = File.ReadAllText("../../../../db/fixes.json");
        }
        else if (File.Exists("../../../../../db/fixes.json"))
        {
            fixesJsonString = File.ReadAllText("../../../../../db/fixes.json");
        }
        else
        {
            Assert.Fail();
            return;
        }

        var fixesJson = JsonSerializer.Deserialize(fixesJsonString, FixesListContext.Default.ListFixesList);

        Assert.NotNull(fixesJson);

        StringBuilder sbFails = new();
        StringBuilder sbSuccesses = new();

        foreach (var list in fixesJson)
        {
            foreach (var fix in list.Fixes)
            {
                if (fix is not FileFixEntity fileFix ||
                    fix.IsDisabled ||
                    fileFix.Url is null)
                {
                    continue;
                }

                if (fileFix.IsDisabled)
                {
                    _ = sbSuccesses.AppendLine($"[Info] File {fileFix.Url} is disabled.");
                    continue;
                }

                var url = fileFix.Url;
                var size = fileFix.FileSize;
                var hash = fileFix.Sha256;

                if (hash is null)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't have Hash in the database.");
                }

                if (size is null)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't have size in the database.");
                }

                if (hash is null || size is null)
                {
                    continue;
                }

                using var header = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!header.IsSuccessStatusCode)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't exist.");
                    continue;
                }

                if (header.Content.Headers.ContentLength is null)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't have size in the header.");
                }
                else if (size != header.Content.Headers.ContentLength)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} size doesn't match. Expected {size} got {header.Content.Headers.ContentLength}.");
                }

                //md5 of files from s3
                if (url.StartsWith(CommonConstants.S3Endpoint))
                {
                    var actualHash = header.Headers
                        .FirstOrDefault(x => x.Key.Equals("x-amz-meta-checksum-sha256"))
                        .Value
                        ?.FirstOrDefault();

                    if (actualHash is null)
                    {
                        _ = sbFails.AppendLine($"[Error] File {url} doesn't have Hash.");
                    }
                    else
                    {
                        if (!actualHash.Equals(hash, StringComparison.OrdinalIgnoreCase))
                        {
                            _ = sbFails.AppendLine($"[Error] File {url} has wrong Hash.");
                        }
                        else
                        {
                            _ = sbSuccesses.AppendLine($"[Info] File's {url} Hash matches: {hash}.");
                        }
                    }
                }
                else
                {
                    await using var stream = await httpClient.GetStreamAsync(url).ConfigureAwait(false);

                    var actualHash = await SHA256.HashDataAsync(stream);
                    var actualHashStr = Convert.ToHexString(actualHash);

                    if (!actualHashStr.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    {
                        _ = sbFails.AppendLine($"[Error] File {url} has wrong Sha256.");
                    }
                    else
                    {
                        _ = sbSuccesses.AppendLine($"[Info] File's {url} Sha256 matches: {hash}.");
                    }
                }
            }
        }

        _output.WriteLine(sbSuccesses.ToString());
        Assert.True(sbFails.Length < 1, sbFails.ToString());
    }


    [Fact, Trait("Category", "Database")]
    public void DatabaseNewsIntegrityTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "BuildLauncher");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string newsJsonString;

        if (File.Exists("../../../../db/news.json"))
        {
            newsJsonString = File.ReadAllText("../../../../db/news.json");
        }
        else if (File.Exists("../../../../../db/news.json"))
        {
            newsJsonString = File.ReadAllText("../../../../../db/news.json");
        }
        else
        {
            Assert.Fail();
            return;
        }

        var fixesJson = JsonSerializer.Deserialize(newsJsonString, NewsListEntityContext.Default.ListNewsEntity);

        Assert.NotNull(fixesJson);
    }



    [Fact, Trait("Category", "Database")]
    public async Task LooseFilesTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var fixesJsonString = File.ReadAllText("../../../../db/fixes.json");
        var fixesJson = JsonSerializer.Deserialize(fixesJsonString, FixesListContext.Default.ListFixesList);

        Assert.NotNull(fixesJson);

        List<string>? fixesUrls = [];

        foreach (var a in fixesJson)
        {
            foreach (var b in a.Fixes.OfType<FileFixEntity>())
            {
                if (b.Url?.StartsWith(CommonConstants.S3Endpoint) == true)
                {
                    fixesUrls.Add(b.Url);
                }
            }
        }

        var access = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secret = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        Assert.NotNull(access);
        Assert.NotNull(secret);

        using var minioClient = new MinioClient();
        using var iMinioClient = minioClient
            .WithEndpoint(CommonConstants.S3Endpoint.Split("//").Last())
            .WithCredentials(access, secret)
            .WithSSL(false)
            .Build();

        var args = new ListObjectsArgs()
            .WithBucket(CommonConstants.S3Bucket)
            .WithPrefix(prefix: CommonConstants.S3Folder + '/')
            .WithRecursive(true);

        var filesInBucket = new List<string>();

        await foreach (var item in iMinioClient.ListObjectsEnumAsync(args))
        {
            if (item.Key.EndsWith('/'))
            {
                continue;
            }

            if (item.Key.Contains("metadata"))
            {
                continue;
            }

            filesInBucket.Add($"{CommonConstants.S3Endpoint}/{CommonConstants.S3Bucket}/{item.Key}");
        }

        var loose = filesInBucket.Except(fixesUrls);

        StringBuilder sb = new();

        foreach (var item in loose)
        {
            _ = sb.AppendLine($"[Error] File {item} is loose.");
        }

        _output.WriteLine(sb.ToString());
        Assert.True(sb.Length < 1, sb.ToString());
    }



    [Fact, Trait("Category", "Database")]
    public async Task UploadFixTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ClientProperties.IsOfflineMode = true;

        using HttpClient httpClient = new();
        var logger = new Mock<ILogger>().Object;
        ProgressReport progressReport = new();
        AppReleasesProvider releasesProvider = new(logger, httpClient);
        GitHubApiInterface api = new(releasesProvider, httpClient, logger);
        FilesUploader uploader = new(logger, api, progressReport);

        await uploader.UploadFilesAsync("test", [Path.Combine("resources", "test_fix.zip")], CancellationToken.None);

        var url = $"{CommonConstants.S3Endpoint}/{CommonConstants.S3Bucket}/uploads/{CommonConstants.S3Folder}/test/test_fix.zip";

        using var resp = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        Assert.True(resp.IsSuccessStatusCode);

        var timespan = DateTime.Now - resp.Content.Headers.LastModified;
        Assert.True(timespan < TimeSpan.FromSeconds(5));
    }
}
