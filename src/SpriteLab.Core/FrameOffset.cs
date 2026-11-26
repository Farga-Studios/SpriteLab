namespace SpriteLab.Core;

// Metadata only. A positive X moves right and a positive Y moves down.
public sealed record FrameOffset(int Cell, int X, int Y)
{
    public static FrameOffset Zero(int cell) => new(cell, 0, 0);

    public static FrameOffset[] Set(IReadOnlyList<FrameOffset> current, CellGrid grid, FrameOffset offset)
    {
        Validate(grid, [offset]);
        var result = current.Where(o => o.Cell != offset.Cell).ToList();
        if (offset.X != 0 || offset.Y != 0) result.Add(offset);
        var ordered = result.OrderBy(o => o.Cell).ToArray();
        Validate(grid, ordered);
        return ordered;
    }

    public static void Validate(CellGrid grid, IReadOnlyList<FrameOffset> offsets)
    {
        if (offsets.Count > 4096) throw new ArgumentException("Too many edited cells.");
        var seen = new HashSet<int>();
        foreach (var o in offsets)
            if (o is null || o.Cell < 0 || o.Cell >= grid.Count || !seen.Add(o.Cell) ||
                o.X < -grid.Cell.Width || o.X > grid.Cell.Width ||
                o.Y < -grid.Cell.Height || o.Y > grid.Cell.Height)
                throw new ArgumentException("Invalid frame offset.");
    }
}

public sealed record EditorGuides(bool ShowCenter = true, bool ShowGround = true,
    bool ShowBounds = true, bool ShowCustom = false, int CustomX = 0, int CustomY = 0);
