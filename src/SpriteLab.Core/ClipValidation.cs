namespace SpriteLab.Core;

public sealed record ValidationTrack(string Direction, int[] Cells, int? Fps = null, int?[]? Durations = null);
public sealed record ValidationClip(string Name, IReadOnlyList<ValidationTrack> Tracks);
public sealed record ClipValidationResult(IReadOnlyList<SheetFinding> Findings, int TotalFindings, int OmittedFindings);

public static class ClipValidation
{
    // Advisory checks on valid workspace data. Structural integrity is enforced separately at persistence.
    public static ClipValidationResult Analyze(CellGrid grid, IReadOnlyDictionary<int, VisibleBounds> bounds,
        IEnumerable<ValidationClip> clips, int limit = SheetValidation.FindingLimit, CancellationToken token = default)
    {
        if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
        var findings = new List<SheetFinding>();
        var total = 0;
        foreach (var clip in clips)
        {
            token.ThrowIfCancellationRequested();
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var track in clip.Tracks)
            {
                token.ThrowIfCancellationRequested();
                var first = track.Cells.Length > 0 && track.Cells[0] >= 0 && track.Cells[0] < grid.Count
                    ? (int?)track.Cells[0] : null;
                var label = track.Direction == "none" ? "Sin dirección" : track.Direction;
                var context = $"{clip.Name} / {label}";
                if (!labels.Add(track.Direction.Trim()))
                    Add("TRACK_LABEL", first, $"{context}: etiqueta similar a otra dirección (mayúsculas o espacios).");
                if (clip.Tracks.Count > 1 && track.Direction == "none")
                    Add("TRACK_UNSPECIFIED", first, $"{context}: pista sin dirección junto a otras direcciones.");
                if (track.Cells.Length == 0)
                    Add("TRACK_EMPTY", null, $"{context}: secuencia sin fotogramas.");
                var emptyOccurrences = 0;
                int? firstEmpty = null;
                foreach (var cell in track.Cells)
                {
                    token.ThrowIfCancellationRequested();
                    if (cell < 0 || cell >= grid.Count)
                    {
                        Add("TRACK_REFERENCE", null, $"{context}: referencia a celda inexistente.");
                        continue;
                    }
                    if (!bounds.ContainsKey(cell)) { emptyOccurrences++; firstEmpty ??= cell; }
                }
                if (emptyOccurrences > 0)
                    Add("TRACK_TRANSPARENT", firstEmpty, $"{context}: {emptyOccurrences} fotogramas transparentes en la secuencia; revisar si son intencionados.");
                if (track.Fps is < 1 or > 60 ||
                    (track.Durations is not null && (track.Durations.Length != track.Cells.Length ||
                        track.Durations.Any(d => d is < 1 or > PreviewTiming.MaxDurationMs))))
                    Add("TRACK_TIMING", first, $"{context}: configuración de tiempos no válida.");
            }
            // Different lengths and timing across directions are allowed, as are free labels and repeated frames.
        }
        return new(findings.AsReadOnly(), total, total - findings.Count);
        void Add(string code, int? cell, string message)
        {
            total++;
            if (findings.Count < limit) findings.Add(new(code, cell, message));
        }
    }
}
