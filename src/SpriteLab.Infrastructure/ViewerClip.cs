using SpriteLab.Core;

namespace SpriteLab.Infrastructure;

// Transitional persisted clip: stable sheet ID plus ordered indices into its fixed grid.
// Grid changes and removal are blocked while referenced. Full Frame-ID mapping follows in SL2.
public sealed record ViewerClip(Guid Id, string Name, Guid SheetId, int[] Cells)
{
    public override string ToString() => Name;

    public static void ValidateAll(IReadOnlyList<ViewerClip> clips, IReadOnlyList<SheetEntry> sheets)
    {
        if (clips.Count > 256) throw new InvalidDataException("Too many clips.");
        var ids = new HashSet<Guid>();
        var lookup = sheets.ToDictionary(s => s.Id);
        foreach (var clip in clips)
        {
            if (clip is null || clip.Id == Guid.Empty || !ids.Add(clip.Id) ||
                string.IsNullOrWhiteSpace(clip.Name) || clip.Name.Length > 200 ||
                clip.Cells is null || clip.Cells.Length is < 1 or > 4096 ||
                !lookup.TryGetValue(clip.SheetId, out var sheet) ||
                sheet.CellWidth is not int w || sheet.CellHeight is not int h)
                throw new InvalidDataException("Invalid clip.");
            var grid = new CellGrid(new PixelSize(sheet.Width, sheet.Height), new PixelSize(w, h));
            if (clip.Cells.Any(i => i < 0 || i >= grid.Count))
                throw new InvalidDataException("Clip references a missing cell.");
        }
    }
}
