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
}
