# SL1 — Incremental viewer delivery

0.0.6: temporary single-sheet PNG preview, read-only import, checkerboard, 100–800% integer zoom and scrolling. Limits: 64 MiB compressed, 8192 px per dimension, 16,777,216 pixels. Header checks are not full PNG validation; the Avalonia decoder must succeed. Preview is not a persistent project and is not a guarantee of straight RGBA preservation for future export.

Next: configurable grid and cell selection, persistent project folder with protected source copies, infrastructure tests, Windows portable artifact after successful CI. No engine or Python dependency.

Manual checks: open transparent PNG, rectangular sheet, malformed PNG (previous image remains), cancel picker, zoom and scroll, close and rename original file, resize panels and verify DPI. No manual GUI result yet. SL0 CI green reported by owner; manual GUI check and formal ADR closure remain outstanding.
