using System.Text.Json;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Client;

namespace Tests.Unit.Sequential;

/// <summary>Checks the checked-in catalog without contacting its services.</summary>
public sealed class LocalCatalogTests
{
    [Fact]
    public void LocalCatalogsDeserializeAndFixIdsAreUnique()
    {
        var dataPath = ClientProperties.PathToLocalDataJson;
        Assert.NotNull(dataPath);
        var directory = Path.GetDirectoryName(dataPath)!;
        var fixes = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(directory, "fixes.json")), FixesListContext.Default.ListFixesList);
        var news = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(directory, "news.json")), NewsListEntityContext.Default.ListNewsEntity);
        Assert.NotNull(fixes);
        Assert.NotEmpty(fixes);
        Assert.NotNull(news);
        var entries = fixes.SelectMany(x => x.Fixes).ToList();
        Assert.All(entries, fix => Assert.NotEqual(Guid.Empty, fix.Guid));
        Assert.Equal(entries.Count, entries.Select(x => x.Guid).Distinct().Count());
        Assert.NotEmpty(Helpers.GetDataJson());
    }
}
