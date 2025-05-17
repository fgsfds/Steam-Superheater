using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using Minio;
using Minio.DataModel.Args;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Tests;

public sealed class DatabaseTests
{
    private readonly ITestOutputHelper _output;

    public DatabaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
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

                var url = fileFix.Url;
                var size = fileFix.FileSize;
                var md5 = fileFix.MD5;

                if (md5 is null)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't have MD5 in the database.");
                }

                if (size is null)
                {
                    _ = sbFails.AppendLine($"[Error] File {url} doesn't have size in the database.");
                }

                if (md5 is null || size is null)
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
                if (url.StartsWith(Consts.FilesRepo))
                {
                    if (url.EndsWith("re4_re4hd_v1_1.zip"))
                    {
                        //nothing to do
                        continue;
                    }
                    else if (header.Headers.ETag?.Tag is null)
                    {
                        _ = sbFails.AppendLine($"[Error] File {url} doesn't have ETag.");
                    }
                    else
                    {
                        var md5e = header.Headers.ETag!.Tag.Replace("\"", "");

                        if (md5e.Contains('-'))
                        {
                            _ = sbFails.AppendLine($"[Error] File {url} has incorrect ETag.");
                        }
                        else if (!md5e.Equals(md5, StringComparison.OrdinalIgnoreCase))
                        {
                            _ = sbFails.AppendLine($"[Error] File {url} has wrong MD5.");
                        }
                        else
                        {
                            _ = sbSuccesses.AppendLine($"[Info] File's {url} MD5 matches: {md5}.");
                        }
                    }
                }
                //md5 of external files
                else if (header.Content.Headers.ContentMD5 is not null)
                {
                    if (!Convert.ToHexString(header.Content.Headers.ContentMD5).Equals(md5, StringComparison.OrdinalIgnoreCase))
                    {
                        _ = sbFails.AppendLine($"[Error] File {url} has wrong MD5.");
                    }
                    else
                    {
                        _ = sbSuccesses.AppendLine($"[Info] File's {url} MD5 matches: {md5}.");
                    }
                }
                else
                {
                    await using var stream = await httpClient.GetStreamAsync(url);
                    using var md5remote = MD5.Create();

                    byte[] hash = md5remote.ComputeHash(stream);

                    var filemd5 = Convert.ToHexString(hash);

                    if (!filemd5.Equals(md5, StringComparison.OrdinalIgnoreCase))
                    {
                        _ = sbFails.AppendLine($"[Error] File {url} has wrong MD5.");
                    }
                    else
                    {
                        _ = sbSuccesses.AppendLine($"[Info] File's {url} MD5 matches: {md5}.");
                    }
                }
            }
        }

        _output.WriteLine(sbSuccesses.ToString());
        Assert.True(sbFails.Length < 1, sbFails.ToString());
    }


    [Fact]
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



    [Fact]
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
                if (b.Url?.StartsWith("https://s3.fgsfds.link") == true)
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
            .WithEndpoint("s3.fgsfds.link:9000")
            .WithCredentials(access, secretKey: secret)
            .Build();

        var args = new ListObjectsArgs()
            .WithBucket("superheater")
            .WithRecursive(true);

        var filesInBucket = new List<string>();

        await foreach (var item in iMinioClient.ListObjectsEnumAsync(args))
        {
            if (item.Key.EndsWith('/'))
            {
                continue;
            }

            filesInBucket.Add("https://s3.fgsfds.link/superheater/" + item.Key);
        }

        var loose = filesInBucket.Except(fixesUrls);

        StringBuilder sb = new();

        foreach (var item in loose)
        {
            _ = sb.AppendLine($"[Error] File {item} is loose.");
        }

        Assert.True(sb.Length < 1, sb.ToString());
    }
}
