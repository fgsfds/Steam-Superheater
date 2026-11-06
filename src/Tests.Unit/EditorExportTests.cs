using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;

namespace Tests.Unit;

public sealed class EditorExportTests
{
    [Theory]
    [InlineData("file:///C:/exports/my%20file%20%C3%A9.json")]
    [InlineData("file:///tmp/my%20file%20%C3%A9.json")]
    public void LocalExportPathsAreDecoded(string value)
    {
        var result = UriHelper.GetLocalFilePath(new Uri(value));
        Assert.Contains("my file é.json", result);
        Assert.DoesNotContain("%20", result);
    }

    [Theory]
    [InlineData("https://example.com/file.json")]
    [InlineData("relative.json")]
    public void NonLocalExportIsRejected(string value)
    {
        _ = Assert.Throws<ArgumentException>(() => UriHelper.GetLocalFilePath(new Uri(value, UriKind.RelativeOrAbsolute)));
    }

    [Fact]
    public void ClonePreservesMetadataWithoutSharingCollections()
    {
        var original = FileFixEntity.CreateBlank();
        original.Changelog = "change";
        original.Notes = "editor note";
        original.IsDisabled = true;
        original.Tags = ["tag"];
        original.FilesToDelete = ["file"];
        original.SharedFix = FileFixEntity.CreateBlank();
        original.SharedFix.Changelog = "shared change";
        var clone = original.Clone();
        Assert.Equal(original.Changelog, clone.Changelog);
        Assert.Equal(original.Notes, clone.Notes);
        Assert.True(clone.IsDisabled);
        Assert.Equal("shared change", clone.SharedFix!.Changelog);
        Assert.NotSame(original.SharedFix, clone.SharedFix);
        clone.Tags!.Add("new");
        Assert.Single(original.Tags);
        Assert.NotSame(original.FilesToDelete, clone.FilesToDelete);
    }
}
