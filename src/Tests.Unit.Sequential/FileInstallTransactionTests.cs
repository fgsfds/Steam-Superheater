using Common.Client.FixTools.FileFix;

namespace Tests.Unit.Sequential;

public sealed class FileInstallTransactionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-journal-" + Guid.NewGuid().ToString("N"));
    public FileInstallTransactionTests() => Directory.CreateDirectory(_root);
    [Fact]
    public void RestartRecoveryRestoresOldAndRemovesNewPaths()
    {
        var original = Path.Combine(_root, "original"); var added = Path.Combine(_root, "added");
        File.WriteAllText(original, "original");
        _ = FileInstallTransaction.Begin(_root, Guid.NewGuid(), [original, added]);
        File.WriteAllText(original, "changed"); File.WriteAllText(added, "new");
        FileInstallTransaction.Recover(_root);
        Assert.Equal("original", File.ReadAllText(original)); Assert.False(File.Exists(added));
        FileInstallTransaction.Recover(_root);
        Assert.False(Directory.Exists(Path.Combine(_root, ".superheater-transactions")));
    }
    [Fact]
    public void CommittedJournalIsCleanedWithoutUndoingChanges()
    {
        var path = Path.Combine(_root, "file"); File.WriteAllText(path, "old");
        var transaction = FileInstallTransaction.Begin(_root, Guid.NewGuid(), [path]);
        File.WriteAllText(path, "new"); transaction.Commit();
        FileInstallTransaction.Recover(_root);
        Assert.Equal("new", File.ReadAllText(path));
    }
    [Fact]
    public void RollbackPreservesExistingBackupsAndCanBeRetried()
    {
        var backup = Path.Combine(_root, ".superheater", "backup"); _ = Directory.CreateDirectory(backup);
        File.WriteAllText(Path.Combine(backup, "original"), "saved");
        using (var transaction = FileInstallTransaction.Begin(_root, Guid.NewGuid(), [Path.Combine(_root, ".superheater")]))
        {
            Directory.Delete(backup, true);
        }
        Assert.Equal("saved", File.ReadAllText(Path.Combine(backup, "original")));
        using var retry = FileInstallTransaction.Begin(_root, Guid.NewGuid(), [Path.Combine(_root, ".superheater")]);
        retry.Commit();
    }
    public void Dispose() => Directory.Delete(_root, true);
}
