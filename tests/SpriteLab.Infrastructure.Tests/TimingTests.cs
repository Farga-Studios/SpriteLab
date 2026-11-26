using SpriteLab.Core;
using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class TimingTests
{
    [Fact]
    public void DefaultsAndOverridesArePerOccurrence()
    {
        var track = new ViewerTrack("front", [0, 0, 1], 10, [null, 300, null]);
        Assert.Equal(100.0, track.DurationAt(0));
        Assert.Equal(300.0, track.DurationAt(1));
        Assert.Equal(100.0, track.DurationAt(2));
        Assert.Equal(125.0, new ViewerTrack("none", [0]).DurationAt(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void InvalidFpsRejected(int fps)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreviewTiming.Duration(fps, null));
    }

    [Fact]
    public void InvalidOverrideAndLengthRejected()
    {
        var sheet = new SheetEntry(Guid.NewGuid(), "sheet.png", new string('A', 64), 96, 48, 48, 48);
        foreach (var durations in new int?[][] { [0, null], [null], [null, 60001] })
            Assert.Throws<InvalidDataException>(() => ViewerClip.ValidateAll(
                [new(Guid.NewGuid(), "Walk", sheet.Id, [0, 1], [new("front", [0, 1], 8, durations)])], [sheet]));
    }

    [Fact]
    public async Task TimingRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sheet = new ViewerSheet(Guid.NewGuid(), "sheet.png", Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII="), 1, 1);
            var clip = new ViewerClip(Guid.NewGuid(), "Idle", sheet.Id, [0, 0], [new("front", [0, 0], 12, [null, 400])]);
            await ViewerWorkspace.SaveAsync(root, new ViewerProject(Guid.NewGuid(), "Test", [sheet], [clip]), null, CancellationToken.None);
            var actual = Assert.Single(Assert.Single((await ViewerWorkspace.LoadAsync(root, CancellationToken.None)).Project.Clips!).EffectiveTracks);
            Assert.Equal((int?)12, actual.Fps);
            Assert.Equal(new int?[] { null, 400 }, actual.DurationsMs);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
