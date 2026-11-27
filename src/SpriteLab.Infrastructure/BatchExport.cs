using SpriteLab.Core;
using SpriteLab.Core.Export;
namespace SpriteLab.Infrastructure;

public sealed record ExportInput(ViewerSheet Sheet,CellGrid Grid,byte[] Rgba,int Selected=0);
public static class BatchExport
{
    public static async Task<ExportOutcome> WriteAsync(string parent,IReadOnlyList<ViewerSheet> sheets,
        Func<ViewerSheet,ExportInput> load,IReadOnlyList<ViewerClip> clips,ExportProfile profile,
        IProgress<int>? progress=null,CancellationToken token=default)
    {
        profile.Validate();
        if(sheets.Count is <1 or >64)throw new ArgumentException("Invalid batch.");
        var root=Path.GetFullPath(parent);if(!Directory.Exists(root))throw new DirectoryNotFoundException(root);
        var id=Guid.NewGuid().ToString("N");var stage=Path.Combine(root,".spritelab-batch-"+id);var final=Path.Combine(root,"SpriteLab-batch-"+id);
        using var marker=ExportRecovery.Begin(stage);int files=0,clipped=0,done=0;
        try
        {
            foreach(var sheet in sheets)
            {
                token.ThrowIfCancellationRequested();
                var input=load(sheet);
                var outcome=await SpriteExport.WriteAsync(stage,sheet,input.Grid,input.Rgba,clips,profile.Kind,input.Selected,token,profile.Gif);
                var target=Path.Combine(stage,$"{done+1:D2}_{sheet.Id:N}");
                Directory.Move(outcome.Folder,target);files=checked(files+outcome.Files);clipped=checked(clipped+outcome.ClippedVisiblePixels);
                progress?.Report(++done*100/sheets.Count);
            }
            token.ThrowIfCancellationRequested();await ExportIntegrity.WriteAsync(stage,token);await ExportIntegrity.VerifyAsync(stage,token);token.ThrowIfCancellationRequested();marker.Dispose();File.Delete(Path.Combine(stage,ExportRecovery.Marker));Directory.Move(stage,final);return new(final,files,clipped);
        }
        finally{marker.Dispose();if(Directory.Exists(stage))Directory.Delete(stage,true);}
    }
}
