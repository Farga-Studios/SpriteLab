using System.Security.Cryptography;

namespace SpriteLab.Infrastructure;

// Temporary viewer state; the persistent format remains owned by ProjectStore.
public sealed record ViewerSheet(Guid Id, string Name, byte[] Png, int? CellWidth, int? CellHeight);
public sealed record ViewerProject(Guid Id, string Name, IReadOnlyList<ViewerSheet> Sheets, IReadOnlyList<ViewerClip>? Clips = null);
public sealed record LoadedViewerProject(ViewerProject Project, string ManifestHash);

public static class ViewerWorkspace
{
    private const long MaxTotalBytes = 256L * 1024 * 1024;
    private const int MaxSheets = 64;

    public static async Task<LoadedViewerProject> LoadAsync(string folder, CancellationToken token)
    {
        var loaded = await ProjectStore.LoadAsync(folder, token);
        if (loaded.Manifest.Assets.Length > MaxSheets)
            throw new InvalidDataException("Too many sheets for the viewer.");
        var sheets = new List<ViewerSheet>();
        long total = 0;
        foreach (var entry in loaded.Manifest.Assets)
        {
            var png = await ProjectStore.ReadSourceAsync(folder, entry, token);
            total += png.Length;
            if (total > MaxTotalBytes) throw new InvalidDataException("Project too large for the viewer.");
            sheets.Add(new ViewerSheet(entry.Id, entry.Name, png, entry.CellWidth, entry.CellHeight));
        }
        return new LoadedViewerProject(new ViewerProject(loaded.Manifest.ProjectId,
            loaded.Manifest.Name, sheets.AsReadOnly(), loaded.Manifest.Clips ?? []), loaded.Revision);
    }

    public static async Task<string> SaveAsync(string folder, ViewerProject project,
        string? expectedHash, CancellationToken token)
    {
        if (project.Sheets.Count > MaxSheets || project.Sheets.Sum(s => (long)s.Png.Length) > MaxTotalBytes)
            throw new InvalidDataException("Project too large for the viewer.");
        var entries = new List<SheetEntry>();
        foreach (var sheet in project.Sheets)
        {
            var size = PngSource.GetSize(sheet.Png);
            var entry = new SheetEntry(sheet.Id, sheet.Name,
                Convert.ToHexString(SHA256.HashData(sheet.Png)), size.Width, size.Height,
                sheet.CellWidth, sheet.CellHeight);
            entries.Add(entry);
        }
        var manifest = new ProjectManifest(4, project.Id, project.Name, entries.ToArray(), project.Clips?.ToArray() ?? []);
        return await ProjectStore.SaveWorkspaceAsync(folder, manifest,
            project.Sheets.ToDictionary(s => s.Id, s => s.Png), expectedHash, token);
    }
}
