using Suite.Updater;

namespace SpriteLab.App.Updates;

/// <summary>Coordinates recovery state under a private per-product session lock.</summary>
internal sealed class UpdateRecoveryService : IDisposable
{
    private readonly UpdateSessionLock session;
    private readonly UpdateJournal journal;
    private readonly UpdateRecovery recovery;
    private readonly VerifiedPackageStore packages;
    private readonly InstallerExitEvidence evidence;

    public UpdateRecoveryService(string root, string publicKey)
    {
        session = UpdateSessionLock.Acquire(root);
        try
        {
            evidence = new InstallerExitEvidence(root);
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

    private void RequireCompletion(Guid id)
    {
        if (!evidence.HasCompletion(id)) throw new InvalidOperationException("No verified installation completion receipt. Manual recovery required.");
    }

    public UpdateTransaction ConfirmHealthy(Guid id, Version running)
    {
        RequireCompletion(id);
        return recovery.ConfirmHealthy(id, running);
    }

    public UpdateTransaction CancelPrepared(Guid id, Version running) => recovery.CancelPrepared(id, running);

    public UpdateTransaction ResolveInterrupted(Guid id, Version running, bool installerExited, bool previousHealthy)
    {
        if (evidence.HasCompletion(id)) throw new InvalidOperationException("Completion receipt exists; manual diagnosis required.");
        var processes = System.Diagnostics.Process.GetProcesses();
        try
        {
            if (processes.Any(p => p.ProcessName.Contains("-win-x64-setup", StringComparison.OrdinalIgnoreCase) ||
                p.ProcessName.Contains("SpriteLab-Setup", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Close all installers before resolving the interruption.");
        }
        finally { foreach (var p in processes) p.Dispose(); }
        return recovery.ResolveInterrupted(id, running, installerExited, previousHealthy, true);
    }

    public UpdateTransaction ConfirmRollbackHealthy(Guid id, Version running)
    {
        RequireCompletion(id);
        return recovery.ConfirmRollbackHealthy(id, running);
    }

    public UpdateTransaction RequireRecovery(Guid id) => recovery.RequireRecovery(id);

    public async Task RollbackAsync(Guid id, Version running, string stagingRoot, bool dataCompatible, CancellationToken token)
    {
        RequireCompletion(id);
        recovery.RequireRecovery(id);
        var manager = new RollbackManager(recovery, packages);
        var update = await manager.PrepareAsync(id, running, true, dataCompatible, stagingRoot, token);
        token.ThrowIfCancellationRequested();
        recovery.MarkRollbackStarted(id, running);
        await InstallerHandoff.LaunchAsync(update, stagingRoot, token, transactionId: id);
    }

    public void Dispose() => session.Dispose();
}
