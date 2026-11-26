using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class PlaybackSeekTests
{
    [Fact]
    public void SeekRespectsVariableDurationsAndRepeatedCells()
    {
        var cursor = new PlaybackCursor([4, 4, 7]);
        var clock = new PlaybackClock(cursor, [100, 300, 50]);
        cursor.Play();
        clock.Seek(250);
        Assert.False(cursor.IsPlaying);
        Assert.Equal(1, cursor.Position);
        Assert.Equal(150, clock.ElapsedInFrame);
        Assert.Equal(250, clock.PositionMs);
        cursor.Play();
        Assert.True(clock.Advance(150, false));
        Assert.Equal(2, cursor.Position);
        Assert.Equal(400, clock.PositionMs);
    }

    [Fact]
    public void SeekClampsAndNaturalEndShowsTotal()
    {
        var cursor = new PlaybackCursor([0, 1]);
        var clock = new PlaybackClock(cursor, [100, 200]);
        clock.Seek(999);
        Assert.Equal(1, cursor.Position);
        Assert.Equal(300, clock.PositionMs, 8);
        clock.Seek(-5);
        Assert.Equal(0, cursor.Position);
        Assert.Equal(0, clock.PositionMs);
        cursor.Play();
        clock.Advance(300, false);
        Assert.True(cursor.HasEnded);
        Assert.Equal(300, clock.PositionMs);
    }
}
