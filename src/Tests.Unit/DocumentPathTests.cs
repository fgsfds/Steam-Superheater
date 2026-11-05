using Common.Client;

namespace Tests.Unit;

/// <summary>Checks missing-folder handling independently of the host profile.</summary>
public sealed class DocumentPathTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-documents")]
    public void MissingOrRelativeDocumentsDirectoryIsRejected(string documents)
    {
        var error = Assert.Throws<DirectoryNotFoundException>(() => ClientHelpers.ResolveDocumentsPath("{documents}/example/file.txt", documents));
        Assert.Contains("Documents folder is unavailable", error.Message);
    }

    [Fact]
    public void AvailableDocumentsDirectoryResolvesPlaceholder()
    {
        var documents = Path.GetFullPath(Path.Combine(Path.DirectorySeparatorChar.ToString(), "fixture-documents"));
        var folder = Path.Combine("{documents}", "example", "file.txt");
        Assert.Equal(Path.Combine(documents, "example", "file.txt"), ClientHelpers.ResolveDocumentsPath(folder, documents));
    }
}
