using System.Buffers.Binary;
using System.Security.Cryptography;
using SpriteLab.Core;

namespace SpriteLab.Infrastructure;

public sealed record StoredFrame(Guid Id, Guid SheetId, int Cell);
public sealed record StoredTrack(string Direction, Guid[] FrameIds, int? Fps, int?[]? DurationsMs);
public sealed record StoredClip(Guid Id, string Name, Guid SheetId, StoredTrack[] Tracks);
public sealed record FrameDocument(StoredFrame[] Frames, StoredClip[] Clips)
{
    // Stable identity for an original, immutable grid cell; not an occurrence ID.
    public static Guid FrameId(Guid sheetId, int cell)
    {
        Span<byte> bytes = stackalloc byte[20];
        sheetId.TryWriteBytes(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], cell);
        return new Guid(SHA256.HashData(bytes).AsSpan(0, 16));
    }

    public static FrameDocument FromViewer(IReadOnlyList<ViewerClip> clips, IReadOnlyList<SheetEntry> sheets)
    {
        ViewerClip.ValidateAll(clips, sheets);
        var frames = new Dictionary<Guid, StoredFrame>();
        var stored = clips.Select(clip => new StoredClip(clip.Id, clip.Name, clip.SheetId,
            clip.EffectiveTracks.Select(track => new StoredTrack(track.Direction,
                track.Cells.Select(cell =>
                {
                    var id = FrameId(clip.SheetId, cell);
                    frames.TryAdd(id, new StoredFrame(id, clip.SheetId, cell));
                    return id;
                }).ToArray(), track.Fps, track.DurationsMs?.ToArray())).ToArray())).ToArray();
        return new FrameDocument(frames.Values.ToArray(), stored);
    }

    public ViewerClip[] ToViewer(IReadOnlyList<SheetEntry> sheets)
    {
        if (Frames is null || Clips is null || Clips.Length > 256)
            throw new InvalidDataException("Invalid frame document.");
        var lookup = new Dictionary<Guid, StoredFrame>();
        var cells = new HashSet<(Guid, int)>();
        var assets = sheets.ToDictionary(s => s.Id);
        foreach (var frame in Frames)
        {
            if (frame is null || frame.Id == Guid.Empty || frame.Id != FrameId(frame.SheetId, frame.Cell) ||
                !lookup.TryAdd(frame.Id, frame) || !cells.Add((frame.SheetId, frame.Cell)) ||
                !assets.TryGetValue(frame.SheetId, out var sheet) ||
                sheet.CellWidth is not int w || sheet.CellHeight is not int h ||
                frame.Cell < 0 || frame.Cell >= new CellGrid(new PixelSize(sheet.Width, sheet.Height), new PixelSize(w, h)).Count)
                throw new InvalidDataException("Invalid frame identity or source cell.");
        }
        var result = new List<ViewerClip>();
        var used = new HashSet<Guid>();
        foreach (var clip in Clips)
        {
            if (clip is null || clip.Tracks is null || clip.Tracks.Length is < 1 or > 64)
                throw new InvalidDataException("Invalid stored clip.");
            var tracks = new List<ViewerTrack>();
            foreach (var track in clip.Tracks)
            {
                if (track is null || track.FrameIds is null || track.FrameIds.Length is < 1 or > 4096)
                    throw new InvalidDataException("Invalid stored track.");
                var indices = new int[track.FrameIds.Length];
                for (var i = 0; i < indices.Length; i++)
                {
                    var id = track.FrameIds[i];
                    if (!lookup.TryGetValue(id, out var frame) || frame.SheetId != clip.SheetId)
                        throw new InvalidDataException("Missing or foreign frame reference.");
                    used.Add(id);
                    indices[i] = frame.Cell;
                }
                tracks.Add(new ViewerTrack(track.Direction, indices, track.Fps, track.DurationsMs?.ToArray()));
            }
            result.Add(new ViewerClip(clip.Id, clip.Name, clip.SheetId, tracks[0].Cells, tracks.ToArray()));
        }
        if (used.Count != lookup.Count) throw new InvalidDataException("Unreferenced frame in document.");
        ViewerClip.ValidateAll(result, sheets);
        return result.ToArray();
    }

    public SpriteProject ToCore(Guid projectId, string name, IReadOnlyList<SheetEntry> sheets)
    {
        _ = ToViewer(sheets);
        var assets = sheets.Where(s => s.CellWidth.HasValue).Select(s =>
        {
            var grid = new CellGrid(new PixelSize(s.Width, s.Height), new PixelSize(s.CellWidth!.Value, s.CellHeight!.Value));
            var frames = Frames.Where(f => f.SheetId == s.Id).Select(f =>
                new Frame(f.Id, grid.GetCell(f.Cell), new PixelPoint(0, 0), new PixelPoint(0, 0), 125));
            return new SpriteAsset(s.Id, s.Name, s.Id, grid.Image, grid.Cell, frames);
        });
        var clips = Clips.Select(c => new AnimationClip(c.Id, c.Name,
            c.Tracks.Select(t => new AnimationTrack(t.Direction, t.FrameIds))));
        return new SpriteProject(projectId, name, assets, clips);
    }
}
