# Changelog

## 0.3.0.3 — Visual consistency
- Shared tool-window palette matching the main workspace, with bordered grid assistant panels and readable detail text.
- Refined original vector icons with consistent 24-unit coordinates, rounded strokes and fixed visual proportions; retain tooltips and accessibility names.
- 0.3.0.2 manual acceptance: 16/16 passed, CI not confirmed. New CI and visual retest pending.

## 0.3.0.2 — Toolbar and playback usability
- Shared vector toolbar icons and tooltips, including accessibility names and dynamic play/pause.
- Millisecond timeline seeking with variable durations, repeated occurrences and playback synchronization.
- Resizable two-panel grid suggestion dialog with independent preview zoom.
- 0.3.0.1 manual checks all passed per user; new CI and targeted usability retest pending.

## 0.3.0.1 — Assisted grid and acceptance fixtures
- Rank exact grid divisors using alpha projections, preview candidates, apply only with confirmation and existing clip protection.
- 0.3.0 CI green, 6/29 manual checks passed; new CI and remaining checks pending.

## 0.3.0 — SL3 acceptance candidate
- Compact diagnostic toolbar and increase default panel height; clear incomplete results on validation failure.
- Reject validation grids above 65,536 cells before pixel extraction to bound per-cell analysis memory; viewer remains unchanged.
- Add cross-detector/report regression and grid-limit tests. 0.2.7.1 CI green confirmed by user; 0.3.0 CI and manual acceptance pending.

## 0.2.7.1 — Diagnostic filter compilation fix
- Remove invalid escaped quotes from interpolation introduced in 0.2.6; calculate filtered count separately. Retain report export from 0.2.7. CI pending.

## 0.2.7 — Validation reports
- Export an unfiltered active-sheet diagnostic snapshot as JSON or UTF-8 text, including metadata, global totals, one-based cell references and explicit omitted count.
- Export invalidated with diagnostics, no project edits; protect the project manifest from report overwrite. Three Core tests. CI and GUI acceptance pending.

## 0.2.6 — Visual diagnostic filters
- Filter diagnostics by category and display optional color-coded cell markers on the sheet, with combined categories and a legend.
- Preserve navigation and full summary; markers cover retained findings only, and truncation notice remains visible. Three Core tests; CI and GUI checks pending.

## 0.2.5 — Animation consistency diagnostics
- Review transparent occurrences, similar direction labels, unspecified directions in multi-track clips and defensive reference/timing checks.
- Preserve free labels, unequal track lengths and deliberate repeated frames. Shared bounded diagnostic list. Five Core tests; CI and manual verification pending.

## 0.2.4 — Visible bounds and alignment review
- Measure cell-local nontransparent bounding boxes and show dimensions/margins in the frame inspector after validation.
- Compare consecutive nonempty occurrences within each direction: advisory center/base shift (20% of cell axis, minimum 2 px) and visible size change (30%, minimum 2 px).
- Share the bounded diagnostics list, retain complete counts and invalidate results when project changes. Four Core tests. CI and manual verification pending.

## 0.2.3 — Exact duplicate cells
- Compare nonempty cells using normalized transparent RGBA, hash lookup and exact collision verification. Reference the first matching cell in sheet order.
- Report duplicates separately from edge warnings; clean count excludes cells with any warning. Empty cells and repeated animation occurrences are not duplicate findings.
- Four additional Core tests. CI and manual verification pending.

## 0.2.2 — SL3 alpha and border diagnostics
- Report exact touched cell edges and complete sheet counts even when the finding list is capped.
- Distinguish possible clipping from confirmed errors; transparent cells remain review warnings.
- Improve diagnostics summary and default panel height. Four additional Core tests. CI and manual validation pending.

## 0.2.1.9 — Final SL2 usability review
- Named project save dialog with automatic child folder creation; safe confirmed overwrite of valid existing SpriteLab projects with revision protection and manifest backup. Non-project occupied directories remain protected.
- Rename project metadata without moving folder; timeline heading, per-frame number/time, Spanish inspector and implicit direction labels.
- Destination validation tests; CI and targeted acceptance pending.

