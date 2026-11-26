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

## 0.3.7–0.3.8 acceptance
Use 01_correcto.png (48×48) and create a row clip. Set duration 250 ms on occurrence 2, replace it with cell 4 using the timeline edit icon. Verify four occurrences remain, only occurrence 2 changes, duration remains 250 ms and source untouched. Cancel or enter invalid cell: no edit. Ctrl+Z restores prior cell, Ctrl+Y reapplies replacement. Drag a sprite and undo once (one history step); apply a batch offset and undo once (all selected cells); assign anchors and align, undo/redo each. Test add/duplicate/remove/reorder/timing changes and clip creation/removal. New edit after undo discards redo. History limited to 64 operations and current sheet/grid, resets when changing resource/grid or opening project; not persisted. Saving preserves current edit state, not history. Undo/redo mark document dirty conservatively even if returned to saved state. No source replacement/import of external PNG in this iteration; replacement means changing one occurrence reference. CI and manual tests pending. SL4 acceptance 0.4.0 is separate.

## 0.4.0
This file records implementation history; the consolidated current acceptance checklist is [SL4-ACCEPTANCE.md](SL4-ACCEPTANCE.md). CI and manual gates remain open.
