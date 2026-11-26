# SL4 — 0.3.1–0.3.3

## Contract
Original PNG and grid cell are immutable; per-sheet sparse cell offsets apply to all occurrences of that cell. Positive X right, positive Y down. Offset range ± cell dimension, 4096 edited cells per sheet. Schema 6 only when offsets exist; old projects load with zero offsets. Old versions cannot open schema 6; keep backups. Changing grid while clips or offsets exist is blocked. Removing a sheet removes its edits. Guides are session-only view settings. Source sheet, diagnostics and thumbnails show original pixels; frame preview/playback and onion skin show corrected positions. Export integration, anchors, snap, multi-edit, replacement, undo/redo not part of this delivery.

## Manual acceptance
Use TestAssets/SL3/05_desalineados.png, 01_correcto.png, 08_rectangular.png.
1. Import 01, apply 48×48, select cell 2; Vista. Enter X=5 Y=-4 and Apply; verify right/up motion, other cells unchanged.
2. Enable Mover sprite and drag left mouse; confirm integer coordinates and change indicator. Middle mouse still pans without editing. Reset returns to zero.
3. Create row clip and play; same cell occurrences share offset. Pause, enable onion previous/next; ghost positions reflect their own offsets. Try editing while playing: controls disabled.
4. Enable center, ground, bounds, custom X=12 Y=30, update guides. Zoom and pan: guides follow cell, not moved sprite. Toggle off and update.
5. Save project, close, reopen; offsets preserved. Original PNG bytes and source sheet unchanged. Open an old project; no offsets until edited.
6. Attempt to change grid with offsets: blocked. Reset edited cells and remove associated clips before grid change.
7. Test rectangular fixture 48×64; X/Y limits use independent dimensions. Attempt invalid values; reject without mutation.
8. Confirm 0.3.0.3 icon and dialog colors; recheck timeline and diagnostics.

CI pending; no claims of completed runtime validation.

## 0.3.4–0.3.6 acceptance
Use 05_desalineados.png and 07_animaciones.png from TestAssets/SL3. Vista: select cell 1, preset center bottom and Apply anchor; orange cross moves with sprite. Save/reopen: anchor and offset survive; schema 7 is not readable by older versions. Clear anchor independently of offset. Enable Snap, update guides with custom X/Y, drag near target (3 px). Align center/base on 05, verify visible center and bottom against fixed guides; transparent cells skipped. Anchor alignment moves origin to bottom center, skips unassigned. Batch select `1,3-4` then apply absolute X/Y; cell 2 unaffected. Apply ΔX=2, ΔY=-1 to preserve relative offsets. Apply/remove anchors in batch. Invalid range or out-of-range result rejects the entire edit. Select current row or direction (duplicates deduplicated). Drag always affects only displayed cell. Original PNG and source diagnostics unchanged; anchors are metadata and do not change rendering origin automatically, alignment explicitly applies offsets. CI and runtime testing pending. Undo/redo and replacement follow.
