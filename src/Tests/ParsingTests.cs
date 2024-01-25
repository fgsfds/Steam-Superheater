using Common.Entities;
using Common.Providers;
using System.Reflection;

namespace Tests
{
    /// <summary>
    /// Tests that can be run in parallel
    /// </summary>
    [Collection("Sync")]
    public sealed class ParsingTests
    {
        [Fact]
        public void GetGameEntityFromAcfTest()
        {
            var method = typeof(GamesProvider).GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            var result = method.Invoke(new GamesProvider(), [Path.Combine("Resources", "test_manifest.acf")]);

            Assert.NotNull(result);
            Assert.IsType<GameEntity>(result);

            var gameEntity = (GameEntity)result;

            Assert.Equal("DOOM (1993)", gameEntity.Name);
            Assert.Equal(2280, gameEntity.Id);
            Assert.Equal($"Resources{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}Ultimate Doom{Path.DirectorySeparatorChar}", gameEntity.InstallDir);
        }
    }
}