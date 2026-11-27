using SpriteLab.Core.Export;
using Xunit;
namespace SpriteLab.Core.Tests;
public sealed class AnimatedGifTests
{
    [Fact] public void GifHasFramesAndTrailer()
    {
        using var stream=new MemoryStream();
        var frame=new RasterFrame([255,0,0,255,0,0,0,0],2,1,0);
        AnimatedGif.Write(stream,2,1,[new GifFrame(frame,100),new GifFrame(frame,150)],new GifOptions(2));
        var bytes=stream.ToArray();
        Assert.Equal("GIF89a",System.Text.Encoding.ASCII.GetString(bytes,0,6));
        Assert.Equal(4,bytes[6]);Assert.Equal(2,bytes[8]);Assert.Equal(0x3b,bytes[^1]);
        Assert.Equal(2,Enumerable.Range(0,bytes.Length-3).Count(i=>bytes[i]==0x21&&bytes[i+1]==0xf9&&bytes[i+2]==4));
    }
    [Fact] public void InvalidScaleRejected()
    {
        using var stream=new MemoryStream();
        Assert.Throws<ArgumentException>(()=>AnimatedGif.Write(stream,1,1,[],new GifOptions(5)));
    }
}
