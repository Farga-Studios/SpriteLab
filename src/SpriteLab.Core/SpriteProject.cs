namespace SpriteLab.Core;

public sealed class SpriteProject
{
    public const int SchemaVersion = 1;

    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyList<SpriteAsset> Assets { get; }
    public IReadOnlyList<AnimationClip> Clips { get; }
    public IReadOnlyList<FrameMarker> Markers { get; }

    public SpriteProject(Guid id, string name, IEnumerable<SpriteAsset> assets,
        IEnumerable<AnimationClip> clips, IEnumerable<FrameMarker>? markers = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Empty identifier.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var assetCopy = assets.ToArray();
        var clipCopy = clips.ToArray();
        var markerCopy = markers?.ToArray() ?? Array.Empty<FrameMarker>();
        var frames = assetCopy.SelectMany(a => a.Frames).Select(f => f.Id).ToArray();
        var frameIds = frames.ToHashSet();

        if (frameIds.Count != frames.Length ||
            assetCopy.Select(a => a.Id).Distinct().Count() != assetCopy.Length ||
            clipCopy.Select(c => c.Id).Distinct().Count() != clipCopy.Length ||
            markerCopy.Select(m => m.Id).Distinct().Count() != markerCopy.Length)
            throw new ArgumentException("Duplicate identifier.");

        if (clipCopy.SelectMany(c => c.Tracks).SelectMany(t => t.FrameIds)
                .Any(id => !frameIds.Contains(id)) ||
            markerCopy.Any(m => m.Id == Guid.Empty || !frameIds.Contains(m.FrameId) ||
                string.IsNullOrWhiteSpace(m.Name)))
            throw new ArgumentException("Invalid frame reference or marker.");

        Id = id;
        Name = name;
        Assets = Array.AsReadOnly(assetCopy);
        Clips = Array.AsReadOnly(clipCopy);
        Markers = Array.AsReadOnly(markerCopy);
    }
}
