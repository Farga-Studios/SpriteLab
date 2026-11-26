using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using CorePixelRect = SpriteLab.Core.PixelRect;

namespace SpriteLab.App.Views;

public sealed class FramePreview : Control
{
    private Bitmap? _bitmap;
    private CorePixelRect _cell;
    private CorePixelRect? _previous;
    private CorePixelRect? _next;
    private double _opacity = 0.35;
    private bool _tinted = true;
    private Bitmap? _previousTint;
    private Bitmap? _nextTint;
    private CorePixelRect? _previousKey;
    private CorePixelRect? _nextKey;

    private void ClearTints()
    {
        _previousTint?.Dispose();
        _nextTint?.Dispose();
        _previousTint = _nextTint = null;
        _previousKey = _nextKey = null;
    }

    // Read only the requested source rectangle into a 32-bit RGBA destination.
    private static Bitmap Tint(Bitmap source, CorePixelRect cell, byte red, byte green, byte blue)
    {
        using var rgba = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using (var buffer = rgba.Lock())
            source.CopyPixels(buffer, AlphaFormat.Unpremul);
        var output = new WriteableBitmap(new Avalonia.PixelSize(cell.Width, cell.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        try
        {
            using var src = rgba.Lock();
            using var dst = output.Lock();
            var row = new byte[checked(cell.Width * 4)];
            for (var y = 0; y < cell.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(src.Address, checked((cell.Y + y) * src.RowBytes + cell.X * 4)), row, 0, row.Length);
                for (var x = 0; x < row.Length; x += 4)
                {
                    row[x] = red; row[x + 1] = green; row[x + 2] = blue;
                }
                Marshal.Copy(row, 0, IntPtr.Add(dst.Address, checked(y * dst.RowBytes)), row.Length);
            }
            return output;
        }
        catch { output.Dispose(); throw; }
    }
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
        if (!ReferenceEquals(_bitmap, bitmap)) ClearTints();
        _bitmap = bitmap;
        _cell = cell;
        _previous = _next = null;
        InvalidateVisual();
    }

    public void SetOnion(CorePixelRect? previous, CorePixelRect? next, double opacity, bool tinted = true)
    {
        _tinted = tinted;
        _previous = previous;
        _next = next;
        _opacity = Math.Clamp(opacity, 0, 1);
        if (_bitmap is { } bitmap && tinted)
        {
            try
            {
                if (_previousKey != previous)
                {
                    _previousTint?.Dispose(); _previousTint = null; _previousKey = null;
                    if (previous is { } p) { _previousTint = Tint(bitmap, p, 242, 92, 102); _previousKey = p; }
                }
                if (_nextKey != next)
                {
                    _nextTint?.Dispose(); _nextTint = null; _nextKey = null;
                    if (next is { } n) { _nextTint = Tint(bitmap, n, 84, 218, 143); _nextKey = n; }
                }
            }
            catch (Exception) { ClearTints(); _tinted = false; }
        }
        else ClearTints();
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
            if (_tinted && _previousTint is { } ghost) context.DrawImage(ghost, new Rect(ghost.Size), target);
            else Draw(previous);
        }
        if (_next is { } next)
        {
            using var opacity = context.PushOpacity(_opacity);
            if (_tinted && _nextTint is { } ghost) context.DrawImage(ghost, new Rect(ghost.Size), target);
            else Draw(next);
        }
    }
}
