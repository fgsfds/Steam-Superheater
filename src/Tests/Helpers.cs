using System.Reflection;

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
}
