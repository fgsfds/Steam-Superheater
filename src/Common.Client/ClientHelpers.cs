namespace Common.Client;

public static class ClientHelpers
{
    public static string GetFullPath(string root, string? folder)
    {
        if (folder is null)
        {
            return root;
        }
        else if (Path.IsPathRooted(folder))
        {
            return folder;
        }
        else if (folder.StartsWith("{documents}", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveDocumentsPath(folder, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
        else
        {
            return Path.Combine(root, folder);
        }
    }
    internal static string ResolveDocumentsPath(string folder, string documentsDirectory)
    {
        if (string.IsNullOrWhiteSpace(documentsDirectory) || !Path.IsPathFullyQualified(documentsDirectory))
        {
            throw new DirectoryNotFoundException("The Documents folder is unavailable. Configure a Documents folder before installing a fix that uses {documents}.");
        }

        return folder.Replace("{documents}", documentsDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
