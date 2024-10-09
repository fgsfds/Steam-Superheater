using Common.Client;
using Common.Client.Providers;
using Common.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

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
    }
}

