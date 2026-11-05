using Common.Client;

namespace Tests.Unit.Sequential;

/// <summary>Resolves the documents placeholder without writing to user documents.</summary>
public sealed class DocumentPathTests
{
    [Fact]
    public void DocumentsPlaceholderResolvesWithoutGamePrefix()
    {
        var relative = Path.Combine("{documents}", "example", "file.txt");
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "example", "file.txt"), ClientHelpers.GetFullPath(Helpers.GameDir, relative));
    }
}
