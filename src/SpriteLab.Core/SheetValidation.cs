namespace SpriteLab.Core;

[Flags]
public enum CellEdges { None = 0, Top = 1, Right = 2, Bottom = 4, Left = 8 }

public sealed record SheetFinding(string Code, int? Cell, string Message)
{
    public CellEdges Edges { get; init; }
    public int? ReferenceCell { get; init; }
    public override string ToString() => Cell is int cell
        ? $"Aviso · Celda {cell + 1}: {Message}" : $"Aviso · {Message}";
}

public sealed record SheetValidationResult(int TotalCells, int EmptyCells, int EdgeCells,
    IReadOnlyList<SheetFinding> Findings, int OmittedFindings, int DuplicateCells = 0, int CleanCellCount = -1)
{
    public IReadOnlyDictionary<int, VisibleBounds> Bounds { get; init; } = new Dictionary<int, VisibleBounds>();
    public int CleanCells => CleanCellCount >= 0 ? CleanCellCount : TotalCells - EmptyCells - EdgeCells;
}

public static class SheetValidation
{
    public const int FindingLimit = 1000;
    public const int MaxAnalyzedCells = 65536;

    public static void CheckGridLimit(CellGrid grid)
    {
        if (grid.Count > MaxAnalyzedCells)
            throw new ArgumentException($"La validación admite hasta {MaxAnalyzedCells} celdas por hoja. La cuadrícula actual tiene {grid.Count}.");
    }

    // Compatibility entry point. The detailed scan counts every cell even if the UI list is capped.
    public static IReadOnlyList<SheetFinding> Scan(CellGrid grid, byte[] rgba, CancellationToken token = default) =>
        Analyze(grid, rgba, token).Findings;

    // Tightly packed straight RGBA. Ignore RGB when alpha is zero.
    public static SheetValidationResult Analyze(CellGrid grid, byte[] rgba, CancellationToken token = default)
    {
        if (rgba.LongLength != (long)grid.Image.Width * grid.Image.Height * 4)
            throw new ArgumentException("Invalid pixel buffer.", nameof(rgba));
        token.ThrowIfCancellationRequested();
        CheckGridLimit(grid);
        var findings = new List<SheetFinding>();
        var empty = 0;
        var edge = 0;
        var duplicates = 0;
        var clean = 0;
        var seen = new Dictionary<ulong, List<int>>();
        var bounds = new Dictionary<int, VisibleBounds>();
        for (var i = 0; i < grid.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var cell = grid.GetCell(i);
            var visible = false;
            ulong hash = 14695981039346656037UL;
            var edges = CellEdges.None;
            int minX = cell.Width, minY = cell.Height, maxX = -1, maxY = -1;
            for (var y = 0; y < cell.Height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (var x = 0; x < cell.Width; x++)
                {
                    var offset = ((cell.Y + y) * grid.Image.Width + cell.X + x) * 4;
                    var alpha = rgba[offset + 3];
                    for (var channel = 0; channel < 4; channel++)
                        hash = unchecked((hash ^ (alpha == 0 ? (byte)0 : rgba[offset + channel])) * 1099511628211UL);
                    if (alpha == 0) continue;
                    visible = true;
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
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
            if (!visible) continue;
            bounds.Add(i, new VisibleBounds(minX, minY, maxX - minX + 1, maxY - minY + 1));
            // A hash is only a candidate lookup; exact comparison prevents collision false positives.
            if (!seen.TryGetValue(hash, out var candidates))
                seen.Add(hash, candidates = new List<int>());
            int? reference = null;
            foreach (var candidate in candidates)
            {
                token.ThrowIfCancellationRequested();
                if (SamePixels(grid, rgba, cell, grid.GetCell(candidate), token))
                { reference = candidate; break; }
            }
            if (reference is int first)
            {
                duplicates++;
                if (findings.Count < FindingLimit)
                    findings.Add(new("DUPLICATE", i, $"Imagen idéntica a la celda {first + 1}; puede ser intencional.") { ReferenceCell = first });
            }
            else
            {
                candidates.Add(i);
                if (edges == CellEdges.None) clean++;
            }
        }
        var omitted = empty + edge + duplicates - findings.Count;
        if (omitted > 0)
            findings.Add(new("LIMIT", null, $"Se omiten {omitted} avisos de la lista. Recuento completo."));
        return new(grid.Count, empty, edge, findings.AsReadOnly(), omitted, duplicates, clean)
        { Bounds = new System.Collections.ObjectModel.ReadOnlyDictionary<int, VisibleBounds>(bounds) };
    }

    private static bool SamePixels(CellGrid grid, byte[] rgba, PixelRect a, PixelRect b, CancellationToken token)
    {
        for (var y = 0; y < a.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < a.Width; x++)
            {
                var left = ((a.Y + y) * grid.Image.Width + a.X + x) * 4;
                var right = ((b.Y + y) * grid.Image.Width + b.X + x) * 4;
                if (rgba[left + 3] != rgba[right + 3]) return false;
                if (rgba[left + 3] != 0 && !rgba.AsSpan(left, 3).SequenceEqual(rgba.AsSpan(right, 3))) return false;
            }
        }
        return true;
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
