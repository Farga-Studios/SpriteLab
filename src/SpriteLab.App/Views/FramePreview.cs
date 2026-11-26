using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CorePixelRect = SpriteLab.Core.PixelRect;

namespace SpriteLab.App.Views;

public sealed class FramePreview : Control
{
    private Bitmap? _bitmap;
    private CorePixelRect _cell;
    private CorePixelRect? _previous;
    private CorePixelRect? _next;
    private double _opacity = 0.35;
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#11151C"));

    public void SetFrame(Bitmap? bitmap, CorePixelRect cell)
    {
        _bitmap = bitmap;
        _cell = cell;
        _previous = _next = null;
        InvalidateVisual();
    }

    public void SetOnion(CorePixelRect? previous, CorePixelRect? next, double opacity)
    {
        _previous = previous;
        _next = next;
        _opacity = Math.Clamp(opacity, 0, 1);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush, null, new Rect(Bounds.Size));
        var bitmap = _bitmap;
        if (bitmap is null || _cell.Width <= 0 || _cell.Height <= 0 ||
            Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var scale = Math.Min(Bounds.Width / _cell.Width, Bounds.Height / _cell.Height);
        if (scale >= 1) scale = Math.Floor(scale);
        var w = _cell.Width * scale;
        var h = _cell.Height * scale;
        var target = new Rect((Bounds.Width - w) / 2, (Bounds.Height - h) / 2, w, h);
        var sx = bitmap.Size.Width / bitmap.PixelSize.Width;
        var sy = bitmap.Size.Height / bitmap.PixelSize.Height;
        using var options = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None });
        void Draw(CorePixelRect cell) =>
            context.DrawImage(bitmap, new Rect(cell.X * sx, cell.Y * sy,
                cell.Width * sx, cell.Height * sy), target);

        Draw(_cell);
        if (_previous is { } previous)
        {
            using var opacity = context.PushOpacity(_opacity);
            Draw(previous);
        }
        if (_next is { } next)
        {
            using var opacity = context.PushOpacity(_opacity);
            Draw(next);
        }
    }
}
