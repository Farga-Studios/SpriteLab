# Build — SL0

Install the .NET 10 SDK (10.0.100 feature band; latest patch allowed).

```powershell
dotnet restore SpriteLab.slnx
dotnet build SpriteLab.slnx -c Release --no-restore
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore
```

Expected: a dark desktop window with resizable explorer, viewer, inspector, timeline and diagnostics panels. File and animation commands are disabled placeholders; Exit and About are enabled. No Python or game assets required.

For CI and non-interactive checks:
```powershell
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore -- --smoke-test
```

This checks entry point and Avalonia assembly loading, not actual window initialization. Manual Windows check: open window, resize panels, inspect at 100/150% DPI, open/close About, exit. Record observed results before closing SL0.

The solution uses SLNX instead of legacy SLN. Avalonia 11.3.8 is retained from 0.0.2, whose CI was reported green by the owner. SDK patch roll-forward is constrained to the selected feature band. SkiaSharp and xUnit will be introduced when needed. No unit tests yet.
