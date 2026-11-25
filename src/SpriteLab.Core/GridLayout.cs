namespace SpriteLab.Core;

public static class GridLayout
{
    public static IReadOnlyList<PixelRect> Split(PixelSize image, PixelSize cell)
    {
        if (!image.IsValid || !cell.IsValid ||
            image.Width % cell.Width != 0 || image.Height % cell.Height != 0)
            throw new ArgumentException("Image must contain a complete rectangular grid.");

        var columns = image.Width / cell.Width;
        var rows = image.Height / cell.Height;
        var result = new PixelRect[checked(rows * columns)];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                result[y * columns + x] = new PixelRect(
                    x * cell.Width, y * cell.Height, cell.Width, cell.Height);
        return Array.AsReadOnly(result);
    }
}
