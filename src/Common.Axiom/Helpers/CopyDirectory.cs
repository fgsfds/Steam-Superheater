using CommunityToolkit.Diagnostics;

namespace Common.Axiom.Helpers;

public static class DirectoryHelper
{
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            ThrowHelper.ThrowInvalidOperationException($"Source directory not found: {dir.FullName}");
        }

        var dirs = dir.GetDirectories();

        _ = Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            _ = file.CopyTo(targetFilePath);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
