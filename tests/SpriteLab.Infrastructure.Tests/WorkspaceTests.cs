using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class WorkspaceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
    private static byte[] Png() => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=");
    private static ViewerProject Project() => new(Guid.NewGuid(), "Prueba", new[] {
        new ViewerSheet(Guid.NewGuid(), "Uno.png", Png(), 1, 1),
        new ViewerSheet(Guid.NewGuid(), "Dos.png", Png(), null, null)
    });
    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task MultipleSheetsRoundTrip()
    {
        var project = Project();
        var revision = await ViewerWorkspace.SaveAsync(_root, project, null, Token);
        var loaded = await ViewerWorkspace.LoadAsync(_root, Token);
        Assert.Equal(revision, loaded.ManifestHash);
        Assert.Equal(project.Id, loaded.Project.Id);
        Assert.Equal(2, loaded.Project.Sheets.Count);
        Assert.Equal(project.Sheets[0].Id, loaded.Project.Sheets[0].Id);
        Assert.Equal(project.Sheets[0].Png, loaded.Project.Sheets[0].Png);
        Assert.Equal((int?)1, loaded.Project.Sheets[0].CellWidth);
        Assert.Null(loaded.Project.Sheets[1].CellWidth);
    }

    [Fact]
    public async Task SaveKeepsPreviousManifestAndRejectsStaleRevision()
    {
        var project = Project();
        var revision = await ViewerWorkspace.SaveAsync(_root, project, null, Token);
        var path = Path.Combine(_root, ProjectStore.ManifestName);
        var previous = await File.ReadAllBytesAsync(path);
        await ViewerWorkspace.SaveAsync(_root, project with { Name = "Nuevo" }, revision, Token);
        Assert.Equal(previous, await File.ReadAllBytesAsync(path + ".bak"));
        await Assert.ThrowsAsync<IOException>(() => ViewerWorkspace.SaveAsync(_root, project, revision, Token));
        Assert.Equal("Nuevo", (await ViewerWorkspace.LoadAsync(_root, Token)).Project.Name);
    }

    [Fact]
    public async Task ModifiedSourceIsRejected()
    {
        var project = Project();
        await ViewerWorkspace.SaveAsync(_root, project, null, Token);
        var path = Path.Combine(_root, "sources", project.Sheets[0].Id.ToString("N") + ".png");
        var changed = Png();
        changed[^1] ^= 1;
        await File.WriteAllBytesAsync(path, changed);
        await Assert.ThrowsAsync<InvalidDataException>(() => ViewerWorkspace.LoadAsync(_root, Token));
    }

    [Fact]
    public async Task InvalidPngHeaderIsRejected()
    {
        using var input = new MemoryStream(new byte[33]);
        await Assert.ThrowsAsync<InvalidDataException>(() => PngSource.ReadAsync(input));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
