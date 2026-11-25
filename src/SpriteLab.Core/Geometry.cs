namespace SpriteLab.Core;

public readonly record struct PixelPoint(int X, int Y);

public readonly record struct PixelSize
{
    public int Width { get; }
    public int Height { get; }

    public PixelSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
        Width = width;
        Height = height;
    }

    public bool IsValid => Width > 0 && Height > 0;
}

public readonly record struct PixelRect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public PixelRect(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid source rectangle.");
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Fits(PixelSize size) =>
        Width > 0 && Height > 0 && X >= 0 && Y >= 0 &&
        (long)X + Width <= size.Width && (long)Y + Height <= size.Height;

    public bool ContainsAnchor(PixelPoint point) =>
        Width > 0 && Height > 0 && point.X >= 0 && point.Y >= 0 &&
        point.X <= Width && point.Y <= Height;
}
