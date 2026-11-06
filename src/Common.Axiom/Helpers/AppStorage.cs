namespace Common.Axiom.Helpers;

/// <summary>
/// Stable paths for the portable application, independent of the launch directory.
/// </summary>
public static class AppStorage
{
    /// <summary>Directory containing the application assemblies/executable.</summary>
    public static string DirectoryPath => AppContext.BaseDirectory;
    /// <summary>Portable SQLite database path.</summary>
    public static string DatabasePath => Path.Combine(DirectoryPath, "Superheater.db");
}
