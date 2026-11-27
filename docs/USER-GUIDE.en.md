# SpriteLab — User guide
## Installation
Run the Windows x64 installer or extract the entire portable ZIP to a new folder. No Python or .NET SDK is required. Keep projects outside the application directory. Uninstall through Windows Installed Apps; keep your project folders.
## Create a project
File > Import PNG (Ctrl+I). In Sheet, enter cell width and height, then Apply grid. The image must divide exactly. Select a cell and inspect it using zoom. File > Save project (Ctrl+S) creates a workspace with source copies and manifest. Open project (Ctrl+O) reopens it.
## Animation
Select a row cell, then Animation > Create clip from row. Assign rows to directions. Space plays/pauses; arrows step frames. Loop controls repeat. FPS sets base timing, individual duration overrides it. Edit sequence uses one-based cell numbers and permits repeats. Add, replace, duplicate, remove and reorder occurrences.
## Editing
View tab: move sprites, set X/Y offsets, anchors, snapping and guides. Positive X moves right, positive Y down. Anchors are local to the source cell. Auto alignment uses visible bounds or anchor. Batch selection supports 1,3-5,8. Undo/redo and onion skin assist review. Save changes.
## Validation
Tools > Validate sheet (F8). Review empty cells, edge pixels, duplicates, alignment and animation findings. Select a finding to navigate. Warnings are advisory. Export TXT or JSON report.
## Processing
Tools > Image processing. Choose clean transparent RGB, alpha threshold, horizontal or vertical flip. Select cell, row, entire sheet or range. Generate preview; import a new resource without changing the source. Animation metadata is not automatically copied.
## Export
File > Export sprites and animations (Ctrl+E). Choose selected frame, individual frames, corrected sheet, animations or GIF. Save named profiles, preview and batch eligible sheets. GIF supports direction, integer scale, speed, background and repeats; its palette and alpha are limited. Choose a parent folder; exports never overwrite existing output. Offsets are baked, out-of-cell pixels clipped, JSON stores corrected anchors and animation timings. Do not apply baked offsets twice.
## Integrity and recovery
File > Verify export checks integrity.json. Recover incomplete exports quarantines recognized abandoned staging folders without deleting files. Keep external project backups.
## Preferences and help
Edit > Preferences selects system, English or Spanish; restart to apply. Help > Keyboard shortcuts and About. Tab navigates, Escape dismisses dialogs.
## Troubleshooting
Check grid divisibility, clip references, write permissions and free space. Use PNG for exact alpha and color. Preserve the original if project loading fails. Report version, reproduction steps and a non-private example.
