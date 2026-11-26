using SpriteLab.Core;
using System.Security.Cryptography;

namespace SpriteLab.Infrastructure;

// Temporary viewer state; the persistent format remains owned by ProjectStore.
public sealed record ViewerSheet(Guid Id, string Name, byte[] Png, int? CellWidth, int? CellHeight, FrameOffset[]? Offsets = null, FrameAnchor[]? Anchors = null);
public sealed record ViewerProject(Guid Id, string Name, IReadOnlyList<ViewerSheet> Sheets, IReadOnlyList<ViewerClip>? Clips = null);
public sealed record LoadedViewerProject(ViewerProject Project, string ManifestHash);

public static class ViewerWorkspace
{
    private const long MaxTotalBytes = 256L * 1024 * 1024;
    private const int MaxSheets = 64;

    public static bool SameFolder(string left, string right) => string.Equals(
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

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
            sheets.Add(new ViewerSheet(entry.Id, entry.Name, png, entry.CellWidth, entry.CellHeight, entry.Offsets?.ToArray(), entry.Anchors?.ToArray()));
        }
        return new LoadedViewerProject(new ViewerProject(loaded.Manifest.ProjectId,
            loaded.Manifest.Name, sheets.AsReadOnly(), loaded.Manifest.AnimationData?.ToViewer(loaded.Manifest.Assets) ?? loaded.Manifest.Clips ?? []), loaded.Revision);
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
                sheet.CellWidth, sheet.CellHeight, sheet.Offsets is { Length: > 0 } ? sheet.Offsets.ToArray() : null,
                sheet.Anchors is { Length: > 0 } ? sheet.Anchors.ToArray() : null);
            entries.Add(entry);
        }
        var animationData = FrameDocument.FromViewer(project.Clips ?? [], entries);
        var manifest = new ProjectManifest(entries.Any(e => e.Anchors is { Length: > 0 }) ? 7 : entries.Any(e => e.Offsets is { Length: > 0 }) ? 6 : 5, project.Id, project.Name, entries.ToArray(), AnimationData: animationData);
        return await ProjectStore.SaveWorkspaceAsync(folder, manifest,
            project.Sheets.ToDictionary(s => s.Id, s => s.Png), expectedHash, token);
    }
}
