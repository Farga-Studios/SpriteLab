using System.Security.Cryptography;
using System.Text.Json;

namespace SpriteLab.Infrastructure;

// Manifest describes only files generated inside the export, never project sources.
public sealed record ExportFileHash(string Path,long Bytes,string Sha256);
public sealed record ExportManifest(int SchemaVersion,ExportFileHash[] Files);
public static class ExportIntegrity
{
    public const string ManifestName="integrity.json";
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower};
    private static string Relative(string root,string file)=>Path.GetRelativePath(root,file).Replace('\\','/');
    private static async Task<ExportFileHash> HashAsync(string root,string file,CancellationToken token)
    {
        if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Export contains a symbolic link.");
        await using var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read,65536,true);
        return new(Relative(root,file),stream.Length,Convert.ToHexString(await SHA256.HashDataAsync(stream,token)));
    }
    private static IEnumerable<string> Enumerate(string root)
    {
        var pending=new Stack<string>();pending.Push(root);
        while(pending.Count>0)
        {
            var directory=pending.Pop();
            if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)throw new IOException("Export contains a linked directory.");
            foreach(var child in Directory.EnumerateDirectories(directory))pending.Push(child);
            foreach(var file in Directory.EnumerateFiles(directory))yield return file;
        }
    }
    public static async Task WriteAsync(string root,CancellationToken token=default)
    {
        var manifest=Path.Combine(root,ManifestName);
        var entries=new List<ExportFileHash>();
        foreach(var file in Enumerate(root).Order(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if(Relative(root,file)==ExportRecovery.Marker)continue;
            if(entries.Count>=300000)throw new IOException("Export file limit exceeded.");
            entries.Add(await HashAsync(root,file,token));
        }
        await using var stream=new FileStream(manifest,FileMode.CreateNew,FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream,new ExportManifest(1,entries.ToArray()),Json,token);
    }
    public static async Task VerifyAsync(string root,CancellationToken token=default)
    {
        var path=Path.Combine(root,ManifestName);
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked manifest.");
        ExportManifest? expected;
        await using(var stream=File.OpenRead(path))
        {
            if(stream.Length>100_000_000)throw new InvalidDataException("Manifest exceeds limit.");
            expected=await JsonSerializer.DeserializeAsync<ExportManifest>(stream,Json,token);
        }
        if(expected is null||expected.SchemaVersion!=1||expected.Files is null||expected.Files.Length>300000)
            throw new InvalidDataException("Invalid export manifest.");
        var actual=new Dictionary<string,ExportFileHash>(StringComparer.Ordinal);
        foreach(var file in Enumerate(root))
        {
            token.ThrowIfCancellationRequested();
            if(Relative(root,file) is ManifestName or ExportRecovery.Marker)continue;
            if(actual.Count>=300000)throw new InvalidDataException("Too many files.");
            var entry=await HashAsync(root,file,token);actual.Add(entry.Path,entry);
        }
        if(actual.Count!=expected.Files.Length)throw new InvalidDataException("Export file count mismatch.");
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in expected.Files)
            if(entry is null||entry.Path is null||!seen.Add(entry.Path)||!actual.TryGetValue(entry.Path,out var value)||value!=entry)
                throw new InvalidDataException("Export integrity check failed.");
    }
}
