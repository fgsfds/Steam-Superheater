using Common.Client;

namespace Tests.Unit.Sequential;

public sealed class ApplicationStartupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-startup-" + Guid.NewGuid().ToString("N"));
    public ApplicationStartupTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void AnotherThreadCannotAcquireUntilOwnerExits()
    {
        var name = "Superheater-test-" + Guid.NewGuid().ToString("N");
        using (var lease = ApplicationStartup.Acquire(_root, name))
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { using var competing = ApplicationStartup.Acquire(_root, name); }
                catch (Exception ex) { failure = ex; }
            });
            thread.Start();
            thread.Join();
            Assert.IsType<IOException>(failure);
        }

        using var next = ApplicationStartup.Acquire(_root, name);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public void FailedWriteProbeReleasesOwnership()
    {
        var name = "Superheater-test-" + Guid.NewGuid().ToString("N");
        _ = Assert.Throws<DirectoryNotFoundException>(() => ApplicationStartup.Acquire(Path.Combine(_root, "missing"), name));
        using var lease = ApplicationStartup.Acquire(_root, name);
    }

    [Fact]
    public void CleanupPreservesDatabaseRecoveryAndUnrelatedFiles()
    {
        var names = new[] { "Superheater", "Superheater.old", "unrelated.old", "download.temp", "Superheater.db-wal", "Superheater.db-shm", ".update" };
        foreach (var name in names) { File.WriteAllText(Path.Combine(_root, name), "preserve"); }
        ApplicationStartup.Cleanup(_root, "Superheater");
        Assert.False(File.Exists(Path.Combine(_root, "Superheater.old")));
        foreach (var name in names.Where(x => x != "Superheater.old")) { Assert.Equal("preserve", File.ReadAllText(Path.Combine(_root, name))); }
    }

    public void Dispose() => Directory.Delete(_root, true);
}
