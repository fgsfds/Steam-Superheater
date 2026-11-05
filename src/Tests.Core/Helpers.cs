using System.Reflection;
using System.Text.Json;
using Common.Axiom.Entities;
using Common.Client;

namespace Tests.Core;

/// <summary>Shared resource locations and isolated test paths.</summary>
public static class Helpers
{
    private static readonly string TestTempFolder = Path.Combine(Path.GetTempPath(), "Superheater-tests-" + Guid.NewGuid().ToString("N"));
    /// <summary>Name of the fixture executable.</summary>
    public const string GameExe = "game exe.exe";
    /// <summary>Dedicated registry key for compatibility fixtures.</summary>
    public const string RegKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers_test";

    /// <summary>Isolated game directory.</summary>
    public static string GameDir => Path.Combine(TestFolder, "game_dir");

    /// <summary>Directory containing built test resources.</summary>
    public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    /// <summary>Unique temporary root for this test process.</summary>
    public static string TestFolder => TestTempFolder;

    /// <summary>Escaped platform directory separator for JSON snapshots.</summary>
    public static string SeparatorForJson
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "\\\\";
            }

            return "/";
        }
    }


    /// <summary>
    /// Loads the local data.json database.
    /// </summary>
    /// <returns>The parsed data.json values.</returns>
    public static Dictionary<string, string> GetDataJson()
    {
        var path = ClientProperties.PathToLocalDataJson ?? throw new FileNotFoundException("Can't find data.json.");

        return JsonSerializer.Deserialize(File.ReadAllText(path), DataJsonModelContext.Default.DictionaryStringString)
            ?? throw new InvalidDataException("Can't deserialize data.json.");
    }
}
