using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private int _zoom;
    private Vector _pan;
    private Point? _drag;
    public string ZoomLabel => _zoom == 0 ? "Ajustar" : $"{_zoom * 100}%";
    public void ChangeZoom(int delta)
    {
        _zoom = Math.Clamp((_zoom == 0 ? 1 : _zoom) + delta, 1, 16);
        InvalidateVisual();
    }
    public void ResetView()
    {
        _zoom = 0;
        _pan = default;
        InvalidateVisual();
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed) return;
        _drag = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag is not Point previous) return;
        var point = e.GetPosition(this);
        _pan += point - previous;
        _drag = point;
        InvalidateVisual();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _drag = null;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _drag = null;
    }
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
        if (_zoom > 0) scale = _zoom;
        var w = _cell.Width * scale;
        var h = _cell.Height * scale;
        var target = new Rect((Bounds.Width - w) / 2 + _pan.X, (Bounds.Height - h) / 2 + _pan.Y, w, h);
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
