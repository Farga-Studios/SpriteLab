using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class DirectionTests
{
    private static SheetEntry Sheet() => new(Guid.NewGuid(), "sheet.png", new string('A', 64), 96, 96, 48, 48);

    [Fact]
    public void OldClipHasImplicitTrack()
    {
        var sheet = Sheet();
        var clip = new ViewerClip(Guid.NewGuid(), "Walk", sheet.Id, [0, 1]);
        var track = Assert.Single(clip.EffectiveTracks);
        Assert.Equal("none", track.Direction);
        Assert.Equal(new[] { 0, 1 }, track.Cells);
    }

    [Fact]
    public void DifferentTrackLengthsAreValid()
    {
        var sheet = Sheet();
        ViewerClip.ValidateAll([new(Guid.NewGuid(), "Walk", sheet.Id, [0, 1],
            [new("front", [0, 1]), new("custom", [3])])], [sheet]);
    }

    [Fact]
    public void DuplicateDirectionAndMissingCellAreRejected()
    {
        var sheet = Sheet();
        Assert.Throws<InvalidDataException>(() => ViewerClip.ValidateAll(
            [new(Guid.NewGuid(), "Walk", sheet.Id, [0], [new("front", [0]), new("front", [1])])], [sheet]));
        Assert.Throws<InvalidDataException>(() => ViewerClip.ValidateAll(
            [new(Guid.NewGuid(), "Walk", sheet.Id, [0], [new("front", [0]), new("back", [4])])], [sheet]));
    }

    [Fact]
    public async Task DirectionTracksRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sheet = new ViewerSheet(Guid.NewGuid(), "sheet.png", Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII="), 1, 1);
            var clip = new ViewerClip(Guid.NewGuid(), "Walk", sheet.Id, [0], [new("front", [0]), new("back", [0, 0])]);
            await ViewerWorkspace.SaveAsync(root, new ViewerProject(Guid.NewGuid(), "Test", [sheet], [clip]), null, CancellationToken.None);
            var actual = Assert.Single((await ViewerWorkspace.LoadAsync(root, CancellationToken.None)).Project.Clips!);
            Assert.Equal("back", actual.EffectiveTracks[1].Direction);
            Assert.Equal(new[] { 0, 0 }, actual.EffectiveTracks[1].Cells);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
