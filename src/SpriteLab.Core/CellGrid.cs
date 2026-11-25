namespace SpriteLab.Core;

// Compact, immutable grid description. No array allocation for hit testing.
public sealed class CellGrid
{
    public PixelSize Image { get; }
    public PixelSize Cell { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int Count { get; }

    public CellGrid(PixelSize image, PixelSize cell)
    {
        if (!image.IsValid || !cell.IsValid ||
            image.Width % cell.Width != 0 || image.Height % cell.Height != 0)
            throw new ArgumentException("Image must contain a complete rectangular grid.");
        Image = image;
        Cell = cell;
        Columns = image.Width / cell.Width;
        Rows = image.Height / cell.Height;
        Count = checked(Columns * Rows);
    }

    public int? HitTest(PixelPoint point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Image.Width || point.Y >= Image.Height)
            return null;
        return point.Y / Cell.Height * Columns + point.X / Cell.Width;
    }

    public PixelRect GetCell(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return new PixelRect(index % Columns * Cell.Width,
            index / Columns * Cell.Height, Cell.Width, Cell.Height);
    }
}
