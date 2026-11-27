using SpriteLab.Core.Export;
using System.Text.Json;
namespace SpriteLab.Infrastructure;

public sealed record ExportProfile(string Name,SpriteExportKind Kind,GifOptions Gif,bool AllSheets=false)
{
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Name)||Name.Length>80||!Enum.IsDefined(Kind)||Gif is null)throw new ArgumentException("Invalid export profile.");
        Gif.Validate();
    }
}
public static class ExportProfiles
{
    public static string DefaultPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SpriteLab","export-profiles.json");
    public static async Task<ExportProfile[]> LoadAsync(string path,CancellationToken token=default)
    {
        if(!File.Exists(path))return [];
        await using var stream=File.OpenRead(path);
        if(stream.Length>65536)throw new InvalidDataException("Profile file too large.");
        var items=await JsonSerializer.DeserializeAsync<ExportProfile[]>(stream,cancellationToken:token)??[];
        if(items.Length>32||items.Any(p=>p is null)||items.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=items.Length)throw new InvalidDataException("Invalid or duplicate profiles.");
        foreach(var p in items)p.Validate();
        return items;
    }
    public static async Task SaveAsync(string path,ExportProfile[] items,CancellationToken token=default)
    {
        if(items.Length>32||items.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=items.Length)
            throw new ArgumentException("Duplicate or too many profiles.");
        foreach(var p in items)p.Validate();
        var full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var tmp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{await using(var stream=new FileStream(tmp,FileMode.CreateNew))await JsonSerializer.SerializeAsync(stream,items,cancellationToken:token);
            token.ThrowIfCancellationRequested();File.Move(tmp,full,true);}
        finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
}
