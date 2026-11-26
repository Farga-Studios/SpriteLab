namespace SpriteLab.Core;

public sealed class AnimationTrack
{
    public string Direction { get; }
    public IReadOnlyList<Guid> FrameIds { get; }
    public int Fps { get; }
    public IReadOnlyList<int?> DurationsMs { get; }
    public double DurationAt(int index) => PreviewTiming.Duration(Fps, DurationsMs[index]);

    public AnimationTrack(string direction, IEnumerable<Guid> frameIds, int fps = PreviewTiming.DefaultFps, IEnumerable<int?>? durationsMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        var copy = frameIds.ToArray();
        if (copy.Length == 0 || copy.Any(id => id == Guid.Empty))
            throw new ArgumentException("A track needs valid frame references.");
        var durations = durationsMs?.ToArray() ?? new int?[copy.Length];
        if (durations.Length != copy.Length) throw new ArgumentException("Timing length mismatch.");
        _ = PreviewTiming.Duration(fps, null);
        foreach (var duration in durations) _ = PreviewTiming.Duration(fps, duration);
        Fps = fps;
        DurationsMs = Array.AsReadOnly(durations);
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
