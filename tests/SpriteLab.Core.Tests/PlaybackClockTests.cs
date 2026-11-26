using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class PlaybackClockTests
{
    [Fact]
    public void VariableTimingAndDelayedTick()
    {
        var cursor = new PlaybackCursor([0, 1, 2]);
        var clock = new PlaybackClock(cursor, [100, 250, 50]);
        cursor.Play();
        Assert.True(clock.Advance(375, false));
        Assert.Equal(2, cursor.Position);
        Assert.Equal(25, clock.ElapsedInFrame);
        Assert.False(clock.Advance(25, false));
        Assert.True(cursor.HasEnded);
        Assert.False(cursor.IsPlaying);
    }

    [Fact]
    public void LargeLoopSkipsCyclesWithoutLosingRemainder()
    {
        var cursor = new PlaybackCursor([0, 1, 2]);
        cursor.Step(1);
        var clock = new PlaybackClock(cursor, [100, 250, 50]);
        cursor.Play();
        Assert.True(clock.Advance(400_280, true));
        Assert.Equal(2, cursor.Position);
        Assert.Equal(30, clock.ElapsedInFrame);
    }

    [Fact]
    public void PauseOnLastFrameIsNotCompletion()
    {
        var cursor = new PlaybackCursor([0, 1]);
        var clock = new PlaybackClock(cursor, [100, 200]);
        cursor.Play();
        clock.Advance(150, false);
        cursor.Pause();
        Assert.False(cursor.HasEnded);
        Assert.False(clock.Advance(1000, false));
        Assert.Equal(50, clock.ElapsedInFrame);
        cursor.Play();
        clock.Advance(149, false);
        Assert.True(cursor.IsPlaying);
        clock.Advance(1, false);
        Assert.True(cursor.HasEnded);
        cursor.Stop();
        clock.Reset();
        Assert.Equal(0, cursor.Position);
        Assert.Equal(0, clock.ElapsedInFrame);
    }
}
