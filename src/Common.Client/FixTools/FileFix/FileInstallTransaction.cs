using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Client.FixTools.FileFix;

/// <summary>Durable before-images for one file-fix operation, including its installed record.</summary>
internal sealed class FileInstallTransaction : IDisposable
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> GameLocks = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private sealed class GameLease(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
    internal static async Task<IDisposable> AcquireAsync(string gameDirectory, CancellationToken cancellationToken)
    {
        var gate = GameLocks.GetOrAdd(Path.GetFullPath(gameDirectory), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new GameLease(gate);
    }
    private readonly string _directory;
    private readonly List<FileSnapshot> _snapshots;
    private bool _committed;
    private bool _disposed;
    private FileInstallTransaction(string directory, List<FileSnapshot> snapshots) { _directory = directory; _snapshots = snapshots; }

    internal static FileInstallTransaction Begin(string gameDirectory, Guid fixId, IEnumerable<string> paths)
    {
        var directory = Path.Combine(gameDirectory, ".superheater-transactions", fixId.ToString("N") + "-" + Guid.NewGuid().ToString("N"));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var candidates = paths.Select(Path.GetFullPath).Select(Path.TrimEndingDirectorySeparator).Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).OrderBy(x => x.Length).ToList();
        var roots = new List<string>();
        foreach (var path in candidates)
        {
            if (directory.StartsWith(path + Path.DirectorySeparatorChar, comparison)) { throw new InvalidDataException("A fix cannot replace the game root or its transaction storage."); }
            if (!roots.Any(root => path.StartsWith(root + Path.DirectorySeparatorChar, comparison))) { roots.Add(path); }
        }
        _ = Directory.CreateDirectory(directory);
        var snapshots = new List<FileSnapshot>();
        foreach (var path in roots)
        {
            var backup = snapshots.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var isDirectory = Directory.Exists(path);
            var exists = isDirectory || File.Exists(path);
            if (exists) { CopyPath(path, Path.Combine(directory, backup)); }
            snapshots.Add(new FileSnapshot { Path = path, Backup = backup, Exists = exists, IsDirectory = isDirectory });
        }
        using (var stream = new FileStream(Path.Combine(directory, "journal.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, snapshots, FileTransactionJson.Default.ListFileSnapshot);
            stream.Flush(true);
        }
        return new FileInstallTransaction(directory, snapshots);
    }

    internal static void Recover(string gameDirectory, bool ownsLock = false)
    {
        var gate = GameLocks.GetOrAdd(Path.GetFullPath(gameDirectory), _ => new SemaphoreSlim(1, 1));
        if (!ownsLock && !gate.Wait(0)) { return; }
        try
        {
        var root = Path.Combine(gameDirectory, ".superheater-transactions");
        if (!Directory.Exists(root)) { return; }
        foreach (var directory in Directory.GetDirectories(root))
        {
            var journal = Path.Combine(directory, "journal.json");
            if (!File.Exists(journal)) { continue; }
            var snapshots = JsonSerializer.Deserialize(File.ReadAllText(journal), FileTransactionJson.Default.ListFileSnapshot)
                ?? throw new InvalidDataException("Invalid recovery journal. Preserve it for manual recovery.");
            using var transaction = new FileInstallTransaction(directory, snapshots) { _committed = File.Exists(Path.Combine(directory, "committed")) };
        }
        }
        finally { if (!ownsLock) { _ = gate.Release(); } }
    }
    internal void Commit()
    {
        using var marker = new FileStream(Path.Combine(_directory, "committed"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        marker.WriteByte(1); marker.Flush(true);
        _committed = true;
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        if (!_committed)
        {
            foreach (var snapshot in _snapshots.AsEnumerable().Reverse())
            {
                if (snapshot.Exists && !File.Exists(Path.Combine(_directory, snapshot.Backup)) && !Directory.Exists(Path.Combine(_directory, snapshot.Backup)))
                { throw new IOException("Recovery backup is missing. Preserve the transaction directory for manual recovery."); }
                DeletePath(snapshot.Path);
                if (snapshot.Exists) { CopyPath(Path.Combine(_directory, snapshot.Backup), snapshot.Path); }
            }
        }
        Directory.Delete(_directory, true);
        var parent = Path.GetDirectoryName(_directory)!;
        if (!Directory.EnumerateFileSystemEntries(parent).Any()) { Directory.Delete(parent); }
        _disposed = true;
    }

    private static void DeletePath(string path)
    {
        if (File.Exists(path)) { File.Delete(path); }
        else if (Directory.Exists(path))
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { throw new IOException("Refusing recovery through a filesystem link."); }
            Directory.Delete(path, true);
        }
    }

    internal static void CopyPath(string source, string destination)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) { throw new InvalidDataException("Filesystem links cannot be included in a file-fix transaction."); }
        if (Directory.Exists(source))
        {
            _ = Directory.CreateDirectory(destination);
            foreach (var child in Directory.EnumerateFileSystemEntries(source)) { CopyPath(child, Path.Combine(destination, Path.GetFileName(child))); }
        }
        else
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, true);
        }
    }
}

internal sealed class FileSnapshot
{
    public required string Path { get; init; }
    public required string Backup { get; init; }
    public bool Exists { get; init; }
    public bool IsDirectory { get; init; }
}

[JsonSerializable(typeof(List<FileSnapshot>))]
internal sealed partial class FileTransactionJson : JsonSerializerContext;
