using System.Net;
using Api.Client;
using Common.Axiom;
using Common.Axiom.Enums;
using Common.Axiom.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit;

/// <summary>
/// Tests for <see cref="GitHubApiInterface"/>
/// </summary>
public sealed class GitHubApiInterfaceTests
{
    /// <summary>
    /// A provider failure is propagated with its result enum
    /// </summary>
    [Fact]
    public async Task GetLatestAppReleaseReturnsConnectionErrorWhenProviderFails()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Forbidden, string.Empty);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);
        GitHubApiInterface api = new(provider, httpClient, new Mock<ILogger>().Object);

        var result = await api.GetLatestAppReleaseAsync(OSEnum.Windows).ConfigureAwait(true);

        Assert.Equal(ResultEnum.ConnectionError, result.ResultEnum);
    }

    /// <summary>
    /// A missing release for the requested operating system is reported as not found
    /// </summary>
    [Fact]
    public async Task GetLatestAppReleaseReturnsNotFoundWhenOsReleaseMissing()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, LinuxOnlyJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);
        GitHubApiInterface api = new(provider, httpClient, new Mock<ILogger>().Object);

        var result = await api.GetLatestAppReleaseAsync(OSEnum.Windows).ConfigureAwait(true);

        Assert.Equal(ResultEnum.NotFound, result.ResultEnum);
    }

    /// <summary>
    /// The release matching the requested operating system is returned
    /// </summary>
    [Fact]
    public async Task GetLatestAppReleaseReturnsReleaseForRequestedOs()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, ReleasesJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);
        GitHubApiInterface api = new(provider, httpClient, new Mock<ILogger>().Object);

        var result = await api.GetLatestAppReleaseAsync(OSEnum.Linux).ConfigureAwait(true);

        Assert.Equal(ResultEnum.Success, result.ResultEnum);
        Assert.NotNull(result.ResultObject);
        Assert.Equal(new Version(1, 2, 3), result.ResultObject!.Version);
        Assert.Equal(new Uri("https://example.com/linux-x64.zip"), result.ResultObject.DownloadUrl);
    }

    private const string ReleasesJson = """
        [
          {
            "tag_name": "1.2.3",
            "draft": false,
            "prerelease": false,
            "body": "Test release",
            "assets": [
              { "name": "superheater_123_win-x64.zip", "browser_download_url": "https://example.com/win-x64.zip", "updated_at": "2024-01-01T00:00:00Z" },
              { "name": "superheater_123_linux-x64.zip", "browser_download_url": "https://example.com/linux-x64.zip", "updated_at": "2024-01-01T00:00:00Z" }
            ]
          }
        ]
        """;

    private const string LinuxOnlyJson = """
        [
          {
            "tag_name": "1.2.3",
            "draft": false,
            "prerelease": false,
            "body": "Test release",
            "assets": [
              { "name": "superheater_123_linux-x64.zip", "browser_download_url": "https://example.com/linux-x64.zip", "updated_at": "2024-01-01T00:00:00Z" }
            ]
          }
        ]
        """;

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);

#pragma warning disable IDISP004 // Content is owned and disposed by the response
            response.Content = new StringContent(content);
#pragma warning restore IDISP004

            return Task.FromResult(response);
        }
    }
}