## 0.2.1.8 — SL2 regression candidate
- Synchronize action availability with sheet/clip state; keep existing clip when applying identical grid dimensions.
- Show selected occurrence duration in timeline, wrap resource controls and trim long status/project labels.
- Reuse RGBA conversion for both onion neighbors and disclose tint fallback in UI.
- Record previous manual feedback and targeted retest checklist. No new SL3 work. CI and Windows retest pending.

## 0.2.1.7 — Onion skin and save feedback
- Optional red/green alpha-preserving onion overlays, numerical opacity.
- Save As to a new empty folder with independent project identity; never silently overwrite another project. Same folder uses normal revision protection.
- Persistent save status and error indicator. Two infrastructure tests. CI and manual validation pending.

## 0.2.1.6 — Visual timeline editing
- Drag visible occurrences to reorder; move left/right, duplicate, remove and insert by sheet cell number.
- Preserve exact occurrence timing through moves and duplicates; new occurrences use automatic timing.
- Protect last occurrence, disable edits during playback, retain textual sequence editor. Four Core tests. CI and GUI acceptance pending.

## 0.2.1.5 — SL2 workspace organization
- Split inspector into sheet, preview and frame tabs; automatically reveal sheet configuration on import and preview on clip selection.
- Move track selection, direction operations and timing fields into animation timeline; wrap controls and allow vertical scrolling.
- Increase default timeline height. Keep bounded splitters and reset layout. CI and visual retest pending.

## 0.2.1.4 — SL2 manual feedback fixes
- Keep active animation when selecting a cell in its sequence; explicitly deselect clip when inspecting another cell. Add deselect button.
- Unify new sheet import with sheet switching; restore invalid grid inputs and provide explicit reset.
- Restore selected clip/direction after resource rename.
- Bound panel sizes, clip overflowing content, add reset layout command.
- Three selection regression tests. CI and manual retest required.

## 0.2.1.3 — SL2 acceptance candidate
- Add historical schema 1–4 migration integration tests, source/backup checks and future-schema write protection.
- Move elapsed-time accumulator to testable Core; distinguish pause on last frame from natural completion and retain elapsed time from last timer tick across pause.
- Document manual acceptance gate; CI and Windows checks pending. No new SL3 features.

## 0.2.1.2 — SL2 usability and timing consolidation
- Core tracks retain FPS and per-occurrence timing; schema 5 mapping preserves them.
- Editable ordered cell sequences with repeat support, keyboard playback/navigation, integer preview zoom and middle-button pan.
- Three regression tests added; UI/CI acceptance still pending. SL3 remains frozen.

## 0.2.1.1 — SL2 architecture consolidation
- Schema 5 stores frame IDs and ordered ID references rather than cell-index tracks; validates against the Core project model.
- Schemas 1–4 remain readable; saving explicitly upgrades to schema 5. Stable IDs are derived from immutable sheet/cell identity. Viewer remains a grid adapter; editing arbitrary source rectangles and occurrence timing in Core are not yet implemented.
- Freeze further SL3 features until SL2 acceptance. CI and manual testing pending.

## 0.2.1 — 2026-11-26 (SL3 initial)
- Add local RGBA scan for transparent cells and nonzero alpha at cell edges.
- Navigable warning list, bounded results, cancellation and invalidation on sheet/grid change.
- Findings are heuristic warnings, not artistic approval. No schema change. CI and GUI validation pending.

## 0.2.0 — 2026-11-26 (SL2 candidate)
- Timeline displays actual ordered clip occurrences, including repeated cells, and supports direct occurrence selection.
- Monotonic accumulated preview clock with cycle skipping and variable durations; UI tick resolution remains approximate.
- Guard navigation during busy operations. Schema 4 unchanged.
- SL2 acceptance not yet confirmed: CI, manual flow and full domain Frame-ID migration remain outstanding.

