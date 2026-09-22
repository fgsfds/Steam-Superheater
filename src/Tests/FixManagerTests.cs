using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes.RegistryFix;
using Common.Axiom.Enums;
using Common.Client.FixTools;
using Common.Client.FixTools.RegistryFix;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for <see cref="FixManager"/>
/// </summary>
public sealed class FixManagerTests
{
    private readonly FixManager _fixManager;
    private readonly GameEntity _game;

    public FixManagerTests()
    {
        var logger = new Mock<ILogger>().Object;

        RegistryFixInstaller registryFixInstaller = new(logger);
        RegistryFixUninstaller registryFixUninstaller = new();
        RegistryFixUpdater registryFixUpdater = new(registryFixInstaller, registryFixUninstaller);

        _fixManager = new(
            null!,
            null!,
            null!,
            null!,
            registryFixInstaller,
            registryFixUninstaller,
            registryFixUpdater,
            null!,
            null!,
            null!,
            null!,
            logger
            );

        _game = new()
        {
            Id = 1,
            Name = "test game",
            InstallDir = Path.GetTempPath(),
            Icon = string.Empty,
            BuildId = 1,
            TargetBuildId = 1,
        };
    }

    /// <summary>
    /// Install returns an error result instead of throwing when the installer fails
    /// </summary>
    [Fact]
    public async Task InstallFixReturnsErrorWhenInstallerThrows()
    {
        var result = await _fixManager.InstallFixAsync(_game, CreateRegistryFix(), null, false, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ResultEnum.Error, result.ResultEnum);
    }

    /// <summary>
    /// Update returns an error result instead of throwing when the updater fails
    /// </summary>
    [Fact]
    public async Task UpdateFixReturnsErrorWhenUpdaterThrows()
    {
        var fix = CreateRegistryFix();
        fix.InstalledFix = new RegistryInstalledFixEntity()
        {
            GameId = _game.Id,
            Guid = fix.Guid,
            Version = "1.0",
            Entries = [],
        };

        var result = await _fixManager.UpdateFixAsync(_game, fix, null, false, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ResultEnum.Error, result.ResultEnum);
    }

    private static RegistryFixEntity CreateRegistryFix()
    {
        return new(true)
        {
            Name = "test registry fix",
            Entries =
            [
                new RegistryEntry()
                {
                    Key = @"HKEY_CURRENT_USER\Software\SuperheaterTests\DoesNotExist",
                    ValueName = "Test",
                    NewValueData = "notanumber",
                    ValueType = RegistryValueTypeEnum.Dword,
                },
            ],
        };
    }
}
