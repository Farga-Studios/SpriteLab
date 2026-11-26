namespace SpriteLab.Core;

public sealed record SheetFinding(string Code, int? Cell, string Message)
{
    public override string ToString() => Cell is int cell
        ? $"Aviso · Celda {cell + 1}: {Message}" : $"Aviso · {Message}";
}

public static class SheetValidation
{
    // Pixels are tightly packed RGBA, with alpha in byte 3. No image decoding dependency.
    public static IReadOnlyList<SheetFinding> Scan(CellGrid grid, byte[] rgba, CancellationToken token = default)
    {
        if (rgba.LongLength != (long)grid.Image.Width * grid.Image.Height * 4)
            throw new ArgumentException("Invalid pixel buffer.", nameof(rgba));
        var findings = new List<SheetFinding>();
        const int limit = 1000;
        for (var i = 0; i < grid.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var cell = grid.GetCell(i);
            var visible = false;
            var edge = false;
            for (var y = 0; y < cell.Height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (var x = 0; x < cell.Width; x++)
                {
                    var alpha = rgba[((cell.Y + y) * grid.Image.Width + cell.X + x) * 4 + 3];
                    if (alpha == 0) continue;
                    visible = true;
                    if (x == 0 || y == 0 || x == cell.Width - 1 || y == cell.Height - 1) edge = true;
                }
            }
            if (!visible) findings.Add(new("EMPTY", i, "Fotograma completamente transparente."));
            else if (edge) findings.Add(new("EDGE", i, "Píxeles visibles en el borde; revisar posible recorte."));
            if (findings.Count >= limit)
            {
                findings.Add(new("LIMIT", null, "Límite de avisos alcanzado; análisis parcial."));
                break;
            }
        }
        return findings.AsReadOnly();
    }
}
