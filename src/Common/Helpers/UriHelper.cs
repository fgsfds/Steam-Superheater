using System.Diagnostics.CodeAnalysis;

namespace Common.Helpers;

public static class UriHelper
{
    public static bool TryParseUri(string input, [NotNullWhen(true)]out Uri? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Scheme != Uri.UriSchemeFile)
        {
            result = uri;
            return true;
        }

        if (input.Contains(":"))
        {
            var withScheme = "http://" + input;

            if (Uri.TryCreate(withScheme, UriKind.Absolute, out uri) &&
                !string.IsNullOrEmpty(uri.Host) &&
                uri.Port > 0)
            {
                result = uri;
                return true;
            }
        }

        return false;
    }
}
