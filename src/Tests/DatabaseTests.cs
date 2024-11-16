using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
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

#if !DEBUG
return;
#endif

        StringBuilder sb = new();

        foreach (var list in fixesJson)
        {
            foreach (var fix in list.Fixes)
            {
                if (fix is not FileFixEntity fileFix ||
                    fix.IsDisabled)
                {
                    continue;
                }

                var url = fileFix.Url;
                var size = fileFix.FileSize;
                var md5 = fileFix.MD5;

                if (url is null)
                {
                    continue;
                }

                if (md5 is null)
                {
                    _ = sb.AppendLine($"File {url} doesn't have MD5 in the database");
                }
                if (size is null)
                {
                    _ = sb.AppendLine($"File {url} doesn't have size in the database");
                }

                using var header = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!header.IsSuccessStatusCode)
                {
                    _ = sb.AppendLine($"File {url} doesn't exist.");
                    continue;
                }

                if (size is not null &&
                    header.Content.Headers.ContentLength is not null &&
                    size != header.Content.Headers.ContentLength)
                {
                    _ = sb.AppendLine($"File {url} size doesn't match. Expected {size} got {header.Content.Headers.ContentLength}");
                }

                //md5 of files from s3
                if (url.StartsWith(Consts.FilesBucketUrl))
                {
                    if (url.EndsWith("re4_re4hd_v1_1.zip"))
                    {
                        //nothing to do
                    }
                    else if (header.Headers.ETag?.Tag is null)
                    {
                        _ = sb.AppendLine($"File {url} doesn't have ETag");
                    }
                    else
                    {
                        var md5e = header.Headers.ETag!.Tag.Replace("\"", "");

                        if (md5e.Contains('-'))
                        {
                            _ = sb.AppendLine($"File {url} has incorrect ETag");
                        }
                        else if (!md5e.Equals(md5, StringComparison.OrdinalIgnoreCase))
                        {
                            _ = sb.AppendLine($"File {url} has wrong MD5");
                        }
                    }
                }
                //md5 of external files
                else if (header.Content.Headers.ContentMD5 is not null &&
                        !Convert.ToHexString(header.Content.Headers.ContentMD5).Equals(md5, StringComparison.OrdinalIgnoreCase))
                {
                    _ = sb.AppendLine($"File {url} has wrong MD5");
                }
            }
        }

        _output.WriteLine(sb.ToString());
        Assert.True(sb.Length < 1);
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
}
