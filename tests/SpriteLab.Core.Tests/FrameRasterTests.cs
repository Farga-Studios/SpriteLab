using SpriteLab.Core;
using SpriteLab.Core.Export;
using Xunit;
namespace SpriteLab.Core.Tests;
public sealed class FrameRasterTests
{
    [Fact]
    public void OffsetClipsWithoutSpillingOrMutating()
    {
        var grid=new CellGrid(new PixelSize(4,1),new PixelSize(2,1));
        byte[] source=[1,2,3,255,4,5,6,255,7,8,9,255,10,11,12,255];
        var before=source.ToArray();
        var result=FrameRaster.RenderSheet(grid,source,[new FrameOffset(0,1,0)]);
        Assert.Equal(new byte[]{0,0,0,0,1,2,3,255,7,8,9,255,10,11,12,255},result.Rgba);
        Assert.Equal(1,result.ClippedVisiblePixels);
        Assert.Equal(before,source);
    }
    [Fact]
    public void NegativeVerticalOffsetOnRectangularCell()
    {
        var grid=new CellGrid(new PixelSize(1,3),new PixelSize(1,3));
        byte[] source=[1,2,3,0,4,5,6,255,7,8,9,128];
        var frame=FrameRaster.Render(grid,source,0,new FrameOffset(0,0,-1));
        Assert.Equal(new byte[]{4,5,6,255,7,8,9,128,0,0,0,0},frame.Rgba);
        Assert.Equal(0,frame.ClippedVisiblePixels);
    }
}
