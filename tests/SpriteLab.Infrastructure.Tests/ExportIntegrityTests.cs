using SpriteLab.Infrastructure;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class ExportIntegrityTests
{
    [Fact] public async Task DetectsModifiedMissingAndExtraFiles()
    {
        var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{
            var file=Path.Combine(root,"a.png");await File.WriteAllBytesAsync(file,[1,2,3]);
            await ExportIntegrity.WriteAsync(root);await ExportIntegrity.VerifyAsync(root);
            await File.WriteAllBytesAsync(file,[1,2,4]);
            await Assert.ThrowsAsync<InvalidDataException>(()=>ExportIntegrity.VerifyAsync(root));
            await File.WriteAllBytesAsync(file,[1,2,3]);File.WriteAllText(Path.Combine(root,"extra"),"x");
            await Assert.ThrowsAsync<InvalidDataException>(()=>ExportIntegrity.VerifyAsync(root));
            File.Delete(Path.Combine(root,"extra"));File.Delete(file);
            await Assert.ThrowsAsync<InvalidDataException>(()=>ExportIntegrity.VerifyAsync(root));
        }finally{Directory.Delete(root,true);}
    }
    [Fact] public void RecoveryPreservesFilesAndIgnoresUnmarkedFolders()
    {
        var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{
            var stage=Path.Combine(root,".spritelab-export-"+Guid.NewGuid().ToString("N"));
            using(ExportRecovery.Begin(stage))File.WriteAllText(Path.Combine(stage,"partial"),"preserve");
            Directory.CreateDirectory(Path.Combine(root,".spritelab-export-"+Guid.NewGuid().ToString("N")));
            Assert.Single(ExportRecovery.Find(root));
            Assert.True(ExportRecovery.TryQuarantine(root,stage));
            var recovered=Assert.Single(Directory.EnumerateDirectories(root,"SpriteLab-recovered-*"));
            Assert.Equal("preserve",File.ReadAllText(Path.Combine(recovered,"partial")));
        }finally{Directory.Delete(root,true);}
    }
}
