using SpriteLab.Core;
using SpriteLab.Core.Processing;
using Xunit;

namespace SpriteLab.Core.Tests;
public sealed class SpriteProcessorTests
{
    private static CellGrid Grid() => new(new PixelSize(4, 1), new PixelSize(2, 1));

    [Fact]
    public void MirrorOnlySelectedCellAndPreserveSource()
    {
        byte[] pixels = [1,2,3,255, 4,5,6,255, 7,8,9,255, 10,11,12,255];
        var before = pixels.ToArray();
        var result = SpriteProcessor.Run(Grid(), pixels, new(SpriteOperation.MirrorHorizontal,[0]));
        Assert.Equal(new byte[]{4,5,6,255,1,2,3,255,7,8,9,255,10,11,12,255},result.Rgba);
        Assert.Equal(before,pixels);
        Assert.Equal(2,result.ChangedPixels);
    }

    [Fact]
    public void CleanAndThresholdKeepSourceUnchanged()
    {
        byte[] pixels=[8,9,10,0, 3,4,5,127, 4,5,6,128, 7,8,9,255];
        var clean=SpriteProcessor.Run(Grid(),pixels,new(SpriteOperation.CleanTransparentRgb,[0,1]));
        Assert.Equal(new byte[]{0,0,0,0,3,4,5,127,4,5,6,128,7,8,9,255},clean.Rgba);
        var threshold=SpriteProcessor.Run(Grid(),pixels,new(SpriteOperation.AlphaThreshold,[0,1]));
        Assert.Equal(new byte[]{0,0,0,0,0,0,0,0,4,5,6,255,7,8,9,255},threshold.Rgba);
        Assert.Equal(8,pixels[0]);
    }

    [Fact]
    public void VerticalMirrorAndDeduplication()
    {
        var grid = new CellGrid(new PixelSize(1,2),new PixelSize(1,2));
        byte[] pixels=[1,2,3,255,4,5,6,255];
        var result=SpriteProcessor.Run(grid,pixels,new(SpriteOperation.MirrorVertical,[0,0]));
        Assert.Equal(new byte[]{4,5,6,255,1,2,3,255},result.Rgba);
        Assert.Equal(1,result.ProcessedCells);
    }

    [Fact]
    public void InvalidSelectionAndCancellationReject()
    {
        var pixels=new byte[16];
        Assert.Throws<ArgumentException>(()=>SpriteProcessor.Run(Grid(),pixels,new(SpriteOperation.MirrorHorizontal,[2])));
        Assert.Throws<OperationCanceledException>(()=>SpriteProcessor.Run(Grid(),pixels,new(SpriteOperation.MirrorHorizontal,[0]),token:new CancellationToken(true)));
    }
}
