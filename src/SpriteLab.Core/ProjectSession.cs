namespace SpriteLab.Core;

public sealed record ProjectSnapshot(SpriteProject Project, long Revision);

// Single writer, owned by the application session. Snapshots are immutable.
public sealed class ProjectSession
{
    public ProjectSnapshot Current { get; private set; }

    public ProjectSession(SpriteProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Current = new ProjectSnapshot(project, 0);
    }

    public bool TryReplace(SpriteProject project, long expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (Current.Revision != expectedRevision) return false;
        if (project.Id != Current.Project.Id)
            throw new ArgumentException("Cannot replace a session with a different project.");
        Current = new ProjectSnapshot(project, checked(Current.Revision + 1));
        return true;
    }
}
