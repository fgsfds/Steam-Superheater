using Api.Axiom.Interfaces;
using Api.Client;
using Common.Axiom.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit;

public sealed class ApiCapabilitiesTests
{
    [Theory]
    [InlineData(ApiCapabilities.Upload, false, ApiCapabilities.Upload, true)]
    [InlineData(ApiCapabilities.Upload, false, ApiCapabilities.CatalogWrite, false)]
    [InlineData(ApiCapabilities.All, false, ApiCapabilities.Community, true)]
    [InlineData(ApiCapabilities.All, true, ApiCapabilities.Upload, false)]
    [InlineData(ApiCapabilities.None, false, ApiCapabilities.Community, false)]
    public void PolicySeparatesServiceFeaturesAndOfflineMode(ApiCapabilities available, bool offline, ApiCapabilities feature, bool expected)
        => Assert.Equal(expected, ApiFeaturePolicy.Allows(available, offline, feature));

    [Fact]
    public async Task GitHubUnsupportedActionsHaveUsefulErrorsWithoutNetwork()
    {
        using var client = new HttpClient();
        var logger = new Mock<ILogger>().Object;
        var api = new GitHubApiInterface(new AppReleasesProvider(logger, client), client, logger);
        Assert.Equal(ApiCapabilities.Upload, api.Capabilities);
        var result = await api.ReportFixAsync(Guid.NewGuid(), "report");
        Assert.False(result.IsSuccess); Assert.Contains("GitHub", result.Message);
    }
}
