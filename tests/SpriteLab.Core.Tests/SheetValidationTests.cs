using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class SheetValidationTests
{
    [Fact]
    public void EmptyEdgeAndInteriorAreDistinguished()
    {
        var grid = new CellGrid(new PixelSize(9, 3), new PixelSize(3, 3));
        var pixels = new byte[9 * 3 * 4];
        pixels[(0 * 9 + 3) * 4 + 3] = 1;
        pixels[(1 * 9 + 7) * 4 + 3] = 255;
        var findings = SheetValidation.Scan(grid, pixels);
        Assert.Equal(2, findings.Count);
        Assert.Equal("EMPTY", findings[0].Code);
        Assert.Equal((int?)0, findings[0].Cell);
        Assert.Equal("EDGE", findings[1].Code);
        Assert.Equal((int?)1, findings[1].Cell);
    }

    [Fact]
    public void TransparentRgbDoesNotCountAsVisible()
    {
        var grid = new CellGrid(new PixelSize(1, 1), new PixelSize(1, 1));
        Assert.Equal("EMPTY", Assert.Single(SheetValidation.Scan(grid, [255, 255, 255, 0])).Code);
    }

    [Fact]
    public void InvalidBufferAndCancellationAreRejected()
    {
        var grid = new CellGrid(new PixelSize(1, 1), new PixelSize(1, 1));
        Assert.Throws<ArgumentException>(() => SheetValidation.Scan(grid, [0]));
        Assert.Throws<OperationCanceledException>(() => SheetValidation.Scan(grid, [0, 0, 0, 0], new CancellationToken(true)));
    }

    [Fact]
    public void ResultLimitIsExplicit()
    {
        var grid = new CellGrid(new PixelSize(1001, 1), new PixelSize(1, 1));
        var findings = SheetValidation.Scan(grid, new byte[4004]);
        Assert.Equal(1001, findings.Count);
        Assert.Equal("LIMIT", findings[^1].Code);
    }

    [Fact]
    public void ReportsEveryTouchedEdgeIncludingCorners()
    {
        var grid = new CellGrid(new PixelSize(3, 3), new PixelSize(3, 3));
        var pixels = new byte[36];
        pixels[3] = 1;
        pixels[35] = 255;
        var result = SheetValidation.Analyze(grid, pixels);
        Assert.Equal(1, result.EdgeCells);
        Assert.Equal(CellEdges.Top | CellEdges.Left | CellEdges.Right | CellEdges.Bottom,
            Assert.Single(result.Findings).Edges);
    }

    [Fact]
    public void CountsEntireSheetBeyondListLimit()
    {
        var grid = new CellGrid(new PixelSize(1002, 1), new PixelSize(1, 1));
        var pixels = new byte[4008];
        pixels[^1] = 255;
        var result = SheetValidation.Analyze(grid, pixels);
        Assert.Equal(1002, result.TotalCells);
        Assert.Equal(1001, result.EmptyCells);
        Assert.Equal(1, result.EdgeCells);
        Assert.Equal(0, result.CleanCells);
        Assert.Equal(2, result.OmittedFindings);
    }

    [Fact]
    public void ExactlyAtLimitDoesNotReportOmittedFindings()
    {
        var grid = new CellGrid(new PixelSize(1000, 1), new PixelSize(1, 1));
        var result = SheetValidation.Analyze(grid, new byte[4000]);
        Assert.Equal(0, result.OmittedFindings);
        Assert.Equal(1000, result.Findings.Count);
    }

    [Fact]
    public void RectangularGridReportsCleanAndEmptyCounts()
    {
        var grid = new CellGrid(new PixelSize(6, 4), new PixelSize(3, 4));
        var pixels = new byte[96];
        pixels[(6 + 1) * 4 + 3] = 128;
        var result = SheetValidation.Analyze(grid, pixels);
        Assert.Equal(1, result.CleanCells);
        Assert.Equal(1, result.EmptyCells);
        Assert.Equal(0, result.EdgeCells);
    }
}