## 0.1.6 — 2026-11-26
- Add previous/next pose overlays with adjustable opacity in the frame inspector, available when playback is paused or stopped.
- Respect direction sequence and loop setting at boundaries; keep overlays out of live playback and persistent metadata.
- Enlarge frame preview to 160 DIP. Add neighbor lookup tests; CI and GUI checks pending.

## 0.1.5 — 2026-11-26
- Add per-track FPS (1–60) and optional per-occurrence preview duration (1–60000 ms), persisted in schema 4.
- Update timer interval per occurrence and allow blank duration to restore FPS default. Older schemas remain readable; older applications cannot open schema 4.
- Add timing validation and regression tests. No gameplay timing semantics. CI and manual verification pending.

## 0.1.4 — 2026-11-26
- Add fixed 8 FPS playback of the selected direction, pause, stop, loop and occurrence navigation.
- Synchronize the frame inspector and sheet highlight with playback; pause before workspace operations.
- Add Core cursor regression tests. No project schema change; variable timing and dedicated clip timeline pending.
- CI and manual verification pending.

## 0.1.3 — 2026-11-25
- Add multiple named direction tracks to sheet-bound clips, assign or replace a track from the selected row, select and remove tracks.
- Read schemas 1 and 2; write schema 3. Existing clips without tracks retain their sequence as the default none track until explicitly assigned.
- Preserve clip IDs and PNGs; keep transitional cell-index representation. Playback and full Core frame-ID mapping remain pending. CI and manual validation pending.

## 0.1.2 — 2026-11-25
- Create named clips from selected grid rows, select and remove clips.
- Save clips in schema 2; read existing schema 1 projects. Older versions cannot open schema 2 files.
- Block resource removal and grid changes while referenced by clips.
- Transitional sheet/cell references, not full Core Frame-ID tracks; playback and directions pending. CI and GUI verification pending; previous icon build not independently confirmed.

## 0.1.1 — 2026-11-25
- Add owner-selected blue pixel S icon with golden selection cell to the Windows executable and Avalonia window.
- Embed a compact 64px rendition derived from the supplied artwork; full-resolution branding assets are distributed separately.
- Update portable artifact naming. Owner reported green CI for 0.1.0; new build verification pending.

## 0.1.0 — 2026-11-25 (candidate)
- Add clickable cell thumbnails following the active selection, and rename/remove resource actions without deleting source files.
- Add self-contained Windows x64 portable build, executable smoke check, ZIP and checksum in private CI.
- Owner reported green CI for 0.0.9. Candidate build and manual validation pending.

## 0.0.9 — 2026-11-25
- Fix ambiguous Core/Avalonia PixelRect in frame preview and rename the resource list to avoid hiding StyledElement.Resources. Owner reported green CI.
- Center the sheet, add integer fit and Ctrl+wheel zoom, and show selected frame preview.
- Replace duplicate resource label with count, add save/open shortcuts and three-choice unsaved dialog.
- Preserve four-component assembly versions where applicable.
- Add four infrastructure tests covering round-trip, backup/conflict, source integrity and PNG header rejection.
- Owner manually demonstrated PNG viewing, grid selection, zoom and project reopen on Windows in previous build. New changes pending CI and GUI checks.

## 0.0.8.1 — 2026-11-25
- Allow later .NET 10 SDK feature bands, including 10.0.404, without selecting .NET 11.
- Update all three Avalonia packages to 11.3.18 to resolve the patched Tmds.DBus.Protocol dependency (GHSA-xrw6-gwf8-vvr9).
- Enable direct and transitive NuGet auditing in CI and fail restore on high or critical vulnerability warnings. CI verification pending.

## 0.0.8 — 2026-11-25
- Add initial local project storage backend: schema-versioned JSON, multiple source entries, immutable PNG copies and SHA-256 checks.
- Add cooperative writer lock, revision conflict detection, temporary manifest and backup replacement.
- Connect project open/save, multi-sheet selection, per-sheet grid settings and unsaved-change confirmation to the existing storage backend.
- Preserve the UUID source naming scheme and cooperative writer lock. Full domain serialization, recovery workflow and infrastructure tests remain pending. CI verification pending.

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
