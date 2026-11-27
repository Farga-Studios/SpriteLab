using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Suite.Updater;

public sealed record UpdateManifest(string Product,string Channel,string Version,string FileName,long Bytes,string Sha256,string Notes);
public sealed class UpdateClient(HttpClient http, Uri manifestUri, string publicKeyPem, string product, string channel)
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public bool Configured=>!string.IsNullOrWhiteSpace(publicKeyPem);
    private async Task<byte[]> ReadSmallAsync(Uri uri,int limit,CancellationToken token)
    {
        using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        await using var stream=await response.Content.ReadAsStreamAsync(token);
        using var output=new MemoryStream();var buffer=new byte[8192];
        while(true){int n=await stream.ReadAsync(buffer,token);if(n==0)break;if(output.Length+n>limit)throw new InvalidDataException("Update metadata exceeds limit.");output.Write(buffer,0,n);}
        return output.ToArray();
    }
    public async Task<UpdateManifest?> CheckAsync(Version installed,CancellationToken token=default)
    {
        if(!Configured)return null;
        if(manifestUri.Scheme!=Uri.UriSchemeHttps)throw new InvalidDataException("HTTPS required.");
        var data=await ReadSmallAsync(manifestUri,65536,token);
        var signature=await ReadSmallAsync(new Uri(manifestUri.AbsoluteUri+".sig"),8192,token);
        using var rsa=RSA.Create();rsa.ImportFromPem(publicKeyPem);
        if(rsa.KeySize<2048||!rsa.VerifyData(data,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))
            throw new CryptographicException("Update signature invalid.");
        var m=JsonSerializer.Deserialize<UpdateManifest>(data,Json)??throw new InvalidDataException("Missing manifest.");
        Validate(m);
        return System.Version.Parse(m.Version)>installed?m:null;
    }
    private Uri PackageUri(UpdateManifest m)
    {
        var slug=product.ToLowerInvariant();
        if(!slug.All(c=>char.IsAsciiLetterOrDigit(c)||c=='-'))throw new InvalidDataException("Invalid product slug.");
        return new Uri($"https://github.com/Sealsworn/DevelopmentSuite-Downloads/releases/download/{slug}-v{m.Version}/{m.FileName}");
    }
    private void Validate(UpdateManifest m)
    {
        if(m.Product!=product||m.Channel!=channel||!System.Version.TryParse(m.Version,out var version)||
            version.Build<0||version.Revision>=0||m.FileName!=$"{product}-{m.Version}-win-x64-setup.exe"||
            m.Bytes is <1 or >536870912||m.Sha256 is null||m.Sha256.Length!=64||!m.Sha256.All(Uri.IsHexDigit)||
            m.Notes is null||m.Notes.Length>20000)throw new InvalidDataException("Invalid update manifest.");
    }
    public async Task<string> DownloadAsync(UpdateManifest m,string root,IProgress<int>? progress,CancellationToken token=default)
    {
        if(!Configured)throw new InvalidOperationException("Update trust not configured.");
        Validate(m);
        // Reauthenticate metadata to prevent callers substituting an unsigned record.
        var authenticated=await CheckAsync(new Version(0,0,0),token);
        if(authenticated!=m)throw new CryptographicException("Update metadata changed.");
        Directory.CreateDirectory(root);
        var path=Path.Combine(root,Guid.NewGuid().ToString("N")+"-"+m.FileName);
        try
        {
            using var response=await http.GetAsync(PackageUri(m),HttpCompletionOption.ResponseHeadersRead,token);
            response.EnsureSuccessStatusCode();
            await using var input=await response.Content.ReadAsStreamAsync(token);
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long total=0;int previous=-1;
            await using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))
            {
                var buffer=new byte[65536];
                while(true){int n=await input.ReadAsync(buffer,token);if(n==0)break;total+=n;if(total>m.Bytes)throw new InvalidDataException("Update exceeds declared length.");hash.AppendData(buffer,0,n);await output.WriteAsync(buffer.AsMemory(0,n),token);int p=(int)(total*100/m.Bytes);if(p!=previous){progress?.Report(p);previous=p;}}
                await output.FlushAsync(token);
            }
            if(total!=m.Bytes||!Convert.ToHexString(hash.GetHashAndReset()).Equals(m.Sha256,StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException("Update checksum mismatch.");
            token.ThrowIfCancellationRequested();return path;
        }
        catch{if(File.Exists(path))File.Delete(path);throw;}
    }
    public static async Task VerifyPackageAsync(string path,UpdateManifest m,CancellationToken token=default)
    {
        await using var stream=File.OpenRead(path);
        if(stream.Length!=m.Bytes||!Convert.ToHexString(await SHA256.HashDataAsync(stream,token)).Equals(m.Sha256,StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("Update checksum mismatch.");
    }
}
