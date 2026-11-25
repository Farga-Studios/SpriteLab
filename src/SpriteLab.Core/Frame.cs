namespace SpriteLab.Core;

public sealed class Frame
{
    public Guid Id { get; }
    public PixelRect Source { get; }
    public PixelPoint Anchor { get; }
    public PixelPoint Offset { get; }
    public int DurationMs { get; }
    public Guid? DerivedImageId { get; }

    public Frame(Guid id, PixelRect source, PixelPoint anchor, PixelPoint offset,
        int durationMs, Guid? derivedImageId = null)
    {
        if (id == Guid.Empty || derivedImageId == Guid.Empty)
            throw new ArgumentException("Empty identifier.");
        if (!source.ContainsAnchor(anchor))
            throw new ArgumentOutOfRangeException(nameof(anchor));
        if (durationMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMs));

        Id = id;
        Source = source;
        Anchor = anchor;
        Offset = offset;
        DurationMs = durationMs;
        DerivedImageId = derivedImageId;
    }

    public PixelPoint DrawPosition(PixelPoint origin) => new(
        checked(origin.X - Anchor.X + Offset.X),
        checked(origin.Y - Anchor.Y + Offset.Y));
}
