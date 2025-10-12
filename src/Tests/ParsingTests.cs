using System.Reflection;
using System.Text.Json;
using Common.Client;
using Common.Client.Providers;
using Common.Entities;
using Common.Entities.Fixes;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests that can be run in parallel
/// </summary>
[Collection("Sync")]
public sealed class ParsingTests
{
    [Fact]
    public void GetGameEntityFromAcfTest()
    {
        Mock<ILogger> loggerMock = new();
        SteamTools steamTools = new(loggerMock.Object);

        var method = typeof(GamesProvider).GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var result = method.Invoke(new GamesProvider(steamTools, loggerMock.Object), [Path.Combine("Resources", "test_manifest.acf")]);

        Assert.NotNull(result);
        _ = Assert.IsType<GameEntity>(result);

        var gameEntity = (GameEntity)result;

        Assert.Equal("DOOM (1993)", gameEntity.Name);
        Assert.Equal(2280, gameEntity.Id);
        Assert.Equal($"Resources{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}Ultimate Doom{Path.DirectorySeparatorChar}", gameEntity.InstallDir);
        Assert.Equal((uint)5619887, gameEntity.BuildId);
        Assert.Equal((uint)7619887, gameEntity.TargetBuildId);
    }
}

/// <summary>
/// Tests that can be run in parallel
/// </summary>
[Collection("Sync")]
public sealed class InstalledFileFixBackwardsCompatibilityTest
{
    [Fact]
    public void GetGameEntityFromAcfTest()
    {
        var installedFixJson = $$"""
{
  "$type": "FileFix",
  "BackupFolder": "test_fix",
  "FilesList": [
    "install folder{{Helpers.SeparatorForJson}}start game.exe"
  ],
  "InstalledSharedFix": {
    "BackupFolder": null,
    "FilesList": [
      "shared install folder{{Helpers.SeparatorForJson}}",
      "shared install folder{{Helpers.SeparatorForJson}}shared fix file.txt"
    ],
    "InstalledSharedFix": null,
    "WineDllOverrides": null,
    "BuildId": 1,
    "GameId": 1,
    "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa6",
    "Version": "1.0"
  },
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "2.0"
}
""";

        var addonsJson = JsonSerializer.Deserialize(installedFixJson, InstalledFixesListContext.Default.BaseInstalledFixEntity);

        var jsonActual = JsonSerializer.Serialize(addonsJson, InstalledFixesListContext.Default.BaseInstalledFixEntity);
        var jsonExpected = $$"""
{
  "$type": "FileFix",
  "BackupFolder": "test_fix",
  "FilesList": {
    "install folder{{Helpers.SeparatorForJson}}start game.exe": null
  },
  "InstalledSharedFix": {
    "BackupFolder": null,
    "FilesList": {
      "shared install folder{{Helpers.SeparatorForJson}}": null,
      "shared install folder{{Helpers.SeparatorForJson}}shared fix file.txt": null
    },
    "InstalledSharedFix": null,
    "WineDllOverrides": null,
    "BuildId": 1,
    "GameId": 1,
    "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa6",
    "Version": "1.0"
  },
  "WineDllOverrides": null,
  "BuildId": 1,
  "GameId": 1,
  "Guid": "c0650f19-f670-4f8a-8545-70f6c5171fa5",
  "Version": "2.0"
}
""";

        Assert.Equal(jsonExpected, jsonActual);
    }
}
