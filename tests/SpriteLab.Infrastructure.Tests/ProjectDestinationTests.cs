using SpriteLab.Infrastructure;
using Xunit;
namespace SpriteLab.Infrastructure.Tests;

public sealed class ProjectDestinationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("CON")]
    [InlineData("LPT1.txt")]
    [InlineData("project.")]
    public void RejectsUnsafeNames(string name) =>
        Assert.Throws<ArgumentException>(() => ProjectDestination.ValidateName(name));

    [Fact]
    public void ResolveUsesNamedChildWithoutCreatingIt()
    {
        var parent = Path.GetTempPath();
        var name = "Proyecto " + Guid.NewGuid().ToString("N");
        var path = ProjectDestination.Resolve(parent, name);
        Assert.Equal(Path.Combine(Path.GetFullPath(parent), name), path);
        Assert.False(Directory.Exists(path));
    }
}
