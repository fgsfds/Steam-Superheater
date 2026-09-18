using System.Reflection;
using System.Text.Json;
using Common.Axiom.Entities;
using Common.Client;

namespace Tests;

public static class Helpers
{
    private const string TestTempFolder = "test_temp";
    public const string GameExe = "game exe.exe";
    public const string RegKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers_test";

    public static string GameDir => Path.Combine(TestFolder, "game_dir");

    public static string RootFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    public static string TestFolder => Path.Combine(RootFolder, TestTempFolder);

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
