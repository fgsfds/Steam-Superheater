using CommunityToolkit.Diagnostics;
using SharpCompress.Archives;

namespace Common.Client.FilesTools;

/// <summary>
/// Class for working with archives
/// </summary>
public sealed class ArchiveTools
{
    private readonly ProgressReport _progressReport;


    public ArchiveTools(ProgressReport progressReport)
    {
        _progressReport = progressReport;
    }


    /// <summary>
    /// Unpack fix from zip archive
    /// </summary>
    /// <param name="pathToArchive">Absolute path to archive file</param>
    /// <param name="unpackTo">Directory to unpack archive to</param>
    /// <param name="variant">Fix variant</param>
    public async Task UnpackArchiveAsync(
        string pathToArchive,
        string unpackTo,
        string? variant
        )
    {
        IProgress<float> progress = _progressReport.Progress;
        _progressReport.OperationMessage = "Unpacking...";

        using var archive = ArchiveFactory.Open(pathToArchive);
        using var reader = archive.ExtractAllEntries();

        var entriesCount = variant is null
        ? archive.Entries.Count()
        : archive.Entries.Count(x => x.Key!.StartsWith(variant));

        var entryNumber = 1f;

        await Task.Run(() =>
        {
            while (reader.MoveToNextEntry())
            {
                var entry = reader.Entry;

                if (variant is not null &&
                    !entry.Key!.StartsWith(variant + "/"))
                {
                    continue;
                }

                var fullName = variant is null
                    ? Path.Combine(unpackTo, entry.Key!)
                    : Path.Combine(unpackTo, entry.Key!.Replace(variant + "/", string.Empty));

                if (!Directory.Exists(Path.GetDirectoryName(fullName)))
                {
                    var dirName = Path.GetDirectoryName(fullName) ?? ThrowHelper.ThrowArgumentNullException<string>(fullName);
                    _ = Directory.CreateDirectory(dirName);
                }

                if (entry.IsDirectory)
                {
                    _ = Directory.CreateDirectory(fullName);
                }
                else
                {
                    using var writableStream = File.OpenWrite(fullName);
                    reader.WriteEntryTo(writableStream);
                }

                var value = entryNumber / entriesCount * 100;
                progress.Report(value);

                entryNumber++;
            }
        }).ConfigureAwait(false);

        _progressReport.OperationMessage = string.Empty;
    }

    /// <summary>
    /// Get list of files and new folders in the archive
    /// </summary>
    /// <param name="pathToArchive">Path to ZIP</param>
    /// <param name="unpackToPath">Full path</param>
    /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
    /// <param name="variant">Fix variant</param>
    /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
    public List<string> GetListOfFilesInArchive(
        string pathToArchive,
        string unpackToPath,
        string? fixInstallFolder,
        string? variant
        )
    {
        using var archive = ArchiveFactory.Open(pathToArchive);
        using var reader = archive.ExtractAllEntries();

        List<string> files = new(archive.Entries.Count() + 1);

        //if directory that the archive will be extracted to doesn't exist, add it to the list too
        if (!Directory.Exists(unpackToPath) &&
            fixInstallFolder is not null)
        {
            files.Add(fixInstallFolder + Path.DirectorySeparatorChar);
        }

        while (reader.MoveToNextEntry())
        {
            var entry = reader.Entry;

            var path = entry.Key;

            if (variant is not null)
            {
                if (entry.Key!.StartsWith(variant + '/'))
                {
                    path = entry.Key.Replace(variant + '/', string.Empty);

                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            var fullName = Path.Combine(fixInstallFolder ?? string.Empty, path!)
                .Replace('/', Path.DirectorySeparatorChar);

            //if it's a file, add it to the list
            if (!entry.IsDirectory)
            {
                files.Add(fullName);
            }
            //if it's a directory and it doesn't already exist, add it to the list
            else if (!Directory.Exists(Path.Combine(unpackToPath, path!)))
            {
                files.Add(fullName);
            }
        }

        return files;
    }
}

