using System.Buffers.Binary;
using System.IO.Compression;
using SpriteLab.Core.Processing;
using Xunit;

namespace SpriteLab.Core.Tests;
public sealed class RgbaPngTests
{
    [Fact]
    public void EncodedRowsPreserveExactRgba()
    {
        byte[] pixels = [9,8,7,0, 1,2,3,127, 4,5,6,255, 7,8,9,1];
        var png=RgbaPng.Encode(2,2,pixels);
        Assert.Equal(new byte[]{137,80,78,71,13,10,26,10},png[..8]);
        Assert.Equal(2,BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4)));
        Assert.Equal(2,BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4)));
        Assert.Equal(6,png[25]);
        var length=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33,4));
        using var compressed=new MemoryStream(png,41,length);
        using var zlib=new ZLibStream(compressed,CompressionMode.Decompress);
        using var raw=new MemoryStream();
        zlib.CopyTo(raw);
        Assert.Equal(new byte[]{0,9,8,7,0,1,2,3,127,0,4,5,6,255,7,8,9,1},raw.ToArray());
    }

    [Fact]
    public void InvalidAndCancelledRequestsReject()
    {
        Assert.Throws<ArgumentException>(()=>RgbaPng.Encode(2,2,new byte[4]));
        Assert.Throws<OperationCanceledException>(()=>RgbaPng.Encode(1,1,new byte[4],new CancellationToken(true)));
    }
}
