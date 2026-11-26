namespace SpriteLab.Core;

// Anchor coordinates are local to the unshifted source cell, inclusive of its edges.
public sealed record FrameAnchor(int Cell, int X, int Y)
{
    public static void Validate(CellGrid grid, IReadOnlyList<FrameAnchor> anchors)
    {
        if (anchors.Count > 4096) throw new ArgumentException("Too many anchors.");
        var seen = new HashSet<int>();
        foreach (var a in anchors)
            if (a is null || a.Cell < 0 || a.Cell >= grid.Count || !seen.Add(a.Cell) ||
                a.X < 0 || a.X > grid.Cell.Width || a.Y < 0 || a.Y > grid.Cell.Height)
                throw new ArgumentException("Invalid frame anchor.");
    }

    public static FrameAnchor[] SetMany(CellGrid grid, IReadOnlyList<FrameAnchor> old,
        IEnumerable<int> cells, int? x, int? y)
    {
        var selected = cells.Distinct().ToHashSet();
        if (selected.Count == 0 || selected.Any(c => c < 0 || c >= grid.Count) || x.HasValue != y.HasValue)
            throw new ArgumentException("Invalid anchor selection.");
        var result = old.Where(a => !selected.Contains(a.Cell)).ToList();
        if (x is int ax && y is int ay) result.AddRange(selected.Select(c => new FrameAnchor(c, ax, ay)));
        var ordered = result.OrderBy(a => a.Cell).ToArray();
        Validate(grid, ordered);
        return ordered;
    }
}

public static class FrameAlignment
{
    public static FrameOffset[] EditMany(CellGrid grid, IReadOnlyList<FrameOffset> old,
        IEnumerable<int> cells, Func<int, FrameOffset, FrameOffset> change)
    {
        var selected = cells.Distinct().ToArray();
        if (selected.Length == 0 || selected.Any(c => c < 0 || c >= grid.Count))
            throw new ArgumentException("Invalid selection.");
        var map = old.ToDictionary(o => o.Cell);
        foreach (var cell in selected)
        {
            var next = change(cell, map.GetValueOrDefault(cell) ?? FrameOffset.Zero(cell));
            if (next.Cell != cell) throw new ArgumentException("Invalid edited cell.");
            if (next.X == 0 && next.Y == 0) map.Remove(cell); else map[cell] = next;
        }
        var result = map.Values.OrderBy(o => o.Cell).ToArray();
        FrameOffset.Validate(grid, result);
        return result;
    }

    public static int Snap(int value, IEnumerable<int> targets, int tolerance)
    {
        if (tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        return targets.Where(t => Math.Abs((long)t - value) <= tolerance)
            .OrderBy(t => Math.Abs((long)t - value)).ThenBy(t => t).Select(t => (int?)t).FirstOrDefault() ?? value;
    }

    public static int[] ParseSelection(string input, int count)
    {
        var result = new SortedSet<int>();
        foreach (var term in input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = term.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length > 2 || !int.TryParse(parts[0], out var first)) throw new ArgumentException("Use 1,3-5.");
            var last = first;
            if (parts.Length == 2 && !int.TryParse(parts[1], out last)) throw new ArgumentException("Invalid range.");
            if (first < 1 || last < first || last > count || last - first >= 4096) throw new ArgumentException("Invalid range.");
            for (var i = first; i <= last; i++) { result.Add(i - 1); if (result.Count > 4096) throw new ArgumentException("Selection exceeds 4096 cells."); }
        }
        if (result.Count == 0) throw new ArgumentException("Empty selection.");
        return result.ToArray();
    }
}
