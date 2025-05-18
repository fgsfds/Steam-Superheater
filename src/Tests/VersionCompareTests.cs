using Common.Helpers;

namespace Tests;

public sealed class VersionCompareTests
{
    [Theory]
    [InlineData("1.1", "==1.1")]
    [InlineData("1.1", ">=1.1")]
    [InlineData("1.1", ">=1.0")]
    [InlineData("1.1", "<=1.1")]
    [InlineData("1.1", "<=1.2")]
    [InlineData("1.1", ">1.0")]
    [InlineData("1.1", "<1.3")]
    public void CompareTest(string v1, string v2)
    {
        var result = VersionComparer.Compare(v1, v2);

        Assert.True(result);
    }

    [Theory]
    [InlineData("1.1-a1", "==1.1-a1")]
    [InlineData("1.1-a1", ">=1.1-a1")]
    [InlineData("1.1-a1", ">=1.1-a0")]
    [InlineData("1.1-a1", "<=1.1-a1")]
    [InlineData("1.1-a1", "<=1.1-a2")]
    [InlineData("1.1-a1", ">1.1-a0")]
    [InlineData("1.1-a1", "<1.1-a3")]
    public void CompareTest2(string v1, string v2)
    {
        var result = VersionComparer.Compare(v1, v2);

        Assert.True(result);
    }

    [Theory]
    [InlineData("1.1-a1", "1.1-a1", "==")]
    [InlineData("1.1-a1", "1.1-a1", ">=")]
    [InlineData("1.1-a1", "1.1-a0", ">=")]
    [InlineData("1.1-a1", "1.1-a1", "<=")]
    [InlineData("1.1-a1", "1.1-a2", "<=")]
    [InlineData("1.1-a1", "1.1-a0", ">")]
    [InlineData("1.1-a1", "1.1-a3", "<")]
    public void CompareTest3(string v1, string v2, string op)
    {
        var result = VersionComparer.Compare(v1, v2, op);

        Assert.True(result);
    }

    [Theory]
    [InlineData("https://example.com", true, "https", "example.com", 443)]
    [InlineData("http://example.com:8080", true, "http", "example.com", 8080)]
    [InlineData("example.com:1234", true, "https", "example.com", 1234)]
    [InlineData("localhost:5000", true, "https", "localhost", 5000)]
    [InlineData("invalid_uri", false, null, null, 0)]
    [InlineData("", false, null, null, 0)]
    [InlineData(null, false, null, null, 0)]
    [InlineData("file:///C:/path/to/file.txt", false, null, null, 0)]
    public void TryParseUri_ShouldParseCorrectly(
        string? input,
        bool expectedResult,
        string? expectedScheme,
        string? expectedHost,
        int expectedPort)
    {
        var result = UriHelper.TryParseUri(input!, out var uri);

        Assert.Equal(expectedResult, result);

        if (expectedResult)
        {
            Assert.NotNull(uri);
            Assert.Equal(expectedScheme, uri.Scheme);
            Assert.Equal(expectedHost, uri.Host);
            Assert.Equal(expectedPort, uri.Port);
        }
        else
        {
            Assert.Null(uri);
        }
    }
}
