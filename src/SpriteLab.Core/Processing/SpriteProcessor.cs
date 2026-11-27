namespace SpriteLab.Core.Processing;

// Operations are pure with respect to the original RGBA buffer and preserve the sheet grid.
public enum SpriteOperation { CleanTransparentRgb, AlphaThreshold, MirrorHorizontal, MirrorVertical }

public sealed record ProcessingRequest(SpriteOperation Operation, int[] Cells, int AlphaThreshold = 128);
public sealed record ProcessingResult(byte[] Rgba, int ChangedPixels, int ProcessedCells);

public static class SpriteProcessor
{
    public static ProcessingResult Run(CellGrid grid, byte[] source, ProcessingRequest request,
        IProgress<int>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        if (source.LongLength != (long)grid.Image.Width * grid.Image.Height * 4 ||
            (long)grid.Image.Width * grid.Image.Height > 16_777_216)
            throw new ArgumentException("Invalid RGBA buffer or image dimensions.");
        if (!Enum.IsDefined(request.Operation) || request.AlphaThreshold is < 0 or > 255 ||
            request.Cells is null || request.Cells.Length == 0 || request.Cells.Length > 65536)
            throw new ArgumentException("Invalid processing request.");
        var cells = request.Cells.Distinct().Order().ToArray();
        if (cells.Any(c => c < 0 || c >= grid.Count)) throw new ArgumentException("Invalid cell selection.");
        token.ThrowIfCancellationRequested();
        var result = source.ToArray();
        var changed = 0;
        var completed = 0;
        progress?.Report(0);
        foreach (var index in cells)
        {
            token.ThrowIfCancellationRequested();
            var cell = grid.GetCell(index);
            for (var y = 0; y < cell.Height; y++)
            {
                token.ThrowIfCancellationRequested();
                for (var x = 0; x < cell.Width; x++)
                {
                    var dst = ((cell.Y + y) * grid.Image.Width + cell.X + x) * 4;
                    var sx = request.Operation == SpriteOperation.MirrorHorizontal ? cell.Width - 1 - x : x;
                    var sy = request.Operation == SpriteOperation.MirrorVertical ? cell.Height - 1 - y : y;
                    var src = ((cell.Y + sy) * grid.Image.Width + cell.X + sx) * 4;
                    if (request.Operation is SpriteOperation.MirrorHorizontal or SpriteOperation.MirrorVertical)
                        source.AsSpan(src, 4).CopyTo(result.AsSpan(dst, 4));
                    else if (request.Operation == SpriteOperation.CleanTransparentRgb && source[dst + 3] == 0)
                        result.AsSpan(dst, 3).Clear();
                    else if (request.Operation == SpriteOperation.AlphaThreshold)
                    {
                        if (source[dst + 3] < request.AlphaThreshold) result.AsSpan(dst, 4).Clear();
                        else if (source[dst + 3] != 0) result[dst + 3] = 255;
                    }
                    if (!source.AsSpan(dst, 4).SequenceEqual(result.AsSpan(dst, 4))) changed++;
                }
            }
            completed++;
            progress?.Report((int)(100L * completed / cells.Length));
        }
        token.ThrowIfCancellationRequested();
        return new(result, changed, completed);
    }
}
