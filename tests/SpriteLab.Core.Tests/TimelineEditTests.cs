using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class TimelineEditTests
{
    [Fact]
    public void MovePreservesExactRepeatedOccurrence()
    {
        var result = TimelineEdit.Apply([0, 1, 0], [100, 200, 300], 2, TimelineOperation.Move, 0);
        Assert.Equal(new[] { 0, 0, 1 }, result.Cells);
        Assert.Equal(new int?[] { 300, 100, 200 }, result.Durations);
        Assert.Equal(0, result.Selected);
    }

    [Fact]
    public void DuplicateCopiesTiming()
    {
        var result = TimelineEdit.Apply([0, 1], [null, 250], 1, TimelineOperation.Duplicate);
        Assert.Equal(new[] { 0, 1, 1 }, result.Cells);
        Assert.Equal(new int?[] { null, 250, 250 }, result.Durations);
        Assert.Equal(2, result.Selected);
    }

    [Fact]
    public void InsertUsesAutomaticDuration()
    {
        var result = TimelineEdit.Apply([0, 1], [100, 200], 0, TimelineOperation.Insert, 5);
        Assert.Equal(new[] { 0, 5, 1 }, result.Cells);
        Assert.Equal(new int?[] { 100, null, 200 }, result.Durations);
    }

    [Fact]
    public void RemoveSelectsNearestRemainingAndProtectsLast()
    {
        var result = TimelineEdit.Apply([0, 1], [null, 200], 1, TimelineOperation.Remove);
        Assert.Equal(new[] { 0 }, result.Cells);
        Assert.Null(result.Durations);
        Assert.Equal(0, result.Selected);
        Assert.Throws<ArgumentException>(() => TimelineEdit.Apply([0], null, 0, TimelineOperation.Remove));
    }
}
