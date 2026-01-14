namespace Common.Axiom.Helpers;

public static class Consts
{
    public const string ConfigFile = "config.json";

    public const string InstalledFile = "installed.json";

    public const string PCGamingWikiUrl = "https://pcgamingwiki.com/api/appid.php?appid=";

    public const string BackupFolder = ".superheater";

    public const string UpdateFile = ".update";

    public const string UpdateFolder = "update";

    public const string S3Endpoint = "https://s3.firstvds.ru/";

    /// <summary>
    /// Path to the files repository
    /// </summary>
    public const string BucketAddress = $"{S3Endpoint}superheater/";

    /// <summary>
    /// Path to the uploads repository
    /// </summary>
    public const string UploadsFolder = $"{S3Endpoint}uploads/superheater/";

    public const string Hosts = @"C:\Windows\System32\drivers\etc\hosts";

    public const string All = "All";

    public const string UpdateAvailable = "Update available";

    public const string WindowsOnly = "Windows Only";

    public const string LinuxOnly = "Linux Only";

    public const string AllSupported = "All supported OSes";

    public const string FixesJsonUrl = "https://raw.githubusercontent.com/fgsfds/Steam-Superheater/refs/heads/master/db/fixes.json";

    public const string NewsJsonUrl = "https://raw.githubusercontent.com/fgsfds/Steam-Superheater/refs/heads/master/db/news.json";
}

