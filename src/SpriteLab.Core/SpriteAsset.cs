namespace SpriteLab.Core;

public sealed class SpriteAsset
{
    public Guid Id { get; }
    public string Name { get; }
    public Guid SourceImageId { get; }
    public PixelSize ImageSize { get; }
    public PixelSize CellSize { get; }
    public IReadOnlyList<Frame> Frames { get; }

    public SpriteAsset(Guid id, string name, Guid sourceImageId, PixelSize imageSize,
        PixelSize cellSize, IEnumerable<Frame> frames)
    {
        if (id == Guid.Empty || sourceImageId == Guid.Empty)
            throw new ArgumentException("Empty identifier.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!imageSize.IsValid || !cellSize.IsValid)
            throw new ArgumentException("Invalid dimensions.");

        var copy = frames.ToArray();
        if (copy.Select(f => f.Id).Distinct().Count() != copy.Length ||
            copy.Any(f => !f.Source.Fits(imageSize)))
            throw new ArgumentException("Duplicate frame ID or out-of-bounds rectangle.");

        Id = id;
        Name = name;
        SourceImageId = sourceImageId;
        ImageSize = imageSize;
        CellSize = cellSize;
        Frames = Array.AsReadOnly(copy);
    }
}
