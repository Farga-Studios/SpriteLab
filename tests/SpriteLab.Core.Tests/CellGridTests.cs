using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class CellGridTests
{
    [Fact]
    public void RectangularGridUsesRowMajorOrder()
    {
        var grid = new CellGrid(new PixelSize(96, 64), new PixelSize(48, 32));
        Assert.Equal(4, grid.Count);
        Assert.Equal(new PixelRect(48, 32, 48, 32), grid.GetCell(3));
        Assert.Equal((int?)3, grid.HitTest(new PixelPoint(48, 32)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(96, 0)]
    [InlineData(0, 64)]
    public void OutsidePointsDoNotSelect(int x, int y)
    {
        var grid = new CellGrid(new PixelSize(96, 64), new PixelSize(48, 32));
        Assert.Null(grid.HitTest(new PixelPoint(x, y)));
    }

    [Fact]
    public void RejectsIncompleteGridAndInvalidIndex()
    {
        Assert.Throws<ArgumentException>(() => new CellGrid(new PixelSize(97, 64), new PixelSize(48, 32)));
        var grid = new CellGrid(new PixelSize(96, 64), new PixelSize(48, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(4));
    }

    [Fact]
    public void BoundaryBelongsToNextCell()
    {
        var grid = new CellGrid(new PixelSize(96, 64), new PixelSize(48, 32));
        Assert.Equal((int?)0, grid.HitTest(new PixelPoint(47, 31)));
        Assert.Equal((int?)1, grid.HitTest(new PixelPoint(48, 31)));
        Assert.Equal((int?)2, grid.HitTest(new PixelPoint(47, 32)));
    }
}
