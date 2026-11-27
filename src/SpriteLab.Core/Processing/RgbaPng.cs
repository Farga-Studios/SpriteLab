using System.Buffers.Binary;
using System.IO.Compression;

namespace SpriteLab.Core.Processing;

// PNG RGBA8, no filters or colour conversion; preserves invisible RGB as well as alpha.
public static class RgbaPng
{
    public static byte[] Encode(int width, int height, byte[] rgba, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        if (width is < 1 or > 8192 || height is < 1 or > 8192 ||
            (long)width * height > 16_777_216 || rgba.LongLength != (long)width * height * 4)
            throw new ArgumentException("Invalid RGBA dimensions.");
        token.ThrowIfCancellationRequested();
        using var output = new MemoryStream();
        output.Write(new byte[] { 137,80,78,71,13,10,26,10 });
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0,4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4,4), height);
        header[8] = 8; header[9] = 6;
        Chunk(output, "IHDR"u8, header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            for (var y = 0; y < height; y++)
            {
                token.ThrowIfCancellationRequested();
                zlib.WriteByte(0);
                zlib.Write(rgba.AsSpan(y * width * 4, width * 4));
            }
        token.ThrowIfCancellationRequested();
        Chunk(output, "IDAT"u8, compressed.GetBuffer().AsSpan(0, checked((int)compressed.Length)));
        Chunk(output, "IEND"u8, []);
        token.ThrowIfCancellationRequested();
        return output.ToArray();
    }

    private static readonly uint[] CrcTable = BuildCrcTable();
    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var b = 0; b < 8; b++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }
    private static void Chunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number); stream.Write(type); stream.Write(data);
        var crc = uint.MaxValue;
        foreach (var b in type) crc = CrcTable[(crc ^ b) & 255] ^ (crc >> 8);
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 255] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        stream.Write(number);
    }
}
