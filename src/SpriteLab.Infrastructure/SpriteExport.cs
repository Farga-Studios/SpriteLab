using SpriteLab.Core;
using SpriteLab.Core.Export;
using SpriteLab.Core.Processing;
using System.Text.Json;

namespace SpriteLab.Infrastructure;

public enum SpriteExportKind { SelectedFrame, IndividualFrames, Sheet, Animations, Gif }
public sealed record ExportOutcome(string Folder,int Files,int ClippedVisiblePixels);
public static class SpriteExport
{
    // Export always creates a new directory. No existing destination file or project source is overwritten.
    public static async Task<ExportOutcome> WriteAsync(string parent, ViewerSheet sheet, CellGrid grid, byte[] rgba,
        IReadOnlyList<ViewerClip> clips, SpriteExportKind kind, int selected, CancellationToken token=default, GifOptions? gif=null, Guid? gifClip=null, string? gifDirection=null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if(!Enum.IsDefined(kind) || selected<0 || selected>=grid.Count || grid.Count>65536)
            throw new ArgumentException("Selección de exportación inválida.");
        if(kind==SpriteExportKind.IndividualFrames && grid.Count>4096)
            throw new ArgumentException("Máximo 4096 archivos de fotogramas por exportación.");
        var offsets=sheet.Offsets??[]; var anchors=sheet.Anchors??[];
        FrameOffset.Validate(grid,offsets); FrameAnchor.Validate(grid,anchors);
        var offsetMap=offsets.ToDictionary(x=>x.Cell); var anchorMap=anchors.ToDictionary(x=>x.Cell);
        var relevant=clips.Where(c=>c.SheetId==sheet.Id).ToArray();
        if(kind is SpriteExportKind.Animations or SpriteExportKind.Gif && relevant.Length==0) throw new ArgumentException("La hoja no tiene animaciones.");
        var root=Path.GetFullPath(parent);
        if(!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var id=Guid.NewGuid().ToString("N");
        var stage=Path.Combine(root,".spritelab-export-"+id);
        var final=Path.Combine(root,"SpriteLab-export-"+id);
        Directory.CreateDirectory(stage);
        var files=0; var clipped=0;
        try
        {
            async Task Save(string name, RasterFrame raster)
            {
                token.ThrowIfCancellationRequested();
                var png=RgbaPng.Encode(raster.Width,raster.Height,raster.Rgba,token);
                await File.WriteAllBytesAsync(Path.Combine(stage,name),png,token);
                files++; clipped+=raster.ClippedVisiblePixels;
            }
            object Metadata(int cell,string file, bool inSheet)
            {
                var rect=grid.GetCell(cell);
                var offset=offsetMap.GetValueOrDefault(cell)??FrameOffset.Zero(cell);
                var anchor=anchorMap.GetValueOrDefault(cell);
                return new { cell, file, x=inSheet?rect.X:0, y=inSheet?rect.Y:0, width=rect.Width,height=rect.Height,
                    offset_x=offset.X, offset_y=offset.Y, offset_baked=true,
                    anchor_x=anchor is null?(int?)null:anchor.X+offset.X, anchor_y=anchor is null?(int?)null:anchor.Y+offset.Y };
            }
            var metadata=new List<object>();
            string FrameName(int c)=>$"frame_{c+1:D5}.png";
            if(kind==SpriteExportKind.Gif)
            {
                var options=gif??new GifOptions();options.Validate();
                var targets=relevant.Where(c=>gifClip is null||c.Id==gifClip).SelectMany(c=>c.EffectiveTracks.Where(t=>gifDirection is null||t.Direction==gifDirection).Select(t=>(c,t))).ToArray();
                if(targets.Length is <1 or >256)throw new ArgumentException("Selecciona entre 1 y 256 animaciones/direcciones.");
                foreach(var (clip,track) in targets)
                {
                    token.ThrowIfCancellationRequested();
                    string Safe(string s)=>new string(s.Take(48).Select(c=>char.IsAsciiLetterOrDigit(c)||c=='-'||c=='_'?c:'_').ToArray());
                    var file=$"{files+1:D3}_{Safe(clip.Name)}_{Safe(track.Direction)}.gif";
                    IEnumerable<GifFrame> Frames(){for(int i=0;i<track.Cells.Length;i++){var cell=track.Cells[i];var raster=FrameRaster.Render(grid,rgba,cell,offsetMap.GetValueOrDefault(cell)??FrameOffset.Zero(cell),token);clipped+=raster.ClippedVisiblePixels;yield return new GifFrame(raster,track.DurationAt(i));}}
                    using(var stream=new FileStream(Path.Combine(stage,file),FileMode.CreateNew))AnimatedGif.Write(stream,grid.Cell.Width,grid.Cell.Height,Frames(),options,token);
                    files++;
                }
            }
            else if(kind is SpriteExportKind.SelectedFrame or SpriteExportKind.IndividualFrames)
            {
                IEnumerable<int> cells=kind==SpriteExportKind.SelectedFrame?[selected]:Enumerable.Range(0,grid.Count);
                foreach(var cell in cells)
                {
                    var file=FrameName(cell);
                    await Save(file,FrameRaster.Render(grid,rgba,cell,offsetMap.GetValueOrDefault(cell)??FrameOffset.Zero(cell),token));
                    metadata.Add(Metadata(cell,file,false));
                }
            }
            else
            {
                await Save("sheet.png",FrameRaster.RenderSheet(grid,rgba,offsets,token));
                for(int cell=0;cell<grid.Count;cell++) { token.ThrowIfCancellationRequested(); metadata.Add(Metadata(cell,"sheet.png",true)); }
            }
            if(kind==SpriteExportKind.Gif){token.ThrowIfCancellationRequested();Directory.Move(stage,final);return new(final,files,clipped);}
            var animations=kind==SpriteExportKind.Animations?relevant.Select(c=>new
            {
                id=c.Id,name=c.Name,tracks=c.EffectiveTracks.Select(t=>new
                {
                    direction=t.Direction, fps=t.Fps??PreviewTiming.DefaultFps,
                    frames=t.Cells.Select((cell,index)=>new {cell, duration_ms=t.DurationAt(index)}).ToArray()
                }).ToArray()
            }).ToArray():null;
            var document=new { schema_version=1, sheet_id=sheet.Id, sheet_name=sheet.Name, cell_index_base=0,
                cell_width=grid.Cell.Width,cell_height=grid.Cell.Height,offsets_baked=true,
                clipped_visible_pixels=clipped,frames=metadata,animations };
            await using(var stream=new FileStream(Path.Combine(stage,"sprites.json"),FileMode.CreateNew,FileAccess.Write))
                await JsonSerializer.SerializeAsync(stream,document,new JsonSerializerOptions{WriteIndented=true},token);
            files++;
            token.ThrowIfCancellationRequested();
            Directory.Move(stage,final);
            return new(final,files,clipped);
        }
        finally { if(Directory.Exists(stage)) Directory.Delete(stage,true); }
    }
}
