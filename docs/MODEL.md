# Core model — 0.0.4

The initial domain model follows ADR-SL-002: immutable project, asset, frame, clip and track collections; stable UUIDs; free direction labels; repeated frame references; positive preview durations; half-open source rectangles; local edge-coordinate anchors; and integer offsets. GridLayout returns complete rectangular grids in row-major order and rejects incomplete grids. ProjectSession is a single-writer owner of immutable snapshots with revision checks.

SourceImageId and DerivedImageId are logical references only. PNG decoding, pixel storage, file persistence, validation profiles, metadata serialization, occurrence-level copy-on-write, undo/redo and UI integration are not implemented yet. No gameplay semantics are attached to markers or preview timing.

The schema version constant is reserved for the upcoming persistence contract; these domain classes are not a JSON file format. The default value of a geometry struct is invalid and is rejected at domain boundaries. Frame references are project-wide; duplicated IDs within each entity category and missing references are rejected. Direction labels are case-sensitive.

Next: unit tests for geometry, grid, references, snapshots, anchors and variable frame durations (0.0.5). Then implement import/persistence in SL1.
