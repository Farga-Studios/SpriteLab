using System.Diagnostics;
using Microsoft.Win32;

namespace Suite.Updater;

public sealed record PreparedUpdate(UpdateManifest Manifest,string Path);
public static class InstallerHandoff
{
    public static bool IsInstalled(string applicationDirectory,string uninstallId)
    {
        if(!OperatingSystem.IsWindows())return false;
        try
        {
            using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\"+uninstallId+"_is1");
            if(key?.GetValue("InstallLocation") is not string installed||string.IsNullOrWhiteSpace(installed))return false;
            return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(installed)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(applicationDirectory)),StringComparison.OrdinalIgnoreCase);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException){return false;}
    }
    public static async Task LaunchAsync(PreparedUpdate update,string stagingRoot,CancellationToken token=default)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException();
        var path=Path.GetFullPath(update.Path);
        var parent=Path.TrimEndingDirectorySeparator(Path.GetFullPath(stagingRoot));
        if(!string.Equals(Path.GetDirectoryName(path),parent,StringComparison.OrdinalIgnoreCase)||
            !Path.GetFileName(path).EndsWith("-"+update.Manifest.FileName,StringComparison.Ordinal)||
            (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
            throw new InvalidDataException("Invalid installer staging path.");
        using var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        await UpdateClient.VerifyPackageAsync(path,update.Manifest,token);
        token.ThrowIfCancellationRequested();
        var info=new ProcessStartInfo(path){UseShellExecute=false,WorkingDirectory=parent};
        info.ArgumentList.Add("/SPRITELABUPDATE");
        info.ArgumentList.Add("/NOCLOSEAPPLICATIONS");
        info.ArgumentList.Add("/WAITPID="+Environment.ProcessId);
        using var process=Process.Start(info)??throw new InvalidOperationException("Could not start installer.");
    }
}
