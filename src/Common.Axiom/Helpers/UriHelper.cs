using System.Diagnostics.CodeAnalysis;

namespace Common.Axiom.Helpers;

public static class UriHelper
{
    public static bool TryParseUri(string input, [NotNullWhen(true)] out Uri? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Add scheme if missing
        string working = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                         input.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                         input.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)
            ? input
            : $"https://{input}";

        if (!Uri.TryCreate(working, UriKind.Absolute, out var uri))
        {
            return false;
        }


        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps &&
            uri.Scheme != Uri.UriSchemeFtp &&
            uri.Scheme != Uri.UriSchemeFtps)
        {
            return false;
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        result = uri;
        return true;
    }
}
