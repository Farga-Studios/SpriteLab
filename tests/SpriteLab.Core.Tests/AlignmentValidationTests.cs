using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class AlignmentValidationTests
{
    [Fact]
    public void BoundsIncludeSemiTransparentPixelsAndIgnoreHiddenRgb()
    {
        var grid = new CellGrid(new PixelSize(8, 4), new PixelSize(4, 4));
        var pixels = new byte[128];
        pixels[0] = 255;
        pixels[(8 + 1) * 4 + 3] = 1;
        pixels[(16 + 2) * 4 + 3] = 255;
        var result = SheetValidation.Analyze(grid, pixels);
        Assert.Equal(new VisibleBounds(1, 1, 2, 2), result.Bounds[0]);
        Assert.False(result.Bounds.ContainsKey(1));
    }

    [Fact]
    public void LargeChangesAreAdvisoryAndReferencePreviousCell()
    {
        var grid = new CellGrid(new PixelSize(100, 20), new PixelSize(20, 20));
        var bounds = new Dictionary<int, VisibleBounds>
        {
            [0] = new(2, 2, 4, 4), [1] = new(10, 10, 10, 10)
        };
        var result = AlignmentValidation.Analyze(grid, bounds, [new("Attack", "front", [0, 1])]);
        Assert.Equal(2, result.TotalFindings);
        Assert.Equal(new[] { "ALIGNMENT", "SIZE" }, result.Findings.Select(f => f.Code).ToArray());
        Assert.All(result.Findings, f => Assert.Equal((int?)0, f.ReferenceCell));
    }

    [Fact]
    public void RepeatsEmptyGapsAndDifferentTracksAreNotCompared()
    {
        var grid = new CellGrid(new PixelSize(60, 20), new PixelSize(20, 20));
        var bounds = new Dictionary<int, VisibleBounds> { [0] = new(0, 0, 2, 2), [2] = new(12, 12, 8, 8) };
        var result = AlignmentValidation.Analyze(grid, bounds,
            [new("Idle", "front", [0, 0, 1, 2]), new("Idle", "back", [0]), new("Walk", "front", [2])]);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void SmallMovementIsAllowedAndCapDoesNotTruncateCounts()
    {
        var grid = new CellGrid(new PixelSize(60, 20), new PixelSize(20, 20));
        var bounds = new Dictionary<int, VisibleBounds> { [0] = new(2, 2, 4, 4), [1] = new(3, 3, 4, 4), [2] = new(10, 10, 10, 10) };
        var result = AlignmentValidation.Analyze(grid, bounds, [new("Walk", "front", [0, 1, 2])], 1);
        Assert.Equal(2, result.TotalFindings);
        Assert.Equal(1, result.OmittedFindings);
        Assert.Single(result.Findings);
    }
}
