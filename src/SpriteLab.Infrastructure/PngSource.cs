using System.Buffers.Binary;

namespace SpriteLab.Infrastructure;

// A bounded, read-only snapshot for preview. Full PNG decoding is delegated to the UI decoder.
public static class PngSource
{
    private const int MaxBytes = 64 * 1024 * 1024;
    private const long MaxPixels = 16_777_216;

    public static async Task<byte[]> ReadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await source.ReadAsync(chunk.AsMemory(), cancellationToken)) != 0)
        {
            if (buffer.Length + count > MaxBytes)
                throw new InvalidDataException("PNG exceeds 64 MiB.");
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        ValidateHeader(bytes);
        return bytes;
    }

    public static SpriteLab.Core.PixelSize GetSize(byte[] bytes)
    {
        ValidateHeader(bytes);
        return new SpriteLab.Core.PixelSize(
            (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)),
            (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static void ValidateHeader(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)) != 13 ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Invalid PNG header.");

        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width == 0 || height == 0 || width > 8192 || height > 8192 ||
            (long)width * height > MaxPixels)
            throw new InvalidDataException("PNG dimensions exceed preview limits.");
    }
}
