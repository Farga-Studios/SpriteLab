# Build — SL1

Install the .NET 10 SDK (10.0.100 feature band; latest patch allowed).

```powershell
dotnet restore SpriteLab.slnx
dotnet build SpriteLab.slnx -c Release --no-restore
dotnet test SpriteLab.slnx -c Release --no-build --no-restore
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore
```

Expected: a dark desktop window with resizable explorer, viewer, inspector, timeline and diagnostics panels. Import PNG, zoom, Exit and About are enabled; remaining commands are placeholders. No Python or game assets required.

For CI and non-interactive checks:
```powershell
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore -- --smoke-test
```

This checks entry point and Avalonia assembly loading, not actual window initialization. Manual Windows check: open window, resize panels, inspect at 100/150% DPI, open/close About, exit. Record observed results before closing SL0.

The solution uses SLNX instead of legacy SLN. Avalonia 11.3.8 is retained from 0.0.2, whose CI was reported green by the owner. SDK patch roll-forward is constrained to the selected feature band. SkiaSharp remains deferred. Core unit tests use xUnit 2.9.3, Visual Studio runner 3.1.5 and Microsoft.NET.Test.Sdk 17.14.1; owner reported green CI for these pinned test-only versions. Infrastructure tests will be introduced with actual I/O. No Python or game assets are required.
