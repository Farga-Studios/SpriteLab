# Testing — 0.0.5

Run `dotnet test SpriteLab.slnx -c Release` from the repository root.

The initial suite contains 28 cases (including theory rows): 14 geometry/grid, 11 model, 3 session. Synthetic fixtures only. Windows CI restores, builds, tests and runs the existing non-interactive smoke check.

Coverage: positive geometry, half-open bounds, overflow-safe rectangle checks, rectangular and square grids, anchors and offsets, variable durations, shared references, duplicate/missing IDs, immutable collections and stale revision rejection.

Deferred: PNG decoding and alpha, filesystem/persistence, cancellation, undo/redo, playback, export and Python adapter tests. These require implementation first. Manual UI check and ADR approval remain outstanding. Do not declare SL0 closed until actual test results and manual checks are recorded.
