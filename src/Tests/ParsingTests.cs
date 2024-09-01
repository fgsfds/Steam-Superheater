using Common.Client;
using Common.Client.DI;
using Common.Client.Providers;
using Common.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Tests;

/// <summary>
/// Tests that can be run in parallel
/// </summary>
[Collection("Sync")]
public sealed class ParsingTests
{
    public ParsingTests()
    {
        BindingsManager.Reset();
        var container = BindingsManager.Instance;
        container.AddTransient<GamesProvider>();
        container.AddTransient<SteamTools>();
        container.AddTransient<Logger>();
    }

    [Fact]
    public void GetGameEntityFromAcfTest()
    {
        var steamTools = BindingsManager.Provider.GetRequiredService<SteamTools>();
        var logger = BindingsManager.Provider.GetRequiredService<Logger>();

        var method = typeof(GamesProvider).GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var result = method.Invoke(new GamesProvider(steamTools, logger), [Path.Combine("Resources", "test_manifest.acf")]);

        Assert.NotNull(result);
        Assert.IsType<GameEntity>(result);

        var gameEntity = (GameEntity)result;

        Assert.Equal("DOOM (1993)", gameEntity.Name);
        Assert.Equal(2280, gameEntity.Id);
        Assert.Equal($"Resources{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}Ultimate Doom{Path.DirectorySeparatorChar}", gameEntity.InstallDir);
    }
}

