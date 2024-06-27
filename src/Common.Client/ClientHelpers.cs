namespace Common.Client
{
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
            else if (folder.StartsWith("{documents}", StringComparison.InvariantCultureIgnoreCase))
            {
                return folder.Replace("{documents}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), StringComparison.InvariantCultureIgnoreCase);
            }
            else
            {
                return Path.Combine(root, folder);
            }
        }
    }
}
