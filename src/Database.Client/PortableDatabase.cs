using Common.Axiom.Helpers;
using Microsoft.Data.Sqlite;

namespace Database.Client;

/// <summary>Imports legacy working-directory databases without discarding SQLite recovery state.</summary>
public static class PortableDatabase
{
    /// <summary>Connection string shared by the context and runtime registration.</summary>
    public static string ConnectionString => new SqliteConnectionStringBuilder { DataSource = AppStorage.DatabasePath }.ToString();

    /// <summary>Copies a legacy database consistently; preserves the source and prefers an existing portable database.</summary>
    public static void ImportLegacyIfNeeded(string appDirectory, string workingDirectory)
    {
        var target = Path.GetFullPath(Path.Combine(appDirectory, "Superheater.db"));
        var source = Path.GetFullPath(Path.Combine(workingDirectory, "Superheater.db"));
        if (File.Exists(target) || !File.Exists(source) || string.Equals(source, target, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) { return; }
        var temporary = target + ".import-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var original = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString()))
            using (var copy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temporary, Pooling = false }.ToString()))
            {
                original.Open();
                copy.Open();
                original.BackupDatabase(copy);
            }
            File.Move(temporary, target);
        }
        finally
        {
            if (File.Exists(temporary)) { File.Delete(temporary); }
        }
    }
}
