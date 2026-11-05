using System.Security.Cryptography;
using Common.Client;
using Database.Client;
using Microsoft.Data.Sqlite;

namespace Tests.Unit.Sequential;

public sealed class AppRecoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-update-" + Guid.NewGuid().ToString("N"));
    public AppRecoveryTests() => Directory.CreateDirectory(_root);
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdateRestoresOldExecutableWhenLaunchFails(bool corrupt)
    {
        PrepareUpdate();
        if (corrupt) { File.WriteAllText(Path.Combine(_root, "update", "app"), "MZcorrupt"); }
        _ = Assert.ThrowsAny<Exception>(() => AppUpdateInstaller.ApplyStagedUpdate(_root, "app", _ => throw new IOException("launch failed")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(_root, "app")));
        Assert.True(File.Exists(Path.Combine(_root, ".update")));
    }
    [Fact]
    public void SuccessfulReplacementKeepsBackupUntilNextStartup()
    {
        PrepareUpdate();
        AppUpdateInstaller.ApplyStagedUpdate(_root, "app", path => Assert.Equal("MZnew", File.ReadAllText(path)));
        Assert.Equal("old", File.ReadAllText(Path.Combine(_root, "app.old")));
        Assert.False(File.Exists(Path.Combine(_root, ".update")));
    }
    [Fact]
    public void MissingStagedExecutableNeverMovesCurrent()
    {
        PrepareUpdate();
        File.Delete(Path.Combine(_root, "update", "app"));
        _ = Assert.Throws<FileNotFoundException>(() => AppUpdateInstaller.ApplyStagedUpdate(_root, "app", _ => Assert.Fail()));
        Assert.Equal("old", File.ReadAllText(Path.Combine(_root, "app")));
    }
    [Fact]
    public void LegacyDatabaseImportPreservesSourceAndPrefersPortableCopy()
    {
        var app = Path.Combine(_root, "appdir");
        var legacy = Path.Combine(_root, "legacy");
        _ = Directory.CreateDirectory(app); _ = Directory.CreateDirectory(legacy);
        var source = Path.Combine(legacy, "Superheater.db");
        using (var connection = new SqliteConnection($"Data Source={source};Pooling=False"))
        {
            connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "CREATE TABLE Test(Value TEXT); INSERT INTO Test VALUES ('preserved');"; _ = command.ExecuteNonQuery();
        }
        PortableDatabase.ImportLegacyIfNeeded(app, legacy);
        Assert.True(File.Exists(source));
        using var copy = new SqliteConnection($"Data Source={Path.Combine(app, "Superheater.db")};Pooling=False");
        copy.Open(); using var read = copy.CreateCommand(); read.CommandText = "SELECT Value FROM Test";
        Assert.Equal("preserved", read.ExecuteScalar());
        PortableDatabase.ImportLegacyIfNeeded(app, Path.Combine(_root, "missing"));
    }
    private void PrepareUpdate()
    {
        _ = Directory.CreateDirectory(Path.Combine(_root, "update"));
        File.WriteAllText(Path.Combine(_root, "app"), "old");
        var staged = Path.Combine(_root, "update", "app"); File.WriteAllText(staged, "MZnew");
        File.WriteAllText(Path.Combine(_root, ".update"), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(staged))));
    }
    public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(_root, true); }
}
