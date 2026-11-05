namespace Common.Client;

/// <summary>
/// Owns startup and checks portable-folder write access before application mutations.
/// </summary>
public sealed class ApplicationStartup : IDisposable
{
    private readonly Mutex _mutex;
    private ApplicationStartup(Mutex mutex) => _mutex = mutex;

    /// <summary>
    /// Acquires single-instance ownership and verifies the application's directory is writable.
    /// </summary>
    public static ApplicationStartup Acquire(string directory, string name = "Superheater")
    {
        var mutex = new Mutex(false, name);
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                throw new IOException("Superheater is already running.");
            }

            using var probe = new FileStream(Path.Combine(directory, ".superheater-write-" + Guid.NewGuid().ToString("N")),
                FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return new ApplicationStartup(mutex);
        }
        catch
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }

            mutex.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Deletes only the known executable backup after a replacement can start.
    /// SQLite sidecars, arbitrary temporary files and update staging are preserved.
    /// </summary>
    public static void Cleanup(string directory, string executable)
    {
        if (Path.GetFileName(executable) != executable)
        {
            throw new ArgumentException("Expected an executable filename.", nameof(executable));
        }

        var current = Path.Combine(directory, executable);
        if (File.Exists(current) && File.Exists(current + ".old"))
        {
            File.Delete(current + ".old");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
