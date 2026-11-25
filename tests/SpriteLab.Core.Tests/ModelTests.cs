using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class ModelTests
{
    private static Frame NewFrame(int duration = 100) =>
        new(Guid.NewGuid(), new PixelRect(0, 0, 48, 32),
            new PixelPoint(24, 30), new PixelPoint(2, -3), duration);

    private static SpriteAsset Asset(params Frame[] frames) =>
        new(Guid.NewGuid(), "Synthetic", Guid.NewGuid(),
            new PixelSize(96, 64), new PixelSize(48, 32), frames);

    [Fact]
    public void DrawPositionUsesAnchorAndOffset() =>
        Assert.Equal(new PixelPoint(78, 167), NewFrame().DrawPosition(new PixelPoint(100, 200)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FrameRejectsNonPositiveDuration(int duration) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewFrame(duration));

    [Fact]
    public void FrameRejectsAnchorOutsideFrame() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Frame(Guid.NewGuid(),
            new PixelRect(0, 0, 48, 32), new PixelPoint(49, 32), default, 100));

    [Fact]
    public void AssetRejectsDuplicateAndOutOfBoundsFrames()
    {
        var frame = NewFrame();
        Assert.Throws<ArgumentException>(() => Asset(frame, frame));
        var outside = new Frame(Guid.NewGuid(), new PixelRect(80, 0, 48, 32), default, default, 100);
        Assert.Throws<ArgumentException>(() => Asset(outside));
    }

    [Fact]
    public void AssetCopiesInputCollection()
    {
        var input = new[] { NewFrame() };
        var original = input[0];
        var asset = Asset(input);
        input[0] = NewFrame();
        Assert.Same(original, asset.Frames[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<Frame>)asset.Frames)[0] = input[0]);
    }

    [Fact]
    public void ProjectAcceptsSharedFramesCustomDirectionsAndDifferentDurations()
    {
        var a = NewFrame(80);
        var b = NewFrame(250);
        var clip = new AnimationClip(Guid.NewGuid(), "Walk", new[] {
            new AnimationTrack("custom", new[] { a.Id, b.Id, a.Id }),
            new AnimationTrack("none", new[] { b.Id })
        });
        var project = new SpriteProject(Guid.NewGuid(), "Demo", new[] { Asset(a, b) },
            new[] { clip }, new[] { new FrameMarker(Guid.NewGuid(), b.Id, "event") });
        Assert.Equal(3, project.Clips[0].Tracks[0].FrameIds.Count);
        Assert.Single(project.Clips[0].Tracks[1].FrameIds);
        Assert.Equal(250, project.Assets[0].Frames[1].DurationMs);
        Assert.Equal(b.Id, project.Markers[0].FrameId);
    }

    [Fact]
    public void TrackCopiesInputAndRejectsEmptySequence()
    {
        var id = Guid.NewGuid();
        var ids = new[] { id };
        var track = new AnimationTrack("front", ids);
        ids[0] = Guid.NewGuid();
        Assert.Equal(id, track.FrameIds[0]);
        Assert.Throws<ArgumentException>(() => new AnimationTrack("front", Array.Empty<Guid>()));
    }

    [Fact]
    public void ClipRejectsRepeatedDirection()
    {
        var track = new AnimationTrack("front", new[] { Guid.NewGuid() });
        Assert.Throws<ArgumentException>(() => new AnimationClip(Guid.NewGuid(), "Attack", new[] { track, track }));
    }

    [Fact]
    public void ProjectRejectsMissingFrameReferencesAndMarkers()
    {
        var missing = Guid.NewGuid();
        var clip = new AnimationClip(Guid.NewGuid(), "Attack",
            new[] { new AnimationTrack("front", new[] { missing }) });
        var assets = new[] { Asset(NewFrame()) };
        Assert.Throws<ArgumentException>(() => new SpriteProject(Guid.NewGuid(), "Demo", assets, new[] { clip }));
        Assert.Throws<ArgumentException>(() => new SpriteProject(Guid.NewGuid(), "Demo", assets,
            Array.Empty<AnimationClip>(), new[] { new FrameMarker(Guid.NewGuid(), missing, "event") }));
    }

    [Fact]
    public void ProjectRejectsDuplicateFramesAcrossAssets()
    {
        var frame = NewFrame();
        Assert.Throws<ArgumentException>(() => new SpriteProject(Guid.NewGuid(), "Demo",
            new[] { Asset(frame), Asset(frame) }, Array.Empty<AnimationClip>()));
    }
}
