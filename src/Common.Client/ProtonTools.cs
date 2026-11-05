namespace Common.Client;

/// <summary>
/// Locates Proton registries and adds only overrides owned by the current installation.
/// </summary>
public static class ProtonTools
{
    /// <summary>
    /// Finds a game's prefix in its Steam library first, then supported user Steam layouts.
    /// </summary>
    public static string? FindUserRegistry(string gameDirectory, int gameId, string? userDirectory = null)
    {
        var roots = new List<string>();
        for (var current = new DirectoryInfo(gameDirectory); current is not null; current = current.Parent)
        {
            if (current.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase)) { roots.Add(current.FullName); }
        }
        var user = userDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var layout in new[] { ".local/share/Steam", ".steam/steam", ".var/app/com.valvesoftware.Steam/.local/share/Steam", "snap/steam/common/.local/share/Steam" })
        {
            roots.Add(Path.Combine(user, layout, "steamapps"));
        }
        return roots.Select(root => Path.Combine(root, "compatdata", gameId.ToString(System.Globalization.CultureInfo.InvariantCulture), "pfx", "user.reg"))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Adds missing DLL overrides while preserving existing user choices; returns only added lines.
    /// </summary>
    public static async Task<List<string>> AddOverridesAsync(string registryPath, IEnumerable<string> dlls)
    {
        var lines = (await File.ReadAllLinesAsync(registryPath).ConfigureAwait(false)).ToList();
        const string Section = @"[Software\\Wine\\DllOverrides]";
        var start = lines.FindIndex(x => x.StartsWith(Section, StringComparison.Ordinal));
        if (start < 0)
        {
            lines.Add(string.Empty);
            lines.Add(Section);
            start = lines.Count - 1;
        }
        var end = lines.FindIndex(start + 1, x => x.StartsWith('['));
        if (end < 0) { end = lines.Count; }
        var added = new List<string>();
        foreach (var dll in dlls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (dll.IndexOfAny(['"', '\r', '\n', '\\']) >= 0) { throw new InvalidDataException("Invalid DLL override name."); }
            var key = $"\"{dll}\"=";
            if (lines.Skip(start + 1).Take(end - start - 1).Any(x => x.StartsWith(key, StringComparison.OrdinalIgnoreCase))) { continue; }
            var line = key + "\"native,builtin\"";
            lines.Insert(++start, line);
            end++;
            added.Add(line);
        }
        await File.WriteAllLinesAsync(registryPath, lines).ConfigureAwait(false);
        return added;
    }
}
