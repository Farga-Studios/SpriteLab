using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class PlaybackCursorTests
{
    [Fact]
    public void RepeatedCellsHaveDistinctPositions()
    {
        var cursor = new PlaybackCursor([2, 2, 1]);
        cursor.Play();
        Assert.True(cursor.Tick(false));
        Assert.Equal(1, cursor.Position);
        Assert.Equal(2, cursor.Cell);
        Assert.True(cursor.Tick(false));
        Assert.Equal(1, cursor.Cell);
        Assert.False(cursor.Tick(false));
        Assert.False(cursor.IsPlaying);
    }

    [Fact]
    public void LoopReturnsToFirstOccurrence()
    {
        var cursor = new PlaybackCursor([4, 8]);
        cursor.Play();
        cursor.Tick(true);
        Assert.True(cursor.Tick(true));
        Assert.Equal(4, cursor.Cell);
        Assert.True(cursor.IsPlaying);
    }

    [Fact]
    public void PausePreservesPositionAndStopResets()
    {
        var cursor = new PlaybackCursor([4, 8]);
        cursor.Play();
        cursor.Tick(false);
        cursor.Pause();
        Assert.False(cursor.Tick(true));
        Assert.Equal(8, cursor.Cell);
        cursor.Stop();
        Assert.Equal(4, cursor.Cell);
    }

    [Fact]
    public void StepClampsAndEmptySequenceIsRejected()
    {
        var cursor = new PlaybackCursor([4, 8]);
        cursor.Step(int.MaxValue);
        Assert.Equal(1, cursor.Position);
        cursor.Step(int.MinValue);
        Assert.Equal(0, cursor.Position);
        Assert.Throws<ArgumentException>(() => new PlaybackCursor([]));
    }
}
