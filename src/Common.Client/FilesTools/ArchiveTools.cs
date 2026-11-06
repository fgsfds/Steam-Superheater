using SharpCompress.Archives;

namespace Common.Client.FilesTools;

/// <summary>
/// Validates archive destinations and extracts fix files.
/// </summary>
public sealed class ArchiveTools
{
    private readonly ProgressReport _progressReport;

    /// <summary>
    /// Creates archive tools using the supplied progress reporter.
    /// </summary>
    public ArchiveTools(ProgressReport progressReport)
    {
        _progressReport = progressReport;
    }

    /// <summary>
    /// Validates all selected destinations before extracting an archive.
    /// </summary>
    /// <param name="pathToArchive">Absolute path to the archive.</param>
    /// <param name="unpackTo">Extraction root.</param>
    /// <param name="variant">Optional leading variant directory.</param>
    public async Task UnpackArchiveAsync(string pathToArchive, string unpackTo, string? variant)
    {
        _progressReport.OperationMessage = "Unpacking...";

        try
        {
            using var archive = ArchiveFactory.Open(pathToArchive);
            var entries = GetValidatedEntries(archive, unpackTo, variant);
            await Task.Run(() =>
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var (entry, _, fullPath) = entries[index];
                    RejectLinks(fullPath);
                    if (entry.IsDirectory)
                    {
                        _ = Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                        entry.WriteToFile(fullPath);
                    }

                    ((IProgress<float>)_progressReport.Progress).Report((index + 1f) / entries.Count * 100);
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _progressReport.OperationMessage = string.Empty;
        }
    }

    /// <summary>
    /// Gets validated file and newly created directory paths for backup and uninstall records.
    /// </summary>
    /// <param name="pathToArchive">Archive path.</param>
    /// <param name="unpackToPath">Extraction root.</param>
    /// <param name="fixInstallFolder">Install folder recorded relative to the game, or an explicit external root.</param>
    /// <param name="variant">Optional leading variant directory.</param>
    /// <returns>Validated paths and file CRC values, with null values for new directories.</returns>
    public Dictionary<string, long?> GetListOfFilesInArchive(
        string pathToArchive, string unpackToPath, string? fixInstallFolder, string? variant)
    {
        using var archive = ArchiveFactory.Open(pathToArchive);
        var entries = GetValidatedEntries(archive, unpackToPath, variant);
        var files = new Dictionary<string, long?>();

        if (!Directory.Exists(unpackToPath) && fixInstallFolder is not null)
        {
            files.Add(fixInstallFolder + Path.DirectorySeparatorChar, null);
        }

        foreach (var (entry, relativePath, fullPath) in entries)
        {
            var name = Path.Combine(fixInstallFolder ?? string.Empty, relativePath);
            if (!entry.IsDirectory)
            {
                files.Add(name, entry.Crc);
            }
            else if (!Directory.Exists(fullPath))
            {
                files.Add(name + Path.DirectorySeparatorChar, null);
            }
        }

        return files;
    }

    private static List<(IArchiveEntry Entry, string RelativePath, string FullPath)> GetValidatedEntries(
        IArchive archive, string destination, string? variant)
    {
        var root = Path.GetFullPath(destination);
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var paths = new Dictionary<string, bool>(comparer);
        var result = new List<(IArchiveEntry Entry, string RelativePath, string FullPath)>();
        var variantPrefix = variant is null ? null : NormalizeRelativePath(variant).TrimEnd('/') + '/';
        RejectLinks(root);

        foreach (var entry in archive.Entries)
        {
            var name = NormalizeRelativePath(entry.Key);
            var attributes = entry.Attrib.GetValueOrDefault();
            if (!string.IsNullOrEmpty(entry.LinkTarget) ||
                ((attributes >> 16) & 0xF000) == 0xA000 ||
                (attributes & 0xF000) == 0xA000 ||
                (attributes & (int)FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException($"Archive links are not supported: {entry.Key}");
            }

            if (variantPrefix is not null)
            {
                if (!name.StartsWith(variantPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                name = name[variantPrefix.Length..];
                if (name.Length == 0)
                {
                    continue;
                }
            }

            var relativePath = name.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!fullPath.StartsWith(prefix, comparison))
            {
                throw new InvalidDataException($"Archive entry escapes the extraction folder: {entry.Key}");
            }

            RejectLinks(fullPath);
            if (!paths.TryAdd(fullPath, entry.IsDirectory))
            {
                throw new InvalidDataException($"Duplicate archive destination: {entry.Key}");
            }

            result.Add((entry, relativePath, fullPath));
        }

        foreach (var (_, _, fullPath) in result)
        {
            var parent = Path.GetDirectoryName(fullPath);
            while (parent is not null && parent.StartsWith(prefix, comparison))
            {
                if (paths.TryGetValue(parent, out var isDirectory) && !isDirectory)
                {
                    throw new InvalidDataException($"Archive file is also used as a directory: {parent}");
                }

                parent = Path.GetDirectoryName(parent);
            }
        }

        return result;
    }

    private static string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException("An archive path is empty.");
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.Contains(':') || normalized.Contains('\0'))
        {
            throw new InvalidDataException($"Rooted or invalid archive path: {path}");
        }

        foreach (var segment in normalized.TrimEnd('/').Split('/'))
        {
            if (segment is "" or "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                (OperatingSystem.IsWindows() && (segment.EndsWith('.') || segment.EndsWith(' '))))
            {
                throw new InvalidDataException($"Invalid archive path segment: {path}");
            }
        }

        return normalized;
    }

    private static void RejectLinks(string path)
    {
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException($"Extraction through a filesystem link is not supported: {current}");
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }
}
