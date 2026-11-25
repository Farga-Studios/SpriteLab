using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class GeometryTests
{
    [Theory]
    [InlineData(0, 48)]
    [InlineData(48, 0)]
    [InlineData(-1, 48)]
    public void SizeRejectsNonPositiveDimensions(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelSize(width, height));

    [Fact]
    public void DefaultSizeIsInvalid() => Assert.False(default(PixelSize).IsValid);

    [Theory]
    [InlineData(-1, 0, 48, 48)]
    [InlineData(0, -1, 48, 48)]
    [InlineData(0, 0, 0, 48)]
    [InlineData(0, 0, 48, -1)]
    public void RectangleRejectsInvalidGeometry(int x, int y, int w, int h) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelRect(x, y, w, h));

    [Fact]
    public void RectangleUsesHalfOpenBounds()
    {
        var rect = new PixelRect(48, 0, 48, 32);
        Assert.True(rect.Fits(new PixelSize(96, 32)));
        Assert.False(rect.Fits(new PixelSize(95, 32)));
        Assert.False(default(PixelRect).Fits(new PixelSize(96, 32)));
    }

    [Fact]
    public void FitsDoesNotOverflow()
    {
        var rect = new PixelRect(int.MaxValue, 0, 2, 1);
        Assert.False(rect.Fits(new PixelSize(int.MaxValue, 1)));
    }

    [Fact]
    public void AnchorAllowsEdgeCoordinates()
    {
        var rect = new PixelRect(10, 20, 48, 32);
        Assert.True(rect.ContainsAnchor(new PixelPoint(48, 32)));
        Assert.False(rect.ContainsAnchor(new PixelPoint(49, 32)));
        Assert.False(rect.ContainsAnchor(new PixelPoint(-1, 0)));
    }

    [Fact]
    public void GridIsRowMajorAndRectangular()
    {
        var cells = GridLayout.Split(new PixelSize(96, 64), new PixelSize(48, 32));
        Assert.Equal(new[] {
            new PixelRect(0, 0, 48, 32), new PixelRect(48, 0, 48, 32),
            new PixelRect(0, 32, 48, 32), new PixelRect(48, 32, 48, 32)
        }, cells.ToArray());
    }

    [Fact]
    public void SquareGridWorks()
    {
        var cells = GridLayout.Split(new PixelSize(240, 384), new PixelSize(48, 48));
        Assert.Equal(40, cells.Count);
        Assert.Equal(new PixelRect(192, 336, 48, 48), cells[^1]);
    }

    [Fact]
    public void GridRejectsPartialCellsAndInvalidSize()
    {
        Assert.Throws<ArgumentException>(() => GridLayout.Split(new PixelSize(97, 64), new PixelSize(48, 32)));
        Assert.Throws<ArgumentException>(() => GridLayout.Split(new PixelSize(96, 64), default));
    }
}
