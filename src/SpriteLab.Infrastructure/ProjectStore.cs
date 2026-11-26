using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpriteLab.Core;

namespace SpriteLab.Infrastructure;

public sealed record SheetEntry(Guid Id, string Name, string Sha256,
    int Width, int Height, int? CellWidth, int? CellHeight);

public sealed record ProjectManifest(int SchemaVersion, Guid ProjectId, string Name, SheetEntry[] Assets, ViewerClip[]? Clips = null);

public sealed record LoadedProject(ProjectManifest Manifest, string Revision);

public static class ProjectStore
{
    public const string ManifestName = "project.spritelab.json";
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    // Reject existing symbolic links and reparse points throughout the path.
    // This protects normal local use, not against a concurrent hostile filesystem actor.
    private static string SafePath(string path)
    {
        var full = Path.GetFullPath(path);
        for (string? current = full; current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked project paths are not supported.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return full;
    }

    private static string SourcePath(string root, Guid id) =>
        SafePath(Path.Combine(root, "sources", id.ToString("N") + ".png"));

    private static void Validate(ProjectManifest manifest)
    {
        if (manifest.SchemaVersion is not (1 or 2 or 3 or 4) || manifest.ProjectId == Guid.Empty ||
            string.IsNullOrWhiteSpace(manifest.Name) || manifest.Assets is null ||
            manifest.Assets.Length > 256)
            throw new InvalidDataException("Unsupported or invalid project manifest.");

        var ids = new HashSet<Guid>();
        foreach (var sheet in manifest.Assets)
        {
            if (sheet is null || sheet.Id == Guid.Empty || !ids.Add(sheet.Id) ||
                string.IsNullOrWhiteSpace(sheet.Name) || sheet.Sha256 is null ||
                sheet.Sha256.Length != 64 || !sheet.Sha256.All(Uri.IsHexDigit) ||
                sheet.Width <= 0 || sheet.Height <= 0 || sheet.Width > 8192 ||
                sheet.Height > 8192 || (long)sheet.Width * sheet.Height > 16_777_216 ||
                sheet.CellWidth.HasValue != sheet.CellHeight.HasValue)
                throw new InvalidDataException("Invalid sheet metadata.");
            if (sheet.CellWidth is int w && sheet.CellHeight is int h)
                _ = new CellGrid(new PixelSize(sheet.Width, sheet.Height), new PixelSize(w, h));
        }
        if (manifest.SchemaVersion == 1 && manifest.Clips is { Length: > 0 })
            throw new InvalidDataException("Clips require schema 2.");
        if (manifest.SchemaVersion < 3 && (manifest.Clips ?? []).Any(c => c?.Tracks is not null))
            throw new InvalidDataException("Direction tracks require schema 3.");
        ViewerClip.ValidateAll(manifest.Clips ?? [], manifest.Assets);
        if (manifest.SchemaVersion < 4 && (manifest.Clips ?? []).Any(c =>
            c.EffectiveTracks.Any(t => t.Fps.HasValue || t.DurationsMs is not null)))
            throw new InvalidDataException("Preview timing requires schema 4.");
    }

    public static async Task<LoadedProject> LoadAsync(string root, CancellationToken ct = default)
    {
        var path = SafePath(Path.Combine(root, ManifestName));
        var bytes = await ReadManifestAsync(path, ct);
        var manifest = JsonSerializer.Deserialize<ProjectManifest>(bytes, Json)
            ?? throw new InvalidDataException("Empty project manifest.");
        Validate(manifest);
        foreach (var sheet in manifest.Assets)
            _ = await ReadSourceAsync(root, sheet, ct);
        return new LoadedProject(manifest, Hash(bytes));
    }

    private static async Task<byte[]> ReadManifestAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        if (stream.Length > 2 * 1024 * 1024)
            throw new InvalidDataException("Manifest exceeds 2 MiB.");
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        if (buffer.Length > 2 * 1024 * 1024)
            throw new InvalidDataException("Manifest exceeds 2 MiB.");
        return buffer.ToArray();
    }

