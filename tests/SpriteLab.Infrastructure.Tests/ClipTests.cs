using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class ClipTests
{
    private static SheetEntry Sheet() => new(Guid.NewGuid(), "sheet.png", new string('A', 64), 96, 48, 48, 48);

    [Fact]
    public void AllowsOrderedRepeatedCells()
    {
        var sheet = Sheet();
        ViewerClip.ValidateAll([new(Guid.NewGuid(), "Walk", sheet.Id, [1, 0, 1])], [sheet]);
    }

    [Fact]
    public void RejectsMissingSheetAndInvalidCell()
    {
        var sheet = Sheet();
        Assert.Throws<InvalidDataException>(() => ViewerClip.ValidateAll([new(Guid.NewGuid(), "Walk", Guid.NewGuid(), [0])], [sheet]));
        Assert.Throws<InvalidDataException>(() => ViewerClip.ValidateAll([new(Guid.NewGuid(), "Walk", sheet.Id, [2])], [sheet]));
    }

    [Fact]
    public async Task ClipsRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sheet = new ViewerSheet(Guid.NewGuid(), "sheet.png", Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII="), 1, 1);
            var clip = new ViewerClip(Guid.NewGuid(), "Idle", sheet.Id, [0, 0]);
            var project = new ViewerProject(Guid.NewGuid(), "Test", [sheet], [clip]);
            await ViewerWorkspace.SaveAsync(root, project, null, CancellationToken.None);
            var loaded = await ViewerWorkspace.LoadAsync(root, CancellationToken.None);
            var actual = Assert.Single(loaded.Project.Clips!);
            Assert.Equal(clip.Id, actual.Id);
            Assert.Equal(clip.Cells, actual.Cells);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
