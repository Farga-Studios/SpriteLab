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
    public bool TintFailed { get; private set; }
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

    // Reuse one RGBA source conversion for both changed neighbors.
    private static Bitmap Tint(WriteableBitmap rgba, CorePixelRect cell, byte red, byte green, byte blue)
    {
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
    private Point? _spriteDrag;
    private int _offsetX, _offsetY, _startX, _startY;
    private double _renderScale = 1;
    private SpriteLab.Core.FrameOffset? _previousOffset, _nextOffset;
    private SpriteLab.Core.EditorGuides _guides = new(false, false, false);
    public bool CanMoveSprite { get; set; }
    public bool SnapEnabled { get; set; }
    private SpriteLab.Core.FrameAnchor? _anchor;
    public void SetAnchor(SpriteLab.Core.FrameAnchor? anchor) { _anchor = anchor; InvalidateVisual(); }
    public event Action<int, int>? OffsetCommitted;
    public event Action<int, int>? OffsetPreviewChanged;

    public void SetOffset(int x, int y) { _offsetX = x; _offsetY = y; InvalidateVisual(); }
    public void SetGuides(SpriteLab.Core.EditorGuides guides) { _guides = guides; InvalidateVisual(); }

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
        var properties = e.GetCurrentPoint(this).Properties;
        if (CanMoveSprite && _bitmap is not null && properties.IsLeftButtonPressed)
        {
            _spriteDrag = e.GetPosition(this); _startX = _offsetX; _startY = _offsetY;
            e.Pointer.Capture(this); e.Handled = true; return;
        }
        if (!properties.IsMiddleButtonPressed) return;
        _drag = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_spriteDrag is Point start)
        {
            var delta = e.GetPosition(this) - start;
            var x = Math.Clamp(_startX + (int)Math.Round(delta.X / _renderScale), -_cell.Width, _cell.Width);
            var y = Math.Clamp(_startY + (int)Math.Round(delta.Y / _renderScale), -_cell.Height, _cell.Height);
            if (SnapEnabled)
            {
                var ax = _anchor?.X ?? _cell.Width / 2;
                var ay = _anchor?.Y ?? _cell.Height;
                var xs = new List<int> { 0 };
                var ys = new List<int> { 0 };
                if (_guides.ShowCenter) xs.Add(_cell.Width / 2 - ax);
                if (_guides.ShowGround) ys.Add(_cell.Height - ay);
                if (_guides.ShowCustom) { xs.Add(_guides.CustomX - ax); ys.Add(_guides.CustomY - ay); }
                x = SpriteLab.Core.FrameAlignment.Snap(x, xs.Where(t => t >= -_cell.Width && t <= _cell.Width), 3);
                y = SpriteLab.Core.FrameAlignment.Snap(y, ys.Where(t => t >= -_cell.Height && t <= _cell.Height), 3);
            }
            SetOffset(x, y);
            OffsetPreviewChanged?.Invoke(_offsetX, _offsetY);
            e.Handled = true; return;
        }
        if (_drag is not Point previous) return;
        var point = e.GetPosition(this);
        _pan += point - previous;
        _drag = point;
        InvalidateVisual();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_spriteDrag.HasValue)
        {
            _spriteDrag = null;
            if (CanMoveSprite) OffsetCommitted?.Invoke(_offsetX, _offsetY);
            else { SetOffset(_startX, _startY); OffsetPreviewChanged?.Invoke(_startX, _startY); }
        }
        _drag = null;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_spriteDrag.HasValue) { _spriteDrag = null; SetOffset(_startX, _startY); OffsetPreviewChanged?.Invoke(_startX, _startY); }
        _drag = null;
    }
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#11151C"));

    public void SetFrame(Bitmap? bitmap, CorePixelRect cell)
    {
        _spriteDrag = null;
        _drag = null;
        if (!ReferenceEquals(_bitmap, bitmap)) ClearTints();
        _bitmap = bitmap;
        _cell = cell;
        _previous = _next = null;
        InvalidateVisual();
    }

    public void SetOnion(CorePixelRect? previous, CorePixelRect? next, double opacity, bool tinted = true, SpriteLab.Core.FrameOffset? previousOffset = null, SpriteLab.Core.FrameOffset? nextOffset = null)
    {
        _previousOffset = previousOffset; _nextOffset = nextOffset;
        _tinted = tinted;
        TintFailed = false;
        _previous = previous;
        _next = next;
        _opacity = Math.Clamp(opacity, 0, 1);
        if (_bitmap is { } bitmap && tinted)
        {
            try
            {
                var needPixels = (_previousKey != previous && previous.HasValue) || (_nextKey != next && next.HasValue);
                using var rgba = needPixels ? new WriteableBitmap(bitmap.PixelSize, bitmap.Dpi, PixelFormat.Rgba8888, AlphaFormat.Unpremul) : null;
                if (rgba is not null)
                {
                    using var buffer = rgba.Lock();
                    bitmap.CopyPixels(buffer, AlphaFormat.Unpremul);
                }
                if (_previousKey != previous)
                {
                    _previousTint?.Dispose(); _previousTint = null; _previousKey = null;
                    if (previous is { } p) { _previousTint = Tint(rgba!, p, 242, 92, 102); _previousKey = p; }
                }
                if (_nextKey != next)
                {
                    _nextTint?.Dispose(); _nextTint = null; _nextKey = null;
                    if (next is { } n) { _nextTint = Tint(rgba!, n, 84, 218, 143); _nextKey = n; }
                }
            }
            catch (Exception) { ClearTints(); _tinted = false; TintFailed = true; }
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
        var scale = Math.Min(Math.Max(1, Bounds.Width - 16) / _cell.Width, Math.Max(1, Bounds.Height - 16) / _cell.Height);
        if (scale >= 1) scale = Math.Floor(scale);
        if (_zoom > 0) scale = _zoom;
        _renderScale = scale;
        var w = _cell.Width * scale;
        var h = _cell.Height * scale;
        var target = new Rect((Bounds.Width - w) / 2 + _pan.X, (Bounds.Height - h) / 2 + _pan.Y, w, h);
        var sx = bitmap.Size.Width / bitmap.PixelSize.Width;
        var sy = bitmap.Size.Height / bitmap.PixelSize.Height;
        using var options = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None });
        Rect Shift(int x, int y) => new(target.X + x * scale, target.Y + y * scale, target.Width, target.Height);
        void Draw(CorePixelRect cell, Rect destination) =>
            context.DrawImage(bitmap, new Rect(cell.X * sx, cell.Y * sy,
                cell.Width * sx, cell.Height * sy), destination);

        Draw(_cell, Shift(_offsetX, _offsetY));
        if (_previous is { } previous)
        {
            using var opacity = context.PushOpacity(_opacity);
            if (_tinted && _previousTint is { } ghost) context.DrawImage(ghost, new Rect(ghost.Size), Shift(_previousOffset?.X ?? 0, _previousOffset?.Y ?? 0));
            else Draw(previous, Shift(_previousOffset?.X ?? 0, _previousOffset?.Y ?? 0));
        }
        if (_next is { } next)
        {
            using var opacity = context.PushOpacity(_opacity);
            if (_tinted && _nextTint is { } ghost) context.DrawImage(ghost, new Rect(ghost.Size), Shift(_nextOffset?.X ?? 0, _nextOffset?.Y ?? 0));
            else Draw(next, Shift(_nextOffset?.X ?? 0, _nextOffset?.Y ?? 0));
        }
        if (_anchor is { } anchor)
        {
            var point = new Point(target.X + (anchor.X + _offsetX) * scale, target.Y + (anchor.Y + _offsetY) * scale);
            var pen = new Pen(Brushes.Orange, 1.5);
            context.DrawLine(pen, new Point(point.X - 6, point.Y), new Point(point.X + 6, point.Y));
            context.DrawLine(pen, new Point(point.X, point.Y - 6), new Point(point.X, point.Y + 6));
        }
        var outline = new Pen(Brushes.SlateGray, 1);
        var center = new Pen(Brushes.DeepSkyBlue, 1);
        var ground = new Pen(Brushes.LightGreen, 1);
        var custom = new Pen(Brushes.Gold, 1);
        if (_guides.ShowBounds) context.DrawRectangle(null, outline, target);
        if (_guides.ShowCenter) context.DrawLine(center, new Point(target.Center.X, target.Y), new Point(target.Center.X, target.Bottom));
        if (_guides.ShowGround) context.DrawLine(ground, new Point(target.X, target.Bottom), new Point(target.Right, target.Bottom));
        if (_guides.ShowCustom)
        {
            var gx = target.X + _guides.CustomX * scale;
            var gy = target.Y + _guides.CustomY * scale;
            context.DrawLine(custom, new Point(gx, 0), new Point(gx, Bounds.Height));
            context.DrawLine(custom, new Point(0, gy), new Point(Bounds.Width, gy));
        }
    }
}
