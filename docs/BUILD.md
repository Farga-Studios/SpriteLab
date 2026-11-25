# Build — SL0

Install the .NET 10 SDK (10.0.100 feature band; latest patch allowed).

```powershell
dotnet restore SpriteLab.slnx
dotnet build SpriteLab.slnx -c Release --no-restore
dotnet run --project src/SpriteLab.App -c Release --no-build --no-restore
```

Expected: a console message with the SpriteLab and Avalonia versions. No window until 0.0.3. No Python or game assets required.

The solution uses SLNX instead of legacy SLN, as proposed in the repository structure; this is a minor format change to ADR-SL-001. Avalonia 11.3.8 is a provisional pinned baseline pending successful CI, not a claim that it is the latest or verified secure version. SDK patch roll-forward is constrained to the selected feature band. SkiaSharp and xUnit will be introduced when needed. No tests yet; the workflow executes a smoke check, not a unit test suite.

CI: Windows restore, Release build and smoke check. Record the actual result before closing SL0.
