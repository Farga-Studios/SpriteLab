namespace SpriteLab.Core;

public readonly record struct VisibleBounds(int X, int Y, int Width, int Height)
{
    public double CenterX => X + Width / 2.0;
    public int Bottom => Y + Height;
}

public sealed record AlignmentTrack(string Clip, string Direction, int[] Cells);
public sealed record AlignmentResult(IReadOnlyList<SheetFinding> Findings, int TotalFindings, int OmittedFindings);

public static class AlignmentValidation
{
    // Advisory heuristics only. Compare consecutive nonempty occurrences, not unrelated poses or different directions.
    public static AlignmentResult Analyze(CellGrid grid, IReadOnlyDictionary<int, VisibleBounds> bounds,
        IEnumerable<AlignmentTrack> tracks, int limit = SheetValidation.FindingLimit, CancellationToken token = default)
    {
        if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
        var findings = new List<SheetFinding>();
        var total = 0;
        var thresholdX = Math.Max(2.0, grid.Cell.Width * 0.20);
        var thresholdY = Math.Max(2.0, grid.Cell.Height * 0.20);
        var sizeX = Math.Max(2.0, grid.Cell.Width * 0.30);
        var sizeY = Math.Max(2.0, grid.Cell.Height * 0.30);
        foreach (var track in tracks)
        {
            token.ThrowIfCancellationRequested();
            for (var i = 1; i < track.Cells.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                var previous = track.Cells[i - 1];
                var current = track.Cells[i];
                if (previous == current || !bounds.TryGetValue(previous, out var a) ||
                    !bounds.TryGetValue(current, out var b)) continue;
                var dx = Math.Abs(b.CenterX - a.CenterX);
                var dy = Math.Abs(b.Bottom - a.Bottom);
                var dw = Math.Abs(b.Width - a.Width);
                var dh = Math.Abs(b.Height - a.Height);
                var context = $"{track.Clip} / {track.Direction}, fotogramas {i}–{i + 1}";
                if (dx >= thresholdX || dy >= thresholdY)
                    Add("ALIGNMENT", current, previous, $"{context}: cambio de centro X {dx:0.#} px / base Y {dy} px; revisar alineación.");
                if (dw >= sizeX || dh >= sizeY)
                    Add("SIZE", current, previous, $"{context}: cambio de ancho {dw} px / alto {dh} px; revisar tamaño visible.");
            }
        }
        return new(findings.AsReadOnly(), total, total - findings.Count);

        void Add(string code, int cell, int reference, string message)
        {
            total++;
            if (findings.Count < limit)
                findings.Add(new(code, cell, message) { ReferenceCell = reference });
        }
    }
}
