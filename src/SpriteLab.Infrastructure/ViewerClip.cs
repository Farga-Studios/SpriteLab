using SpriteLab.Core;

namespace SpriteLab.Infrastructure;

// Transitional persisted clip: stable sheet ID plus ordered indices into its fixed grid.
// Grid changes and removal are blocked while referenced. Full Frame-ID mapping follows in SL2.
public sealed record ViewerTrack(string Direction, int[] Cells, int? Fps = null, int?[]? DurationsMs = null)
{
    public override string ToString() => Direction;

    public double DurationAt(int index) => PreviewTiming.Duration(Fps ?? PreviewTiming.DefaultFps, DurationsMs?[index]);
}

public sealed record ViewerClip(Guid Id, string Name, Guid SheetId, int[] Cells, ViewerTrack[]? Tracks = null)
{
    public override string ToString() => Name;

    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<ViewerTrack> EffectiveTracks => Tracks ?? [new ViewerTrack("none", Cells)];

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
            if (clip.Tracks is null) continue;
            if (clip.Tracks.Length is < 1 or > 64 || clip.Tracks[0] is null ||
                clip.Tracks[0].Cells is null || !clip.Cells.SequenceEqual(clip.Tracks[0].Cells))
                throw new InvalidDataException("Invalid primary track.");
            var directions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var track in clip.Tracks)
            {
                if (track is null || string.IsNullOrWhiteSpace(track.Direction) || track.Direction.Length > 80 ||
                    !directions.Add(track.Direction) || track.Cells is null || track.Cells.Length is < 1 or > 4096 ||
                    track.Cells.Any(i => i < 0 || i >= grid.Count))
                    throw new InvalidDataException("Invalid direction track.");
                if (track.Fps is < 1 or > 60 ||
                    (track.DurationsMs is not null && (track.DurationsMs.Length != track.Cells.Length ||
                        track.DurationsMs.Any(d => d is < 1 or > PreviewTiming.MaxDurationMs))))
                    throw new InvalidDataException("Invalid preview timing.");
            }
        }
    }
}
