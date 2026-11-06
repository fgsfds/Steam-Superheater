using Common.Client;

namespace Tests.Unit.Sequential;

/// <summary>Resolves the documents placeholder without writing to user documents.</summary>
public sealed class DocumentPathTests
{
    [Fact]
    public void DocumentsPlaceholderResolvesWithoutGamePrefix()
    {
        var relative = Path.Combine("{documents}", "example", "file.txt");
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documents))
        {
            _ = Assert.Throws<DirectoryNotFoundException>(() => ClientHelpers.GetFullPath(Helpers.GameDir, relative));
        }
        else
        {
            Assert.Equal(Path.Combine(documents, "example", "file.txt"), ClientHelpers.GetFullPath(Helpers.GameDir, relative));
        }
    }
}
