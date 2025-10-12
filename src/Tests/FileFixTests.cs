using System.IO.Compression;
using Api.Common.Interface.ServerApiInterface;
using Common;
using Common.Client;
using Common.Client.FilesTools;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.Helpers;
using Downloader;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests that use instance data and should be run in a single thread
/// </summary>
[Collection("Sync")]
public sealed partial class FileFixTests
{
    private readonly string _testFixZip = Path.Combine(Helpers.RootFolder, "Resources", "test_fix.zip");
    private readonly string _testFixV2Zip = Path.Combine(Helpers.RootFolder, "Resources", "test_fix_v2.zip");
    private readonly string _testFixVariantZip = Path.Combine(Helpers.RootFolder, "Resources", "test_fix_variant.zip");

    private readonly GameEntity _gameEntity;
    private readonly FileFixEntity _fileFixEntity;
    private readonly FileFixEntity _fileFixVariantEntity;

    private readonly HttpClient _httpClient;
    private readonly DownloadService _downloadService;
    private readonly FixManager _fixManager;

    #region Test Preparations

    public FileFixTests()
    {
        ClearTempFolders();

        ClientProperties.IsDeveloperMode = true;

        _ = Directory.CreateDirectory(Helpers.TestFolder);
        Directory.SetCurrentDirectory(Helpers.TestFolder);

        _gameEntity = new()
        {
            Id = 1,
            Name = "test game",
            InstallDir = PrepareGameFolder(),
            Icon = string.Empty,
            BuildId = 1,
            TargetBuildId = 1,
        };

        _fileFixEntity = new()
        {
            Name = "test fix",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = "install folder",
            FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
            FilesToBackup = ["install folder\\file to backup.txt"],
            MD5 = "4E9DE15FC40592B26421E05882C2F6F7",
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _fileFixVariantEntity = new()
        {
            Name = "test fix",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixVariantZip,
            InstallFolder = "install folder",
            FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
            FilesToBackup = ["install folder\\file to backup.txt"],
            MD5 = "4E9DE15FC40592B26421E05882C2F6F7",
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        InstalledFixesProvider installedFixesProvider = new(new Mock<IGamesProvider>().Object, new Mock<ILogger>().Object);
        _ = installedFixesProvider.GetInstalledFixesListAsync();

        _httpClient = new();
        _downloadService = new();
        var configMock = new Mock<IConfigProvider>().Object;

        FileFixInstaller fileFixInstaller = new(
            configMock,
            new(new()),
            new FilesDownloader(
                new(),
                _downloadService,
                new Mock<ILogger>().Object
                ),
            new(),
            new Mock<ILogger>().Object,
                new ServerApiInterface(_httpClient, configMock),
                new Mock<IFixesProvider>().Object
            );

        FileFixUninstaller fileFixUninstaller = new();
        FileFixUpdater fileFixUpdater = new(fileFixInstaller, fileFixUninstaller);
        FileFixChecker fileFixChecker = new();

        _fixManager = new(
            fileFixInstaller,
            fileFixUninstaller,
            fileFixUpdater,
            fileFixChecker,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            installedFixesProvider,
            new Mock<ILogger>().Object
            );
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(Helpers.RootFolder);

        ClearTempFolders();

        foreach (var file in Directory.GetFiles(Helpers.RootFolder))
        {
            if (file.EndsWith(".zip"))
            {
                File.Delete(file);
            }
        }

        _httpClient.Dispose();
        _downloadService.Dispose();
    }

    private static void ClearTempFolders()
    {
        if (Directory.Exists(Helpers.TestFolder))
        {
            Directory.Delete(Helpers.TestFolder, true);
        }

        if (Directory.Exists(Path.Combine("C:", "Windows", "temp", "test_fix")))
        {
            Directory.Delete(Path.Combine("C:", "Windows", "temp", "test_fix"), true);
        }

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (Directory.Exists(Path.Combine(documents, "test_fix")))
        {
            Directory.Delete(Path.Combine(documents, "test_fix"), true);
        }
    }

    #endregion Test Preparations

    #region Tests

    /// <summary>
    /// Simple install and uninstall of a fix
    /// </summary>
    [Fact]
    public async Task InstallUninstallFix()
    {
        await InstallFixAsync(fixEntity: _fileFixEntity, variant: null, new()).ConfigureAwait(true);

        UninstallFix(_fileFixEntity);
    }

    /// <summary>
    /// Install and uninstall fix variant
    /// </summary>
    [Fact]
    public async Task InstallUninstallFixVariant()
    {
        await InstallFixAsync(fixEntity: _fileFixVariantEntity, variant: "variant2", new()).ConfigureAwait(true);

        UninstallFix(_fileFixVariantEntity);
    }

    /// <summary>
    /// Install, update and uninstall fix
    /// </summary>
    [Fact]
    public async Task UpdateFix()
    {
        await InstallFixAsync(fixEntity: _fileFixEntity, variant: null, new()).ConfigureAwait(true);

        await UpdateFixAsync(_gameEntity, _fileFixEntity).ConfigureAwait(true);

        UninstallFix(_fileFixEntity);
    }

    /// <summary>
    /// Install fix with incorrect MD5
    /// </summary>
    [Fact]
    public async Task InstallCompromisedFix()
    {
        FileFixEntity fixEntity = new()
        {
            Name = "test fix compromised",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = $"{Consts.BucketAddress}nointro/bsp_nointro.zip",
            MD5 = "badMD5",
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        var installResult = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, false, new()).ConfigureAwait(true);

        Assert.False(File.Exists(Consts.InstalledFile));
        Assert.Equal(ResultEnum.MD5Error, installResult.ResultEnum);
    }

    /// <summary>
    /// Install fix to a new folder
    /// </summary>
    [Fact]
    public async Task InstallFixToANewFolder()
    {
        FileFixEntity fixEntity = new()
        {
            Name = "test fix new folder",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = "new folder",
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("game", "new folder", "start game.exe")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": null,
  "FilesList": {
    "new folder{{Helpers.SeparatorForJson}}": null,
    "new folder{{Helpers.SeparatorForJson}}start game.exe": 446523244,
    "new folder{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}": null,
    "new folder{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}file.txt": 446523244
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "1.0"
}
""";

        Assert.Equal(installedExpected, installedActual);

        _ = _fixManager.UninstallFix(_gameEntity, fixEntity);

        Assert.False(Directory.Exists(Path.Combine("game", "new folder")));
    }

    /// <summary>
    /// Install fix to a new folder
    /// </summary>
    [Fact]
    public async Task UninstallFixAndKeepNonEmptyFolder()
    {
        FileFixEntity fixEntity = new()
        {
            Name = "test fix new folder",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = "new folder",
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("game", "new folder", "start game.exe")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": null,
  "FilesList": {
    "new folder{{Helpers.SeparatorForJson}}": null,
    "new folder{{Helpers.SeparatorForJson}}start game.exe": 446523244,
    "new folder{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}": null,
    "new folder{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}file.txt": 446523244
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "1.0"
}
""";

        Assert.Equal(installedExpected, installedActual);

        File.WriteAllText(Path.Combine("game", "new folder", "new file.txt"), "new file");

        Assert.True(File.Exists(Path.Combine("game", "new folder", "new file.txt")));

        _ = _fixManager.UninstallFix(_gameEntity, fixEntity);

        Assert.True(Directory.Exists(Path.Combine("game", "new folder")));
    }

    /// <summary>
    /// Install fix to an absolute path
    /// </summary>
    [Fact]
    public async Task InstallFixToAnAbsolutePathTemp()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        FileFixEntity fixEntity = new()
        {
            Name = "test fix absolute pack",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = Path.Combine("C:", "Windows", "temp", "test_fix"),
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("C:", "Windows", "temp", "test_fix", "start game.exe")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": null,
  "FilesList": {
    "C:{{Helpers.SeparatorForJson}}Windows{{Helpers.SeparatorForJson}}temp{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}": null,
    "C:{{Helpers.SeparatorForJson}}Windows{{Helpers.SeparatorForJson}}temp{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}start game.exe": 446523244,
    "C:{{Helpers.SeparatorForJson}}Windows{{Helpers.SeparatorForJson}}temp{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}": null,
    "C:{{Helpers.SeparatorForJson}}Windows{{Helpers.SeparatorForJson}}temp{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}file.txt": 446523244
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "1.0"
}
""";

        Assert.Equal(installedExpected, installedActual);

        _ = _fixManager.UninstallFix(_gameEntity, fixEntity);

        Assert.False(Directory.Exists(Path.Combine("C:", "Windows", "temp", "test_fix")));
    }

    /// <summary>
    /// Install fix to an absolute path
    /// </summary>
    [Fact]
    public async Task InstallFixToAnAbsolutePathDocuments()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        FileFixEntity fixEntity = new()
        {
            Name = "test fix absolute pack",
            Version = "1.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = Path.Combine("{documents}", "test_fix"),
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.True(File.Exists(Path.Combine(documents, "test_fix", "start game.exe")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": null,
  "FilesList": {
    "{documents}{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}": null,
    "{documents}{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}start game.exe": 446523244,
    "{documents}{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}": null,
    "{documents}{{Helpers.SeparatorForJson}}test_fix{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}file.txt": 446523244
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "1.0"
}
""";

        Assert.Equal(installedExpected, installedActual);

        _ = _fixManager.UninstallFix(_gameEntity, fixEntity);

        Assert.False(Directory.Exists(Path.Combine(documents, "test_fix")));
    }

    #endregion Tests

    #region Private Methods

    private async Task InstallFixAsync(FileFixEntity fixEntity, string? variant, CancellationToken cancellationToken)
    {
        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, variant, true, cancellationToken).ConfigureAwait(true);

        CheckNewFiles(fixEntity.Guid.ToString());

        //modify backed up file
        await File.WriteAllTextAsync(Path.Combine("game", "install folder", "file to backup.txt"), "modified", cancellationToken).ConfigureAwait(true);
    }

    private async Task UpdateFixAsync(GameEntity gameEntity, FileFixEntity fileFix)
    {
        FileFixEntity newFileFix = new()
        {
            Name = "test fix",
            Version = "2.0",
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixV2Zip,
            InstallFolder = "install folder",
            FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
            FilesToBackup = ["install folder\\file to backup.txt"],
            InstalledFix = fileFix.InstalledFix,
            SupportedOSes = OSEnum.Windows | OSEnum.Linux
        };

        _ = await _fixManager.UpdateFixAsync(gameEntity, newFileFix, null, true, new()).ConfigureAwait(true);

        fileFix.InstalledFix = newFileFix.InstalledFix;

        CheckUpdatedFiles(newFileFix.Guid.ToString());
    }

    private void UninstallFix(FileFixEntity fixEntity)
    {
        _ = _fixManager.UninstallFix(_gameEntity, fixEntity);

        CheckOriginalFiles();
    }

    private void CheckOriginalFiles()
    {
        //check original files
        var exeExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.True(exeExists);

        var fi = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.Equal("original", fi);

        var fileExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.True(fileExists);

        var fi2 = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.Equal("original", fi2);

        //check deleted files
        var fileToDeleteExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to delete.txt");
        Assert.True(fileToDeleteExists);

        var fileToDeleteSubExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file to delete in subfolder.txt");
        Assert.True(fileToDeleteSubExists);

        var fileToDeleteParentExists = File.Exists($"game{Path.DirectorySeparatorChar}file to delete in parent folder.txt");
        Assert.True(fileToDeleteParentExists);

        //check backed up files
        var fileToBackupExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.True(fileToBackupExists);

        var fi3 = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.Equal("original", fi3);
    }

    private void CheckNewFiles(string fixGuid)
    {
        //check replaced files
        var exeExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.True(exeExists);

        var fi = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.Equal("fix_v1", fi);

        var fileExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.True(fileExists);

        var fi2 = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.Equal("fix_v1", fi2);

        //check deleted files
        var fileToDeleteExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to delete.txt");
        Assert.False(fileToDeleteExists);

        var fileToDeleteSubExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file to delete in subfolder.txt");
        Assert.False(fileToDeleteSubExists);

        var fileToDeleteParentExists = File.Exists($"game{Path.DirectorySeparatorChar}file to delete in parent folder.txt");
        Assert.False(fileToDeleteParentExists);

        //check backed up files
        var fileToBackupExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.True(fileToBackupExists);

        var backedUpFileExists = File.Exists($"game{Path.DirectorySeparatorChar}{Consts.BackupFolder}{Path.DirectorySeparatorChar}test_fix{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.True(backedUpFileExists);

        //check installed.xml
        var textActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixGuid + ".json"));
        var textExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": "test_fix",
  "FilesList": {
    "install folder{{Helpers.SeparatorForJson}}start game.exe": 446523244,
    "install folder{{Helpers.SeparatorForJson}}subfolder{{Helpers.SeparatorForJson}}file.txt": 446523244
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "1.0"
}
""";

        Assert.Equal(textExpected, textActual);
    }

    private void CheckUpdatedFiles(string fixGuid)
    {
        //check replaced files
        var exeExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.True(exeExists);

        var fi = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}start game.exe");
        Assert.Equal("fix_v2", fi);

        var fileExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.True(fileExists);

        var fi2 = File.ReadAllText($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file.txt");
        Assert.Equal("original", fi2);

        //check deleted files
        var fileToDeleteExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to delete.txt");
        Assert.False(fileToDeleteExists);

        var fileToDeleteSubExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}file to delete in subfolder.txt");
        Assert.False(fileToDeleteSubExists);

        var fileToDeleteParentExists = File.Exists($"game{Path.DirectorySeparatorChar}file to delete in parent folder.txt");
        Assert.False(fileToDeleteParentExists);

        //check backed up files
        var fileToBackupExists = File.Exists($"game{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.True(fileToBackupExists);

        var backedUpFileExists = File.Exists($"game{Path.DirectorySeparatorChar}{Consts.BackupFolder}{Path.DirectorySeparatorChar}test_fix{Path.DirectorySeparatorChar}install folder{Path.DirectorySeparatorChar}file to backup.txt");
        Assert.True(backedUpFileExists);

        //check installed.xml
        var textActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixGuid + ".json"));
        var textExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": "test_fix",
  "FilesList": {
    "install folder{{Helpers.SeparatorForJson}}start game.exe": 2207528662
  },
  "InstalledSharedFix": null,
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "2.0"
}
""";

        Assert.Equal(textExpected, textActual);
    }

    private string PrepareGameFolder()
    {
        var gameFolder = Path.Combine(Helpers.TestFolder, "game");

        ZipFile.ExtractToDirectory(Path.Combine(Helpers.RootFolder, "Resources", "test_game.zip"), gameFolder);

        return gameFolder;
    }

    #endregion Private Methods
}

