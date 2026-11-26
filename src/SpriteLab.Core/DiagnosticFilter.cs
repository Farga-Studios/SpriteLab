namespace SpriteLab.Core;

public enum DiagnosticCategory { All, Empty, Edge, Duplicate, Alignment, Animation, Other }

public static class DiagnosticFilter
{
    public static DiagnosticCategory Category(string code) => code switch
    {
        "EMPTY" => DiagnosticCategory.Empty,
        "EDGE" => DiagnosticCategory.Edge,
        "DUPLICATE" => DiagnosticCategory.Duplicate,
        "ALIGNMENT" or "SIZE" => DiagnosticCategory.Alignment,
        _ when code.StartsWith("TRACK_", StringComparison.Ordinal) => DiagnosticCategory.Animation,
        _ => DiagnosticCategory.Other
    };

    public static SheetFinding[] Apply(IEnumerable<SheetFinding> findings, DiagnosticCategory category) =>
        findings.Where(f => f.Code == "LIMIT" || category == DiagnosticCategory.All || Category(f.Code) == category).ToArray();

    public static IReadOnlyDictionary<int, DiagnosticCategory[]> Markers(IEnumerable<SheetFinding> findings) =>
        findings.Where(f => f.Cell.HasValue && f.Code != "LIMIT")
            .GroupBy(f => f.Cell!.Value)
            .ToDictionary(g => g.Key, g => g.Select(f => Category(f.Code)).Distinct().OrderBy(c => c).ToArray());
}
