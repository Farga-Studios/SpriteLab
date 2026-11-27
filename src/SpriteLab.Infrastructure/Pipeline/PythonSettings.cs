using System.Text.Json;

namespace SpriteLab.Infrastructure.Pipeline;

public sealed record PythonSettings(string Executable = "python")
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpriteLab", "python.json");

    public static async Task<PythonSettings> LoadAsync(string path, CancellationToken token = default)
    {
        if (!File.Exists(path)) return new();
        await using var stream = File.OpenRead(path);
        if (stream.Length > 16384) throw new InvalidDataException("Configuración de Python demasiado grande.");
        var settings = await JsonSerializer.DeserializeAsync<PythonSettings>(stream, cancellationToken: token);
        if (settings is null || string.IsNullOrWhiteSpace(settings.Executable))
            throw new InvalidDataException("Configuración de Python inválida.");
        return settings;
    }

    public async Task SaveAsync(string path, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(Executable) || Executable.Length > 4096)
            throw new ArgumentException("Indica un ejecutable válido.");
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, this, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, full, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
