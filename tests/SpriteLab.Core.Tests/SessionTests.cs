using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class SessionTests
{
    private static SpriteProject Project(Guid id, string name) =>
        new(id, name, Array.Empty<SpriteAsset>(), Array.Empty<AnimationClip>());

    [Fact]
    public void SuccessfulReplacementIncrementsRevisionAndPreservesOldSnapshot()
    {
        var id = Guid.NewGuid();
        var session = new ProjectSession(Project(id, "Before"));
        var old = session.Current;
        Assert.Equal(0L, old.Revision);
        Assert.True(session.TryReplace(Project(id, "After"), old.Revision));
        Assert.Equal(1L, session.Current.Revision);
        Assert.Equal("After", session.Current.Project.Name);
        Assert.Equal("Before", old.Project.Name);
    }

    [Fact]
    public void StaleResultCannotOverwriteNewRevision()
    {
        var id = Guid.NewGuid();
        var session = new ProjectSession(Project(id, "Start"));
        Assert.True(session.TryReplace(Project(id, "New"), 0));
        Assert.False(session.TryReplace(Project(id, "Stale"), 0));
        Assert.Equal("New", session.Current.Project.Name);
        Assert.Equal(1L, session.Current.Revision);
    }

    [Fact]
    public void DifferentProjectCannotReplaceSession()
    {
        var session = new ProjectSession(Project(Guid.NewGuid(), "Original"));
        Assert.Throws<ArgumentException>(() => session.TryReplace(Project(Guid.NewGuid(), "Other"), 0));
        Assert.Equal(0L, session.Current.Revision);
    }
}
