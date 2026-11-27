using SpriteLab.Core;
using SpriteLab.Infrastructure;
using System.Text.Json;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class SpriteExportTests
{
    [Fact]
    public async Task AnimationExportPreservesSequenceTimingAndAnchor()
    {
        var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var grid=new CellGrid(new PixelSize(4,1),new PixelSize(2,1));
            byte[] rgba=[1,2,3,255,0,0,0,0,4,5,6,255,0,0,0,0];
            var sheet=new ViewerSheet(Guid.NewGuid(),"test.png",[],2,1,[new FrameOffset(0,1,0)],[new FrameAnchor(0,0,1)]);
            var clip=new ViewerClip(Guid.NewGuid(),"walk",sheet.Id,[0,1,0],[new ViewerTrack("east",[0,1,0],10,[null,150,null])]);
            var result=await SpriteExport.WriteAsync(root,sheet,grid,rgba,[clip],SpriteExportKind.Animations,0);
            Assert.Equal(2,result.Files);
            Assert.True(File.Exists(Path.Combine(result.Folder,"sheet.png")));
            using var json=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(result.Folder,"sprites.json")));
            var frames=json.RootElement.GetProperty("frames");
            Assert.Equal(1,frames[0].GetProperty("anchor_x").GetInt32());
            Assert.True(frames[0].GetProperty("offset_baked").GetBoolean());
            var sequence=json.RootElement.GetProperty("animations")[0].GetProperty("tracks")[0].GetProperty("frames");
            Assert.Equal(0,sequence[2].GetProperty("cell").GetInt32());
            Assert.Equal(150d,sequence[1].GetProperty("duration_ms").GetDouble());
            var second=await SpriteExport.WriteAsync(root,sheet,grid,rgba,[clip],SpriteExportKind.SelectedFrame,0);
            Assert.NotEqual(result.Folder,second.Folder);
            Assert.True(File.Exists(Path.Combine(second.Folder,"frame_00001.png")));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public async Task CancelledExportDoesNotPublishDirectory()
    {
        var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var grid=new CellGrid(new PixelSize(1,1),new PixelSize(1,1));
            var sheet=new ViewerSheet(Guid.NewGuid(),"test.png",[],1,1);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>SpriteExport.WriteAsync(root,sheet,grid,new byte[4],[],SpriteExportKind.Sheet,0,new CancellationToken(true)));
            Assert.Empty(Directory.EnumerateFileSystemEntries(root));
        }
        finally { Directory.Delete(root,true); }
    }
}
