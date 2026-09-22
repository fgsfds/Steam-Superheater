using System.Net;
using Common.Axiom;
using Common.Axiom.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit;

/// <summary>
/// Tests for <see cref="AppReleasesProvider"/>
/// </summary>
public sealed class AppReleasesProviderTests
{
    /// <summary>
    /// A non-success status code is reported as a connection error and no releases are set
    /// </summary>
    [Fact]
    public async Task GetLatestVersionReturnsConnectionErrorOnNonSuccessStatus()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Forbidden, string.Empty);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);

        var result = await provider.GetLatestVersionAsync().ConfigureAwait(true);

        Assert.Equal(ResultEnum.ConnectionError, result.ResultEnum);
        Assert.Null(provider.WindowsRelease);
        Assert.Null(provider.LinuxRelease);
    }

    /// <summary>
    /// A request that throws is reported as a connection error
    /// </summary>
    [Fact]
    public async Task GetLatestVersionReturnsConnectionErrorWhenRequestThrows()
    {
        using var handler = new ThrowingHttpMessageHandler();
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);

        var result = await provider.GetLatestVersionAsync().ConfigureAwait(true);

        Assert.Equal(ResultEnum.ConnectionError, result.ResultEnum);
    }

    /// <summary>
    /// A malformed response body is reported as a general error
    /// </summary>
    [Fact]
    public async Task GetLatestVersionReturnsErrorOnMalformedJson()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{ not json");
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);

        var result = await provider.GetLatestVersionAsync().ConfigureAwait(true);

        Assert.Equal(ResultEnum.Error, result.ResultEnum);
    }

    /// <summary>
    /// Valid releases populate both the Windows and Linux release properties
    /// </summary>
    [Fact]
    public async Task GetLatestVersionReturnsReleases()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, ReleasesJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);

        var result = await provider.GetLatestVersionAsync().ConfigureAwait(true);

        Assert.Equal(ResultEnum.Success, result.ResultEnum);
        Assert.NotNull(provider.WindowsRelease);
        Assert.NotNull(provider.LinuxRelease);
        Assert.Equal(new Version(1, 2, 3), provider.WindowsRelease!.Version);
        Assert.Equal(new Version(1, 2, 3), provider.LinuxRelease!.Version);
        Assert.Equal(new Uri("https://example.com/win-x64.zip"), provider.WindowsRelease.DownloadUrl);
        Assert.Equal(new Uri("https://example.com/linux-x64.zip"), provider.LinuxRelease.DownloadUrl);
    }

    /// <summary>
    /// An operating system without a matching asset is left as null
    /// </summary>
    [Fact]
    public async Task GetLatestVersionLeavesMissingOsReleaseNull()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, LinuxOnlyJson);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        AppReleasesProvider provider = new(new Mock<ILogger>().Object, httpClient);

        var result = await provider.GetLatestVersionAsync().ConfigureAwait(true);

        Assert.Equal(ResultEnum.Success, result.ResultEnum);
        Assert.Null(provider.WindowsRelease);
        Assert.NotNull(provider.LinuxRelease);
        Assert.Equal(new Version(1, 2, 3), provider.LinuxRelease!.Version);
    }

    private const string ReleasesJson = """
        [
          {
            "tag_name": "9.9.9",
            "draft": true,
            "prerelease": false,
            "body": "Draft release",
            "assets": [
              { "name": "superheater_999_win-x64.zip", "browser_download_url": "https://example.com/draft_win-x64.zip", "updated_at": "2024-01-01T00:00:00Z" }
            ]
          },
          {
            "tag_name": "8.8.8",
            "draft": false,
            "prerelease": true,
            "body": "Prerelease",
            "assets": [
              { "name": "superheater_888_win-x64.zip", "browser_download_url": "https://example.com/prerelease_win-x64.zip", "updated_at": "2024-01-01T00:00:00Z" }
            ]
          },
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

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Request failed");
        }
    }
}
