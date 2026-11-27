using SpriteLab.Core.Export;
using SpriteLab.Infrastructure;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class ExportProfilesTests
{
    [Fact] public async Task ProfileRoundTrip()
    {
        var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        try{var path=Path.Combine(root,"profiles.json");var p=new ExportProfile("GIF",SpriteExportKind.Gif,new GifOptions(4,150,2,false,0xff00ff),true);
            await ExportProfiles.SaveAsync(path,[p]);Assert.Equal(p,Assert.Single(await ExportProfiles.LoadAsync(path)));}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
