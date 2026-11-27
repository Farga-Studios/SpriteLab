using SpriteLab.Infrastructure.Pipeline;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;
public sealed class PythonSettingsTests
{
    [Fact]
    public async Task SettingsRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "python.json");
        try
        {
            Assert.Equal("python", (await PythonSettings.LoadAsync(path)).Executable);
            var settings = new PythonSettings("C:/Python/python.exe");
            await settings.SaveAsync(path);
            Assert.Equal(settings, await PythonSettings.LoadAsync(path));
            await new PythonSettings("python3").SaveAsync(path);
            Assert.Equal("python3", (await PythonSettings.LoadAsync(path)).Executable);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Fact]
    public async Task MissingInterpreterIsReported()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-python.exe");
        Assert.False((await PythonProbe.CheckAsync(path)).Available);
    }
}
