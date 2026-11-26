using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace SpriteLab.App.Views;

internal static class ValidationPixels
{
    public static byte[] Read(Bitmap source)
    {
        using var target = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using var buffer = target.Lock();
        source.CopyPixels(buffer, AlphaFormat.Unpremul);
        var stride = checked(source.PixelSize.Width * 4);
        var result = new byte[checked(stride * source.PixelSize.Height)];
        for (var y = 0; y < source.PixelSize.Height; y++)
            Marshal.Copy(IntPtr.Add(buffer.Address, checked(y * buffer.RowBytes)), result, y * stride, stride);
        return result;
    }
}
