using SpriteLab.Infrastructure;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class SaveAsTests
{
    [Fact]
    public void FolderComparisonNormalizesTrailingSeparator()
    {
        var root = Path.GetTempPath();
        Assert.True(ViewerWorkspace.SameFolder(root, Path.Combine(root, ".")));
        Assert.False(ViewerWorkspace.SameFolder(root, Path.Combine(root, "other")));
    }

    [Fact]
    public async Task SavingCopyDoesNotAlterOriginal()
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
        var original = Path.Combine(root, "original");
        var copy = Path.Combine(root, "copy");
        try
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=");
            var sheet = new ViewerSheet(Guid.NewGuid(), "test.png", png, 1, 1);
            var project = new ViewerProject(Guid.NewGuid(), "Original", [sheet],
                [new ViewerClip(Guid.NewGuid(), "Idle", sheet.Id, [0, 0])]);
            await ViewerWorkspace.SaveAsync(original, project, null, CancellationToken.None);
            var before = await File.ReadAllBytesAsync(Path.Combine(original, ProjectStore.ManifestName));
            var cloned = project with { Id = Guid.NewGuid(), Name = "Copy" };
            await ViewerWorkspace.SaveAsync(copy, cloned, null, CancellationToken.None);
            Assert.Equal(before, await File.ReadAllBytesAsync(Path.Combine(original, ProjectStore.ManifestName)));
            var loaded = await ViewerWorkspace.LoadAsync(copy, CancellationToken.None);
            Assert.Equal(cloned.Id, loaded.Project.Id);
            Assert.Equal(png, Assert.Single(loaded.Project.Sheets).Png);
            Assert.Equal(new[] { 0, 0 }, Assert.Single(loaded.Project.Clips!).Cells);
            await Assert.ThrowsAsync<IOException>(() => ViewerWorkspace.SaveAsync(copy, cloned, null, CancellationToken.None));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
