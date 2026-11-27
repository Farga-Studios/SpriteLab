namespace SpriteLab.Infrastructure;

// Explicit recovery only: never delete a directory based on its name alone.
public static class ExportRecovery
{
    public const string Marker=".spritelab-incomplete";
    public static FileStream Begin(string stage)
    {
        Directory.CreateDirectory(stage);
        return new FileStream(Path.Combine(stage,Marker),FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None);
    }
    public static string[] Find(string parent)
    {
        return Directory.EnumerateDirectories(parent,".spritelab-*",SearchOption.TopDirectoryOnly)
            .Where(IsCandidate).ToArray();
    }
    private static bool IsCandidate(string path)
    {
        var name=Path.GetFileName(path);
        var prefix=name.StartsWith(".spritelab-export-",StringComparison.Ordinal)?".spritelab-export-":
            name.StartsWith(".spritelab-batch-",StringComparison.Ordinal)?".spritelab-batch-":"";
        if(prefix.Length==0||!Guid.TryParseExact(name[prefix.Length..],"N",out _))return false;
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)return false;
        var marker=Path.Combine(path,Marker);
        return File.Exists(marker)&&(File.GetAttributes(marker)&FileAttributes.ReparsePoint)==0;
    }
    public static bool TryQuarantine(string parent,string candidate)
    {
        var full=Path.GetFullPath(candidate);
        if(!ViewerWorkspace.SameFolder(Path.GetDirectoryName(full)!,parent)||!IsCandidate(full))return false;
        try
        {
            using(var marker=new FileStream(Path.Combine(full,Marker),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            { if(marker.Length!=0)return false; }
            // Retain data for manual inspection; never recursively delete recovered files.
            Directory.Move(full,Path.Combine(parent,"SpriteLab-recovered-"+Guid.NewGuid().ToString("N")));
            return true;
        }
        catch(IOException){return false;}
        catch(UnauthorizedAccessException){return false;}
    }
}
