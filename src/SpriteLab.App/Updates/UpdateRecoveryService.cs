using Suite.Updater;

namespace SpriteLab.App.Updates;

/// <summary>Coordinates recovery state under a private per-product session lock.</summary>
internal sealed class UpdateRecoveryService : IDisposable
{
    private readonly UpdateSessionLock session;
    private readonly UpdateJournal journal;
    private readonly UpdateRecovery recovery;
    private readonly VerifiedPackageStore packages;

    public UpdateRecoveryService(string root, string publicKey)
    {
        session = UpdateSessionLock.Acquire(root);
        try
        {
            journal = new UpdateJournal(root, "SpriteLab");
            recovery = new UpdateRecovery(journal);
            packages = new VerifiedPackageStore(Path.Combine(root, "packages"), publicKey, "SpriteLab", "stable");
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public RecoveryAssessment Assess(Version running) => recovery.Assess(running);

    public Task<PreparedUpdate> RetainAsync(UpdateClient client, PreparedUpdate update, CancellationToken token)
        => client.RetainDownloadedAsync(update.Manifest, update.Path, packages, token);

    public UpdateTransaction Begin(Version running, PreparedUpdate update)
        => journal.Begin(new Version(running.Major, running.Minor, Math.Max(0, running.Build)).ToString(3), update.Manifest.Version);

    public UpdateTransaction MarkHandoff(Guid id) => journal.Advance(id, UpdatePhase.HandoffStarted);

    public UpdateTransaction ConfirmHealthy(Guid id, Version running) => recovery.ConfirmHealthy(id, running);

    public UpdateTransaction CancelPrepared(Guid id, Version running) => recovery.CancelPrepared(id, running);

    public void Dispose() => session.Dispose();
}
