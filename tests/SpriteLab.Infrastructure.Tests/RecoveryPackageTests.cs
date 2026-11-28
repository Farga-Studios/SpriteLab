using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Suite.Updater;
using Xunit;

public sealed class RecoveryPackageTests : IDisposable
{
    private const string Product = "SpriteLab";
    private readonly string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly RSA rsa = RSA.Create(3072);
    private UpdateJournal Journal => new(root, Product);
    private VerifiedPackageStore Store => new(Path.Combine(root, "packages"), rsa.ExportSubjectPublicKeyInfoPem(), Product, "stable");

    private async Task Retain(string version)
    {
        byte[] bytes = [1,2,3,4];
        var path = Path.Combine(root, "sample.exe");
        File.WriteAllBytes(path, bytes);
        var manifest = new UpdateManifest(Product, "stable", version, $"{Product}-{version}-win-x64-setup.exe", bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), "test");
        var data = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        await Store.RetainAsync(path, data, rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public async Task ReceiptAndSignedPackageSurviveRestartAndRollback()
    {
        using var session = UpdateSessionLock.Acquire(root);
        await Retain("1.0.0");
        await Retain("1.1.0");
        var transaction = Journal.Begin("1.0.0", "1.1.0");
        Journal.Advance(transaction.Id, UpdatePhase.HandoffStarted);
        var evidence = new InstallerExitEvidence(root);
        var receipt = evidence.Prepare(transaction.Id);
        Assert.False(evidence.HasCompletion(transaction.Id));
        File.WriteAllText(receipt, "completed:" + transaction.Id.ToString("N"), Encoding.ASCII);
        Assert.True(new InstallerExitEvidence(root).HasCompletion(transaction.Id));
        var recovery = new UpdateRecovery(Journal);
        Assert.Equal(RecoveryStatus.HealthCheckRequired, recovery.Assess(new Version("1.1.0")).Status);
        recovery.RequireRecovery(transaction.Id);
        var manager = new RollbackManager(recovery, Store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.PrepareAsync(transaction.Id, new Version("1.1.0"), true, false, root));
        var rollback = await manager.PrepareAsync(transaction.Id, new Version("1.1.0"), true, true, root);
        Assert.Equal("1.0.0", rollback.Manifest.Version);
        recovery.MarkRollbackStarted(transaction.Id, new Version("1.1.0"));
        evidence.Prepare(transaction.Id);
        Assert.False(evidence.HasCompletion(transaction.Id));
        File.WriteAllText(receipt, "completed:" + transaction.Id.ToString("N"), Encoding.ASCII);
        Assert.True(evidence.HasCompletion(transaction.Id));
        new UpdateRecovery(Journal).ConfirmRollbackHealthy(transaction.Id, new Version("1.0.0"));
        Assert.Equal(RecoveryStatus.RolledBack, new UpdateRecovery(Journal).Assess(new Version("1.0.0")).Status);
    }

    public void Dispose()
    {
        rsa.Dispose();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
