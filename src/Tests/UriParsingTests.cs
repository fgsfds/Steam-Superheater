using Common.Axiom.Helpers;

namespace Tests;

[Collection("Sync")]

public sealed class UriParsingTests
{
    [Theory]
    [InlineData("https://example.com", true, "https", "example.com", 443)]
    [InlineData("https://example.com:8080", true, "https", "example.com", 8080)]

    [InlineData("http://example.com", true, "http", "example.com", 80)]
    [InlineData("http://example.com:8080", true, "http", "example.com", 8080)]

    [InlineData("example.com", true, "https", "example.com", 443)]
    [InlineData("example.com:1234", true, "https", "example.com", 1234)]

    [InlineData("ftp://example.com", true, "ftp", "example.com", 21)]
    [InlineData("ftp://example.com:666", true, "ftp", "example.com", 666)]

    [InlineData("ftps://example.com", true, "ftps", "example.com", -1)]
    [InlineData("ftps://example.com:666", true, "ftps", "example.com", 666)]

    [InlineData("", false, null, null, 0)]
    [InlineData("  ", false, null, null, 0)]
    [InlineData("invalid uri", false, null, null, 0)]
    public void UriHelperTest(string input, bool shouldSucceed, string? expectedScheme, string? expectedHost, int expectedPort)
    {
        var success = UriHelper.TryParseUri(input, out var uri);
        Assert.Equal(shouldSucceed, success);

        if (shouldSucceed)
        {
            Assert.NotNull(uri);
            Assert.Equal(expectedScheme, uri!.Scheme);
            Assert.Equal(expectedHost, uri.Host);
            Assert.Equal(expectedPort, uri.Port);
        }
        else
        {
            Assert.Null(uri);
        }
    }
}