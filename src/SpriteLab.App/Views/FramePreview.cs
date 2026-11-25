using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SpriteLab.Core;

namespace SpriteLab.App.Views;

public sealed class FramePreview : Control
{
    private Bitmap? _bitmap;
    private PixelRect _cell;
    public void SetFrame(Bitmap? bitmap, PixelRect cell)
    {
        _bitmap = bitmap;
        _cell = cell;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#11151C")), null, new Rect(Bounds.Size));
        if (_bitmap is null || _cell.Width <= 0 || _cell.Height <= 0) return;
        var scale = Math.Min(Bounds.Width / _cell.Width, Bounds.Height / _cell.Height);
        if (scale >= 1) scale = Math.Floor(scale);
        var w = _cell.Width * scale;
        var h = _cell.Height * scale;
        var pixelScaleX = _bitmap.Size.Width / _bitmap.PixelSize.Width;
        var pixelScaleY = _bitmap.Size.Height / _bitmap.PixelSize.Height;
        var source = new Rect(_cell.X * pixelScaleX, _cell.Y * pixelScaleY,
            _cell.Width * pixelScaleX, _cell.Height * pixelScaleY);
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            context.DrawImage(_bitmap, source, new Rect((Bounds.Width - w) / 2, (Bounds.Height - h) / 2, w, h));
    }
}