    public static async Task<byte[]> ReadSourceAsync(string root, SheetEntry sheet,
        CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(SourcePath(root, sheet.Id));
        var bytes = await PngSource.ReadAsync(stream, ct);
        if (!string.Equals(Hash(bytes), sheet.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Source image hash mismatch.");
        var size = PngSource.GetSize(bytes);
        if (size.Width != sheet.Width || size.Height != sheet.Height)
            throw new InvalidDataException("Source image dimensions mismatch.");
        return bytes;
    }

    public static async Task<SheetEntry> ImportAsync(string root, string name, byte[] png,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        using var stream = new MemoryStream(png, writable: false);
        var bytes = await PngSource.ReadAsync(stream, ct);
        var size = PngSource.GetSize(bytes);
        var id = Guid.NewGuid();
        var path = SourcePath(root, id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Unique name, never overwrite an existing source.
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await output.WriteAsync(bytes, ct);
            output.Flush(flushToDisk: true);
        }
        return new SheetEntry(id, name, Hash(bytes), size.Width, size.Height, null, null);
    }

    public static Task<string> SaveAsync(string root, ProjectManifest manifest,
        string? expectedRevision, CancellationToken ct = default) =>
        SaveInternalAsync(root, manifest, null, expectedRevision, ct);

    internal static Task<string> SaveWorkspaceAsync(string root, ProjectManifest manifest,
        IReadOnlyDictionary<Guid, byte[]> sources, string? expectedRevision, CancellationToken ct) =>
        SaveInternalAsync(root, manifest, sources, expectedRevision, ct);

    private static async Task<string> SaveInternalAsync(string root, ProjectManifest manifest,
        IReadOnlyDictionary<Guid, byte[]>? sources, string? expectedRevision, CancellationToken ct)
    {
        Validate(manifest);
        var path = SafePath(Path.Combine(root, ManifestName));
        var backup = SafePath(path + ".bak");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // A cooperative lock prevents two SpriteLab writers from racing.
        await using var gate = new FileStream(SafePath(path + ".lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(path))
        {
            var existing = await ReadManifestAsync(path, ct);
            if (expectedRevision is null || Hash(existing) != expectedRevision)
                throw new IOException("Project changed externally; reopen before saving.");
        }
        else if (expectedRevision is not null)
            throw new IOException("Project manifest disappeared.");

        foreach (var sheet in manifest.Assets)
        {
            var sourcePath = SourcePath(root, sheet.Id);
            if (!File.Exists(sourcePath) && sources is not null)
            {
                if (!sources.TryGetValue(sheet.Id, out var png) ||
                    !string.Equals(Hash(png), sheet.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Missing or changed source.");
                using var input = new MemoryStream(png, writable: false);
                _ = await PngSource.ReadAsync(input, ct);
                Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
                var sourceTemp = SafePath(sourcePath + "." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    await using (var output = new FileStream(sourceTemp, FileMode.CreateNew,
                        FileAccess.Write, FileShare.None))
                    {
                        await output.WriteAsync(png, ct);
                        output.Flush(flushToDisk: true);
                    }
                    ct.ThrowIfCancellationRequested();
                    File.Move(sourceTemp, sourcePath, overwrite: false);
                }
                finally { if (File.Exists(sourceTemp)) File.Delete(sourceTemp); }
            }
            _ = await ReadSourceAsync(root, sheet, ct);
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, Json);
        if (bytes.Length > 2 * 1024 * 1024) throw new InvalidDataException("Manifest exceeds 2 MiB.");
        var temp = SafePath(path + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await output.WriteAsync(bytes, ct);
                output.Flush(flushToDisk: true);
            }
            ct.ThrowIfCancellationRequested();
            if (expectedRevision is null)
                File.Move(temp, path, overwrite: false);
            else
                File.Replace(temp, path, backup);
            return Hash(bytes);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
