using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SpriteLab.App.Views;

internal static class UiText
{
    private static readonly IReadOnlyDictionary<string,string> English=Load();
    private static readonly string PreferencePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SpriteLab","preferences-language.txt");
    public static string Preference {get;private set;}="system";
    public static bool IsEnglish {get;private set;}
    private static IReadOnlyDictionary<string,string> Load()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SpriteLab.Localization.en.json");
        return stream is null?new Dictionary<string,string>():JsonSerializer.Deserialize<Dictionary<string,string>>(stream)??new();
    }
    public static void Initialize()
    {
        try{if(File.Exists(PreferencePath)&&new FileInfo(PreferencePath).Length<=16)Preference=File.ReadAllText(PreferencePath).Trim();}catch(IOException){}catch(UnauthorizedAccessException){}
        if(Preference is not ("system" or "es" or "en"))Preference="system";
        IsEnglish=Preference=="en"||(Preference=="system"&&CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="es");
    }
    public static string T(string source)=>IsEnglish&&English.TryGetValue(source,out var translated)?translated:source;
    public static void Save(string preference)
    {
        if(preference is not ("system" or "es" or "en"))throw new ArgumentException("Invalid language.");
        Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
        var tmp=PreferencePath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(tmp,preference);File.Move(tmp,PreferencePath,true);Preference=preference;}
        finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
}
