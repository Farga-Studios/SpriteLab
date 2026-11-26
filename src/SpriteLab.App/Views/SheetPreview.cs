using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using SpriteLab.Core;

namespace SpriteLab.App.Views;

public sealed class SheetPreview : Control
{
    private Bitmap? _bitmap;
    private int _zoom = 1;
    private CellGrid? _grid;
    private int? _selected;
    private bool _showGrid = true;
    private IReadOnlyDictionary<int, DiagnosticCategory[]>? _markers;
    private static readonly IBrush[] MarkerBrushes = [Brushes.White, new SolidColorBrush(Color.Parse("#9CA3AF")), new SolidColorBrush(Color.Parse("#F59E0B")), new SolidColorBrush(Color.Parse("#B18AFF")), new SolidColorBrush(Color.Parse("#60A5FA")), new SolidColorBrush(Color.Parse("#F472B6")), Brushes.White];

    public void SetDiagnosticMarkers(IReadOnlyDictionary<int, DiagnosticCategory[]>? markers)
    {
        _markers = markers;
        InvalidateVisual();
    }
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.Parse("#90A0B0")), 1);
    private static readonly Pen SelectionPen = new(new SolidColorBrush(Color.Parse("#FFC65C")), 2);
    public event Action<int>? CellSelected;

    public void SetGrid(CellGrid? grid)
    {
        _grid = grid;
        _markers = null;
        _selected = null;
        InvalidateVisual();
    }

    public void SetSelection(int? index)
    {
        _selected = index;
        InvalidateVisual();
    }

    public void SetGridVisible(bool visible)
    {
        _showGrid = visible;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_grid is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var point = e.GetPosition(this);
        var index = _grid.HitTest(new SpriteLab.Core.PixelPoint((int)Math.Floor(point.X / _zoom),
            (int)Math.Floor(point.Y / _zoom)));
        if (index is int selected)
        {
            CellSelected?.Invoke(selected);
            e.Handled = true;
        }
    }
    private static readonly IBrush LightTile = new SolidColorBrush(Color.Parse("#46505D"));
    private static readonly IBrush DarkTile = new SolidColorBrush(Color.Parse("#343C48"));

    public void SetImage(Bitmap? bitmap)
    {
        _bitmap = bitmap;
        SetGrid(null);
        SetZoom(1);
    }

    public void SetZoom(int zoom)
    {
        _zoom = Math.Clamp(zoom, 1, 8);
        Width = _bitmap is null ? 0 : (double)_bitmap.PixelSize.Width * _zoom;
        Height = _bitmap is null ? 0 : (double)_bitmap.PixelSize.Height * _zoom;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_bitmap is null) return;
        // DrawingBrush tiles the checkerboard without enumerating every pixel.
        var tiles = new DrawingGroup();
        tiles.Children.Add(new GeometryDrawing { Brush = LightTile, Geometry = new RectangleGeometry(new Rect(0, 0, 16, 16)) });
        tiles.Children.Add(new GeometryDrawing { Brush = DarkTile, Geometry = new RectangleGeometry(new Rect(8, 0, 8, 8)) });
        tiles.Children.Add(new GeometryDrawing { Brush = DarkTile, Geometry = new RectangleGeometry(new Rect(0, 8, 8, 8)) });
        var checker = new DrawingBrush
        {
            Drawing = tiles,
            TileMode = TileMode.Tile,
            DestinationRect = new RelativeRect(0, 0, 16, 16, RelativeUnit.Absolute),
            Stretch = Stretch.None
        };
        var target = new Rect(0, 0, Width, Height);
        context.DrawRectangle(checker, null, target);
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            context.DrawImage(_bitmap, new Rect(_bitmap.Size), target);

        if (_grid is null) return;
        if (_showGrid)
        {
            // Do not draw sub-four-DIP grid lines at low zoom.
            var dx = (double)_grid.Cell.Width * _zoom;
            var dy = (double)_grid.Cell.Height * _zoom;
            if (dx >= 4)
                for (int col = 1; col < _grid.Columns; col++)
                    context.DrawLine(GridPen, new Point(col * dx, 0), new Point(col * dx, Height));
            if (dy >= 4)
                for (int row = 1; row < _grid.Rows; row++)
                    context.DrawLine(GridPen, new Point(0, row * dy), new Point(Width, row * dy));
        }
        if (_markers is not null)
        {
            foreach (var (index, categories) in _markers)
            {
                if (index < 0 || index >= _grid.Count || categories.Length == 0) continue;
                var cell = _grid.GetCell(index);
                var w = (double)cell.Width * _zoom;
                var h = (double)cell.Height * _zoom;
                var segment = w / categories.Length;
                var height = Math.Min(5, h);
                for (var i = 0; i < categories.Length; i++)
                    context.DrawRectangle(MarkerBrushes[(int)categories[i]], null,
                        new Rect(cell.X * _zoom + i * segment, cell.Y * _zoom, segment, height));
            }
        }
        if (_selected is int selected)
        {
            var cell = _grid.GetCell(selected);
            var bounds = new Rect(cell.X * _zoom, cell.Y * _zoom,
                cell.Width * _zoom, cell.Height * _zoom);
            context.DrawRectangle(null, SelectionPen, bounds);
        }
    }
}
