namespace SpriteLab.Core;

[Flags]
public enum CellEdges { None = 0, Top = 1, Right = 2, Bottom = 4, Left = 8 }

public sealed record SheetFinding(string Code, int? Cell, string Message)
{
    public CellEdges Edges { get; init; }
    public override string ToString() => Cell is int cell
        ? $"Aviso · Celda {cell + 1}: {Message}" : $"Aviso · {Message}";
}

public sealed record SheetValidationResult(int TotalCells, int EmptyCells, int EdgeCells,
    IReadOnlyList<SheetFinding> Findings, int OmittedFindings)
{
    public int CleanCells => TotalCells - EmptyCells - EdgeCells;
}

public static class SheetValidation
{
    public const int FindingLimit = 1000;

    // Compatibility entry point. The detailed scan counts every cell even if the UI list is capped.
    public static IReadOnlyList<SheetFinding> Scan(CellGrid grid, byte[] rgba, CancellationToken token = default) =>
        Analyze(grid, rgba, token).Findings;

    // Tightly packed RGBA, straight or premultiplied alpha; only alpha is inspected.
    public static SheetValidationResult Analyze(CellGrid grid, byte[] rgba, CancellationToken token = default)
    {
        if (rgba.LongLength != (long)grid.Image.Width * grid.Image.Height * 4)
            throw new ArgumentException("Invalid pixel buffer.", nameof(rgba));
        token.ThrowIfCancellationRequested();
        var findings = new List<SheetFinding>();
        var empty = 0;
        var edge = 0;
        for (var i = 0; i < grid.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var cell = grid.GetCell(i);
            var visible = false;
            var edges = CellEdges.None;
            for (var y = 0; y < cell.Height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (var x = 0; x < cell.Width; x++)
                {
                    var alpha = rgba[((cell.Y + y) * grid.Image.Width + cell.X + x) * 4 + 3];
                    if (alpha == 0) continue;
                    visible = true;
                    if (y == 0) edges |= CellEdges.Top;
                    if (x == cell.Width - 1) edges |= CellEdges.Right;
                    if (y == cell.Height - 1) edges |= CellEdges.Bottom;
                    if (x == 0) edges |= CellEdges.Left;
                }
            }
            if (!visible)
            {
                empty++;
                if (findings.Count < FindingLimit)
                    findings.Add(new("EMPTY", i, "Fotograma completamente transparente."));
            }
            else if (edges != CellEdges.None)
            {
                edge++;
                if (findings.Count < FindingLimit)
                    findings.Add(new("EDGE", i, $"Píxeles visibles en {Describe(edges)}; posible recorte.") { Edges = edges });
            }
        }
        var omitted = empty + edge - findings.Count;
        if (omitted > 0)
            findings.Add(new("LIMIT", null, $"Se omiten {omitted} avisos de la lista. Recuento completo."));
        return new(grid.Count, empty, edge, findings.AsReadOnly(), omitted);
    }

    private static string Describe(CellEdges edges)
    {
        var names = new List<string>(4);
        if (edges.HasFlag(CellEdges.Top)) names.Add("arriba");
        if (edges.HasFlag(CellEdges.Right)) names.Add("derecha");
        if (edges.HasFlag(CellEdges.Bottom)) names.Add("abajo");
        if (edges.HasFlag(CellEdges.Left)) names.Add("izquierda");
        return string.Join(", ", names);
    }
}
