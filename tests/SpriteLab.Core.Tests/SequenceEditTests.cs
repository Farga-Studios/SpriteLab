using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class SequenceEditTests
{
    [Fact]
    public void SequenceSupportsRepeatAndOrder()
    {
        Assert.Equal(new[] { 2, 0, 2 }, SequenceEdit.Parse("3, 1, 3", 3));
        Assert.Throws<ArgumentException>(() => SequenceEdit.Parse("0", 3));
    }
    [Fact]
    public void TimingFollowsOccurrenceOrder()
    {
        Assert.Equal(new int?[] { 200, 100, 300, null },
            SequenceEdit.Remap([0, 1, 0], [100, 200, 300], [1, 0, 0, 2]));
    }
    [Fact]
    public void CoreRetainsTiming()
    {
        var id = Guid.NewGuid();
        var track = new AnimationTrack("front", [id, id], 10, [null, 250]);
        Assert.Equal(100.0, track.DurationAt(0));
        Assert.Equal(250.0, track.DurationAt(1));
    }
}
