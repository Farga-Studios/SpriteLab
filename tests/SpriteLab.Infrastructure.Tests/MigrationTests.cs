using System.Text.Json;
using System.Text.Json.Nodes;
using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class MigrationTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task LegacyManifestUpgradesWithoutChangingSource(int schema)
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab migración " + Guid.NewGuid().ToString("N"));
        try
        {
            var entry = (await ProjectStore.ImportAsync(root, "hoja.png", Png)) with { CellWidth = 1, CellHeight = 1 };
            var projectId = Guid.NewGuid();
            var clipId = Guid.NewGuid();
            // Construct historical JSON without fields introduced later.
            var json = new JsonObject
            {
                ["schema_version"] = schema,
                ["project_id"] = projectId.ToString(),
                ["name"] = "Migración",
                ["assets"] = new JsonArray(JsonSerializer.SerializeToNode(entry,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }))
            };
            if (schema >= 2)
            {
                var clip = new JsonObject
                {
                    ["id"] = clipId.ToString(), ["name"] = "Walk", ["sheet_id"] = entry.Id.ToString(),
                    ["cells"] = new JsonArray(0, 0)
                };
                if (schema >= 3)
                {
                    var track = new JsonObject { ["direction"] = "front", ["cells"] = new JsonArray(0, 0) };
                    if (schema == 4) { track["fps"] = 12; track["durations_ms"] = new JsonArray(null, JsonValue.Create(350)); }
                    clip["tracks"] = new JsonArray(track, new JsonObject
                    { ["direction"] = "custom", ["cells"] = new JsonArray(0) });
                }
                json["clips"] = new JsonArray(clip);
            }
            var path = Path.Combine(root, ProjectStore.ManifestName);
            await File.WriteAllTextAsync(path, json.ToJsonString());
            var original = await File.ReadAllBytesAsync(path);
            var loaded = await ViewerWorkspace.LoadAsync(root, CancellationToken.None);
            Assert.Equal(projectId, loaded.Project.Id);
            Assert.Equal(Png, Assert.Single(loaded.Project.Sheets).Png);
            await ViewerWorkspace.SaveAsync(root, loaded.Project, loaded.ManifestHash, CancellationToken.None);
            var upgraded = await ProjectStore.LoadAsync(root);
            Assert.Equal(5, upgraded.Manifest.SchemaVersion);
            Assert.Equal(original, await File.ReadAllBytesAsync(path + ".bak"));
            Assert.Equal(Png, await ProjectStore.ReadSourceAsync(root, entry));
            var restored = await ViewerWorkspace.LoadAsync(root, CancellationToken.None);
            if (schema == 1) Assert.Empty(restored.Project.Clips!);
            else
            {
                var clip = Assert.Single(restored.Project.Clips!);
                Assert.Equal(clipId, clip.Id);
                Assert.Equal(new[] { 0, 0 }, clip.Cells);
                Assert.Equal(schema >= 3 ? 2 : 1, clip.EffectiveTracks.Count);
                Assert.Equal(schema >= 3 ? "front" : "none", clip.EffectiveTracks[0].Direction);
                if (schema == 4)
                {
                    Assert.Equal((int?)12, clip.EffectiveTracks[0].Fps);
                    Assert.Equal(new int?[] { null, 350 }, clip.EffectiveTracks[0].DurationsMs);
                }
                var idTrack = upgraded.Manifest.AnimationData!.Clips[0].Tracks[0];
                Assert.Equal(idTrack.FrameIds[0], idTrack.FrameIds[1]);
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task UnknownSchemaCannotBeOverwrittenEvenWithMatchingRevision()
    {
        var root = Path.Combine(Path.GetTempPath(), "SpriteLab-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var id = Guid.NewGuid();
            var path = Path.Combine(root, ProjectStore.ManifestName);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schema_version = 99, project_id = id, name = "Future", assets = Array.Empty<object>() });
            await File.WriteAllBytesAsync(path, bytes);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            await Assert.ThrowsAsync<InvalidDataException>(() => ViewerWorkspace.SaveAsync(root, new ViewerProject(id, "Test", []), hash, CancellationToken.None));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
