using Common.Client;
using Common.Client.FixTools.FileFix;

namespace Tests.Unit.Sequential;

public sealed class ProtonToolsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-proton-" + Guid.NewGuid().ToString("N"));
    public ProtonToolsTests() => Directory.CreateDirectory(_root);
    [Theory]
    [InlineData(".local/share/Steam")]
    [InlineData(".steam/steam")]
    [InlineData(".var/app/com.valvesoftware.Steam/.local/share/Steam")]
    [InlineData("snap/steam/common/.local/share/Steam")]
    public void FindsSupportedLayouts(string layout)
    {
        var path = CreateRegistry(Path.Combine(_root, layout, "steamapps"));
        Assert.Equal(path, ProtonTools.FindUserRegistry(Path.Combine(_root, "other"), 42, _root));
    }
    [Fact]
    public void SecondaryLibraryWinsOverDefaultPrefix()
    {
        _ = CreateRegistry(Path.Combine(_root, ".local/share/Steam/steamapps"));
        var library = Path.Combine(_root, "secondary", "steamapps");
        var expected = CreateRegistry(library);
        Assert.Equal(expected, ProtonTools.FindUserRegistry(Path.Combine(library, "common", "game"), 42, _root));
    }
    [Fact]
    public async Task MissingSectionIsCreatedAndExistingChoicesArePreserved()
    {
        var path = CreateRegistry(Path.Combine(_root, "steamapps"));
        var added = await ProtonTools.AddOverridesAsync(path, ["test", "test"]);
        Assert.Single(added);
        Assert.Empty(await ProtonTools.AddOverridesAsync(path, ["test"]));
        FileFixUninstaller.RemoveWineDllOverridesFromFile(path, added);
        Assert.DoesNotContain("\"test\"=", await File.ReadAllTextAsync(path));
        await File.WriteAllTextAsync(path, "[Software\\\\Wine\\\\DllOverrides]\n\"test\"=\"builtin\"\n");
        Assert.Empty(await ProtonTools.AddOverridesAsync(path, ["test"]));
        Assert.Contains("builtin", await File.ReadAllTextAsync(path));
    }
    private static string CreateRegistry(string steamapps)
    {
        var directory = Path.Combine(steamapps, "compatdata", "42", "pfx");
        _ = Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "user.reg");
        File.WriteAllText(path, "WINE REGISTRY Version 2\n");
        return path;
    }
    public void Dispose() => Directory.Delete(_root, true);
}
