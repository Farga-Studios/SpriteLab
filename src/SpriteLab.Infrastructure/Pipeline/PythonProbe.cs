using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpriteLab.Infrastructure.Pipeline;

public sealed record PythonProbeResult(bool Available, string Version, string Message);

public static partial class PythonProbe
{
    [GeneratedRegex(@"^Python\s+3\.\d+(?:\.\d+)?(?:[^\r\n]*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();

    public static async Task<PythonProbeResult> CheckAsync(string executable, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("--version");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            if (!process.Start()) return new(false, "", "No se pudo iniciar Python.");
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errors = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var version = (await output).Trim();
            if (version.Length == 0) version = (await errors).Trim();
            return process.ExitCode == 0 && VersionPattern().IsMatch(version)
                ? new(true, version, "Intérprete Python 3 disponible.")
                : new(false, "", "El ejecutable no devolvió una versión válida de Python 3.");
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            cancellationToken.ThrowIfCancellationRequested();
            return new(false, "", "Python no respondió en cinco segundos.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new(false, "", "No se encontró o no se pudo iniciar el intérprete.");
        }
    }
}
