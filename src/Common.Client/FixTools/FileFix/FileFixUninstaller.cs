using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.Client.FixTools.FileFix;

public sealed class FileFixUninstaller
{
    /// <summary>
    /// Uninstall fix: delete files, restore backup
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <param name="installedFix">Fix entity</param>
    public void UninstallFix(
        GameEntity game,
        BaseInstalledFixEntity installedFix
        )
    {
        Guard2.IsOfType<FileInstalledFixEntity>(installedFix, out var installedFileFix);

        if (installedFileFix.InstalledSharedFix is not null)
        {
            UninstallFix(game, installedFileFix.InstalledSharedFix);
        }

        DeleteFiles(game.InstallDir, installedFileFix.FilesList);

        RestoreBackup(game.InstallDir, installedFileFix);

        RemoveWineDllOverrides(game.Id, installedFileFix.WineDllOverrides);
    }

    /// <summary>
    /// Remove dll overrides from the registry
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="dllList">List of added lines</param>
    private void RemoveWineDllOverrides(
        int gameId,
        List<string>? dllList
        )
    {
        if (dllList is null ||
            !OperatingSystem.IsLinux())
        {
            return;
        }

        var file = $"{Environment.GetEnvironmentVariable("HOME")}/.local/share/Steam/steamapps/compatdata/{gameId}/pfx/user.reg";

        var linesList = File.ReadAllLines(file).ToList();

        var startIndex = linesList.FindIndex(static x => x.Contains(@"[Software\\Wine\\DllOverrides]"));

        List<int> indexes = new(dllList.Count);

        for (var i = startIndex; i < linesList.Count; i++)
        {
            if (linesList[i].Equals(""))
            {
                break;
            }

            if (dllList.Contains(linesList[i]))
            {
                indexes.Add(i);
            }
        }

        indexes.Reverse();

        foreach (var ind in indexes)
        {
            linesList.RemoveAt(ind);
        }

        File.WriteAllLines(file, linesList);
    }

    /// <summary>
    /// Delete files from game install directory
    /// </summary>
    /// <param name="gameInstallDir">Game install folder</param>
    /// <param name="fixFiles">Files to delete</param>
    private static void DeleteFiles(
        string gameInstallDir,
        List<string>? fixFiles
        )
    {
        if (fixFiles is null)
        {
            return;
        }

        //checking if files can be opened before deleting them
        foreach (var file in fixFiles)
        {
            var fullPath = ClientHelpers.GetFullPath(gameInstallDir, file);

            if (!file.EndsWith('/') &&
                !file.EndsWith('\\') &&
                File.Exists(fullPath))
            {
                var stream = File.Open(fullPath, FileMode.Open);
                stream.Dispose();
            }
        }

        //deleting files and adding folders to the list
        List<string> directories = [];
        foreach (var file in fixFiles)
        {
            var fullPath = ClientHelpers.GetFullPath(gameInstallDir, file);

            if (!file.EndsWith('/') &&
                !file.EndsWith('\\') &&
                File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                directories.Add(fullPath);
            }
        }

        var sortedDirectories = directories.OrderByDescending(static x => x.Length);

        //removing empty folders
        foreach (var dir in sortedDirectories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var files = Directory.GetFiles(dir);
            var dirs = Directory.GetDirectories(dir);

            if (files.Length == 0 && dirs.Length == 0)
            {
                Directory.Delete(dir, true);
            }
        }
    }

    /// <summary>
    /// Restore backed up files
    /// </summary>
    /// <param name="gameDir">Game install folder</param>
    /// <param name="fix">Installed fix</param>
    private static void RestoreBackup(
        string gameDir,
        FileInstalledFixEntity fix
        )
    {
        if (fix.BackupFolder is null)
        {
            return;
        }

        var backupFolder = Path.Combine(gameDir, Consts.BackupFolder, fix.BackupFolder);

        if (!Directory.Exists(backupFolder))
        {
            return;
        }

        var files = Directory.GetFiles(backupFolder, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(backupFolder, file);

            var pathTo = ClientHelpers.GetFullPath(gameDir, relativePath);

            File.Move(file, pathTo, true);
        }

        Directory.Delete(backupFolder, true);
    }
}
