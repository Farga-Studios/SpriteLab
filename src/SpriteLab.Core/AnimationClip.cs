namespace SpriteLab.Core;

public sealed class AnimationTrack
{
    public string Direction { get; }
    public IReadOnlyList<Guid> FrameIds { get; }

    public AnimationTrack(string direction, IEnumerable<Guid> frameIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        var copy = frameIds.ToArray();
        if (copy.Length == 0 || copy.Any(id => id == Guid.Empty))
            throw new ArgumentException("A track needs valid frame references.");
        Direction = direction;
        FrameIds = Array.AsReadOnly(copy);
    }
}

public sealed class AnimationClip
{
    public Guid Id { get; }
    public string Name { get; }
    public IReadOnlyList<AnimationTrack> Tracks { get; }

    public AnimationClip(Guid id, string name, IEnumerable<AnimationTrack> tracks)
    {
        if (id == Guid.Empty) throw new ArgumentException("Empty identifier.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var copy = tracks.ToArray();
        if (copy.Length == 0 ||
            copy.Select(t => t.Direction).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("A clip needs uniquely named tracks.");
        Id = id;
        Name = name;
        Tracks = Array.AsReadOnly(copy);
    }
}

public sealed record FrameMarker(Guid Id, Guid FrameId, string Name);
