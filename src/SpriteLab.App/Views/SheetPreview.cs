using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SpriteLab.App.Views;

public sealed class SheetPreview : Control
{
    private Bitmap? _bitmap;
    private int _zoom = 1;
    private static readonly IBrush LightTile = new SolidColorBrush(Color.Parse("#46505D"));
    private static readonly IBrush DarkTile = new SolidColorBrush(Color.Parse("#343C48"));

    public void SetImage(Bitmap? bitmap)
    {
        _bitmap = bitmap;
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
    }
}
