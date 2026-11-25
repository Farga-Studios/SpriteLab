# SL1 — Incremental viewer delivery

0.0.6: temporary single-sheet PNG preview, read-only import, checkerboard, 100–800% integer zoom and scrolling. Limits: 64 MiB compressed, 8192 px per dimension, 16,777,216 pixels. Header checks are not full PNG validation; the Avalonia decoder must succeed. Preview is not a persistent project and is not a guarantee of straight RGBA preservation for future export.

Next: configurable grid and cell selection, persistent project folder with protected source copies, infrastructure tests, Windows portable artifact after successful CI. No engine or Python dependency.

Manual checks: open transparent PNG, rectangular sheet, malformed PNG (previous image remains), cancel picker, zoom and scroll, close and rename original file, resize panels and verify DPI. No manual GUI result yet. SL0 CI green reported by owner; manual GUI check and formal ADR closure remain outstanding.

0.0.7: grid configuration defaults to 48 × 48; exact divisibility required. Selection uses zero-based row-major indices internally and one-based labels in the UI. Grid lines can be hidden; selection remains visible. Lines spaced below four DIPs are omitted to limit clutter. Seven new Core tests cover hit testing and bounds. Cells are temporary view selections, not persistent Frame objects; no durations or anchors assigned. Manual check: rectangular grid at multiple zooms, edge clicks, invalid dimensions, next/previous, grid toggle and reimport reset. Next: project persistence.
