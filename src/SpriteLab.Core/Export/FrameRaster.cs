namespace SpriteLab.Core.Export;

public sealed record RasterFrame(byte[] Rgba, int Width, int Height, int ClippedVisiblePixels);

// Fixed-size cells: offset is baked once, pixels outside the cell are clipped, never spilled into neighbors.
public static class FrameRaster
{
    public static RasterFrame Render(CellGrid grid, byte[] source, int cell, FrameOffset offset, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(source);
        if (source.LongLength != (long)grid.Image.Width * grid.Image.Height * 4 ||
            (long)grid.Image.Width * grid.Image.Height > 16_777_216) throw new ArgumentException("Invalid RGBA buffer.");
        if (offset.Cell != cell) throw new ArgumentException("Offset belongs to a different cell.");
        FrameOffset.Validate(grid,[offset]);
        var rect = grid.GetCell(cell);
        var result = new byte[checked(rect.Width * rect.Height * 4)];
        var clipped = 0;
        for (int y=0;y<rect.Height;y++)
        {
            token.ThrowIfCancellationRequested();
            for (int x=0;x<rect.Width;x++)
            {
                var src=((rect.Y+y)*grid.Image.Width+rect.X+x)*4;
                var dx=x+offset.X; var dy=y+offset.Y;
                if (dx<0 || dx>=rect.Width || dy<0 || dy>=rect.Height)
                { if(source[src+3]!=0) clipped++; continue; }
                source.AsSpan(src,4).CopyTo(result.AsSpan((dy*rect.Width+dx)*4,4));
            }
        }
        return new(result,rect.Width,rect.Height,clipped);
    }
    public static RasterFrame RenderSheet(CellGrid grid, byte[] source, IReadOnlyList<FrameOffset> offsets, CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(source.LongLength!=(long)grid.Image.Width*grid.Image.Height*4 || (long)grid.Image.Width*grid.Image.Height>16_777_216)
            throw new ArgumentException("Invalid RGBA buffer.");
        FrameOffset.Validate(grid,offsets);
        token.ThrowIfCancellationRequested();
        var result=source.ToArray(); var clipped=0;
        foreach(var offset in offsets)
        {
            token.ThrowIfCancellationRequested();
            var frame=Render(grid,source,offset.Cell,offset,token);
            var rect=grid.GetCell(offset.Cell);
            for(int y=0;y<rect.Height;y++)
            {
                token.ThrowIfCancellationRequested();
                frame.Rgba.AsSpan(y*rect.Width*4,rect.Width*4).CopyTo(result.AsSpan(((rect.Y+y)*grid.Image.Width+rect.X)*4,rect.Width*4));
            }
            clipped+=frame.ClippedVisiblePixels;
        }
        return new(result,grid.Image.Width,grid.Image.Height,clipped);
    }
}
