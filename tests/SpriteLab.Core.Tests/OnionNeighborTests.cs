using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class OnionNeighborTests
{
    [Fact]
    public void NeighborsFollowTrackOrderNotSheetOrder()
    {
        var cursor = new PlaybackCursor([6, 2, 9]);
        cursor.Step(1);
        Assert.Equal((int?)6, cursor.Neighbor(-1, false));
        Assert.Equal((int?)9, cursor.Neighbor(1, false));
    }

    [Fact]
    public void BoundariesRespectLoop()
    {
        var cursor = new PlaybackCursor([6, 2, 9]);
        Assert.Null(cursor.Neighbor(-1, false));
        Assert.Equal((int?)9, cursor.Neighbor(-1, true));
        cursor.Step(2);
        Assert.Null(cursor.Neighbor(1, false));
        Assert.Equal((int?)6, cursor.Neighbor(1, true));
    }

    [Fact]
    public void SingleFrameHasNoNeighbors()
    {
        var cursor = new PlaybackCursor([5]);
        Assert.Null(cursor.Neighbor(-1, true));
        Assert.Null(cursor.Neighbor(1, true));
    }

    [Fact]
    public void RepeatedCellIsStillAnOccurrence()
    {
        var cursor = new PlaybackCursor([5, 5, 8]);
        Assert.Equal((int?)5, cursor.Neighbor(1, false));
        Assert.Equal(0, cursor.Position);
    }
}
