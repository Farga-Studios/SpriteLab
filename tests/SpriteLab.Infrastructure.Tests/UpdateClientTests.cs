using Suite.Updater;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class UpdateClientTests
{
    [Fact] public async Task MissingTrustFailsClosed()
    {
        using var http=new HttpClient();
        var client=new UpdateClient(http,new Uri("https://example.invalid/update.json"),"","SpriteLab","stable");
        Assert.False(client.Configured);
        Assert.Null(await client.CheckAsync(new Version(0,6,7)));
    }
    [Fact] public async Task CorruptPackageRejected()
    {
        var path=Path.GetTempFileName();
        try{await File.WriteAllBytesAsync(path,[1,2,3]);var m=new UpdateManifest("SpriteLab","stable","0.6.9","SpriteLab-0.6.9-win-x64-setup.exe",3,new string('0',64),"");
        await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(()=>UpdateClient.VerifyPackageAsync(path,m));}
        finally{File.Delete(path);}
    }
}
