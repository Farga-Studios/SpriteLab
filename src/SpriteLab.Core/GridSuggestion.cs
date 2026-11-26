namespace SpriteLab.Core;

public sealed record GridSuggestion(int Width, int Height, int Columns, int Rows, double Score)
{
    public override string ToString() => $"{Width} × {Height} px · {Columns} columnas × {Rows} filas";
}

public static class GridSuggestions
{
    public static IReadOnlyList<GridSuggestion> Analyze(PixelSize image, byte[] rgba, CancellationToken token = default)
    {
        if (!image.IsValid || rgba.LongLength != (long)image.Width * image.Height * 4)
            throw new ArgumentException("Invalid pixel buffer.");
        var columns = new int[image.Width];
        var rows = new int[image.Height];
        for (var y = 0; y < image.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < image.Width; x++)
                if (rgba[(y * image.Width + x) * 4 + 3] != 0) { columns[x]++; rows[y]++; }
        }
        if (!columns.Any(v => v > 0)) return [];
        var widths = RankAxis(columns, image.Height);
        var heights = RankAxis(rows, image.Width);
        return widths.SelectMany(w => heights.Select(h => new GridSuggestion(w.Size, h.Size,
                image.Width / w.Size, image.Height / h.Size, w.Score + h.Score -
                Math.Abs(Math.Log((double)w.Size / h.Size)) * 0.08)))
            .Where(s => (long)s.Columns * s.Rows <= SheetValidation.MaxAnalyzedCells)
            .OrderByDescending(s => s.Score).ThenBy(s => s.Width).ThenBy(s => s.Height).Take(8).ToArray();
    }

    private static (int Size, double Score)[] RankAxis(int[] projection, int perpendicular)
    {
        var length = projection.Length;
        var candidates = new List<(int Size, double Score)>();
        int[] common = [8, 12, 16, 20, 24, 32, 40, 48, 56, 64, 72, 80, 96, 112, 128, 160, 192, 256];
        for (var size = 1; size <= length; size++)
        {
            if (length % size != 0 || size < Math.Min(4, length)) continue;
            var bands = length / size;
            var seam = 0.0;
            for (var x = size; x < length; x += size)
                seam += (projection[x - 1] + projection[x]) / (2.0 * perpendicular);
            var occupied = 0;
            var runs = 0;
            for (var start = 0; start < length; start += size)
            {
                var active = false;
                var previous = false;
                for (var x = start; x < start + size; x++)
                {
                    var visible = projection[x] > 0;
                    if (visible && !previous) runs++;
                    active |= visible;
                    previous = visible;
                }
                if (active) occupied++;
            }
            var score = (bands > 1 ? 2.0 - 6.0 * seam / (bands - 1) : 0.5)
                + 1.5 * occupied / bands - 0.7 * Math.Abs(runs - occupied) / Math.Max(1, occupied)
                + (common.Contains(size) ? 0.2 : 0)
                - (bands == 1 && runs > 1 ? 1.0 : 0);
            candidates.Add((size, score));
        }
        return candidates.OrderByDescending(c => c.Score).ThenBy(c => c.Size).Take(12).ToArray();
    }
}
