# Changelog

## 0.0.8 — 2026-11-25
- Add initial local project storage backend: schema-versioned JSON, multiple source entries, immutable PNG copies and SHA-256 checks.
- Add cooperative writer lock, revision conflict detection, temporary manifest and backup replacement.
- Project UI, full domain serialization and recovery workflow remain pending. CI verification pending.

## 0.0.7 — 2026-11-25
- Fix CS0104 in sheet hit testing by explicitly using the Core PixelPoint type; replace a single-item count assertion with Assert.Single (xUnit2013). Rebuild pending.
- Add configurable rectangular grid, optional grid overlay and mouse cell selection.
- Show cell coordinates and navigate previous/next cells; no animation playback or metadata editing yet.
- Add compact Core grid hit testing and seven regression cases.
- Require exact grid divisibility; preserve prior grid on invalid input. CI and manual verification pending.

## 0.0.6 — 2026-11-25
- Add read-only PNG import with bounded input and header validation.
- Add temporary single-sheet preview, checkerboard and integer zoom 100–800%.
- Show filename, resolution and import errors; preserve previous preview on failed decode.
- No project persistence or grid selection yet. Owner reported green CI; manual verification pending.

## 0.0.5 — 2026-11-25
- Add xUnit regression coverage for Core geometry, grid extraction, anchors, references and immutable session snapshots.
- Run unit tests in Windows CI before the non-interactive smoke check.
- No application features or runtime dependencies changed. Owner reported green CI.

## 0.0.4 — 2026-11-25
- Add immutable project, asset, frame, clip, track and marker model.
- Add integer geometry, anchor positioning and rectangular grid splitting.
- Add structural reference checks and single-writer revisioned project snapshots.
- Document model boundaries; no file persistence, UI integration or unit tests yet.
- Owner reported green CI before starting 0.0.5.

## 0.0.3 — 2026-11-25
- Add the initial dark Avalonia desktop window in Spanish.
- Add resizable explorer, viewer, inspector, timeline and diagnostics panels.
- Add Exit, About and assembly-derived version display.
- Disable unimplemented commands; no sprite import or editing yet.
- Make CI smoke check non-interactive. Owner reported green CI; manual GUI verification pending.

## 0.0.2 — 2026-11-25
- Establish the three-project .NET 10 solution and reference graph.
- Configure a provisional pinned Avalonia baseline.
- Add Windows CI for restore, Release build and console smoke check.
- Document build instructions. Owner reported green CI before starting 0.0.3.

## 0.0.1 — 2026-11-25
- Record eight proposed architecture decisions and the implementation roadmap.
- Establish the independent desktop-tool scope.
- No application code or build verification yet.
