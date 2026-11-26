using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SpriteLab.Core;

namespace SpriteLab.App.Views;

// Bounded thumbnail window around selection; no per-cell bitmap allocations.
public sealed class CellStrip : Control
{
    private Bitmap? _bitmap;
    private CellGrid? _grid;
    private int _selected;
    private int[]? _sequence;
    private int Count => _sequence?.Length ?? _grid?.Count ?? 0;
    public event Action<int>? OccurrenceSelected;

    public void SetSequence(int[]? cells)
    {
        _sequence = cells;
        _selected = _first = 0;
        InvalidateVisual();
    }
    private int _first;
    private const int Slot = 64;
    private static readonly IBrush Tile = new SolidColorBrush(Color.Parse("#303946"));
    private static readonly Pen Active = new(new SolidColorBrush(Color.Parse("#FFC65C")), 2);
    public event Action<int>? CellSelected;

    public void SetSheet(Bitmap? bitmap, CellGrid? grid)
    {
        _bitmap = bitmap;
        _grid = grid;
        _sequence = null;
        _selected = _first = 0;
        InvalidateVisual();
    }

    public void SetSelection(int index)
    {
        _selected = index;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_grid is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var point = e.GetPosition(this);
        if (point.X < 0 || point.X >= Bounds.Width || point.Y < 0 || point.Y >= Bounds.Height) return;
        var slot = (int)(point.X / Slot);
        if (slot >= Math.Max(1, (int)(Bounds.Width / Slot))) return;
        var index = _first + slot;
        if (index < Count)
        {
            if (_sequence is null) CellSelected?.Invoke(index);
            else OccurrenceSelected?.Invoke(index);
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_bitmap is null || _grid is null || Bounds.Height <= 0) return;
        var visible = Math.Max(1, (int)(Bounds.Width / Slot));
        _first = Math.Clamp(_selected - visible / 2, 0, Math.Max(0, Count - visible));
        var height = Math.Min(54, Bounds.Height);
        var sx = _bitmap.Size.Width / _bitmap.PixelSize.Width;
        var sy = _bitmap.Size.Height / _bitmap.PixelSize.Height;
        using var options = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None });
        for (int i = 0; i < visible && _first + i < Count; i++)
        {
            var index = _first + i;
            var cell = _grid.GetCell(_sequence is null ? index : _sequence[index]);
            var slot = new Rect(i * Slot + 2, 2, Slot - 4, Math.Max(1, height - 4));
            context.DrawRectangle(Tile, index == _selected ? Active : null, slot);
            var scale = Math.Min((slot.Width - 4) / cell.Width, Math.Max(1, slot.Height - 4) / cell.Height);
            if (scale >= 1) scale = Math.Floor(scale);
            var w = cell.Width * scale;
            var h = cell.Height * scale;
            var source = new Rect(cell.X * sx, cell.Y * sy, cell.Width * sx, cell.Height * sy);
            context.DrawImage(_bitmap, source, new Rect(slot.X + (slot.Width - w) / 2,
                slot.Y + (slot.Height - h) / 2, w, h));
        }
    }
}
