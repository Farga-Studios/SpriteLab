# Build

Install the .NET 10 SDK (10.0.100 or later stable .NET 10 feature band).

```powershell
dotnet restore SpriteLab.slnx
dotnet build SpriteLab.slnx -c Release --no-restore
dotnet test SpriteLab.slnx -c Release --no-build --no-restore
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore
```

Expected: a desktop application with sprite sheet inspection, animation, non-destructive editing, diagnostics and export. No Python or game assets required.

For CI and non-interactive checks:
```powershell
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore -- --smoke-test
```

This checks entry point and Avalonia assembly loading, not actual window initialization. Manual Windows check: open window, resize panels, inspect at 100/150% DPI, open/close About, exit. Record observed results before approving a release candidate.

The solution uses SLNX instead of legacy SLN. Avalonia 11.3.18 is pinned across all three UI packages to resolve GHSA-xrw6-gwf8-vvr9; verify restore and audit in CI. SDK roll-forward uses latestFeature within .NET 10. SkiaSharp remains deferred. Core unit tests use xUnit 2.9.3, Visual Studio runner 3.1.5 and Microsoft.NET.Test.Sdk 17.14.1; owner reported green CI for these pinned test-only versions. No Python or game assets are required.

## Shared updater
The project pins `FargaStudios.Suite.Updater` 0.2.1 from NuGet.org. No GitHub Packages credentials or private feeds are needed. CI validates restore, compilation and tests.
