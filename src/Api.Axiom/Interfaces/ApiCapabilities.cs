namespace Api.Axiom.Interfaces;

/// <summary>Optional service features, independent of local catalog editing.</summary>
[Flags]
public enum ApiCapabilities
{
    /// <summary>No optional service operations.</summary>
    None = 0,
    /// <summary>Submission uploads.</summary>
    Upload = 1,
    /// <summary>Catalog/news mutations.</summary>
    CatalogWrite = 2,
    /// <summary>Voting, reports and statistics.</summary>
    Community = 4,
    /// <summary>All optional service operations.</summary>
    All = Upload | CatalogWrite | Community
}

/// <summary>Shared capability policy for UI service commands.</summary>
public static class ApiFeaturePolicy
{
    /// <summary>Whether the requested service action is available in the selected mode.</summary>
    public static bool Allows(ApiCapabilities capabilities, bool offline, ApiCapabilities feature) => !offline && (capabilities & feature) == feature;
    /// <summary>Explanation for unavailable service commands.</summary>
    public static string Explanation(bool offline) => offline
        ? "Service actions are disabled in offline catalog mode. Downloads may still need a network connection."
        : "This catalog backend does not support this action. Export catalog changes for maintainer review; uploads are a separate service.";
}
