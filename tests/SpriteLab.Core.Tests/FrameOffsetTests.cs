using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class FrameOffsetTests
{
    private static CellGrid Grid() => new(new PixelSize(96, 48), new PixelSize(48, 48));

    [Fact]
    public void EditsAreSparseAndResetRemovesEntry()
    {
        var original = new FrameOffset[] { new(0, 3, -4) };
        var edited = FrameOffset.Set(original, Grid(), new(1, -2, 8));
        Assert.Single(original);
        Assert.Equal(2, edited.Length);
        Assert.Single(FrameOffset.Set(edited, Grid(), FrameOffset.Zero(0)));
        Assert.Equal(3, original[0].X);
    }

    [Fact]
    public void InvalidCellsAndOffsetsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => FrameOffset.Validate(Grid(), [new(2, 0, 0)]));
        Assert.Throws<ArgumentException>(() => FrameOffset.Validate(Grid(), [new(0, 49, 0)]));
        Assert.Throws<ArgumentException>(() => FrameOffset.Validate(Grid(), [new(0, 1, 0), new(0, 2, 0)]));
    }
}
