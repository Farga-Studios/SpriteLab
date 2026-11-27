# SL6 — Exportación 0.5.5
Archivo > Exportar sprites y animaciones. Requiere hoja y cuadrícula activa. Selecciona fotograma, sprites individuales, hoja o hoja con animaciones. Elige carpeta padre. Se crea un directorio nuevo SpriteLab-export-<id>, nunca se sobrescribe una fuente. sprites.json acompaña todos los modos. El índice cell es base cero; nombres frame_00001.png base uno. x/y son coordenadas del rectángulo en su PNG. offset_x/y son informativos: ya aplicados, no volver a aplicar en el juego. anchor_x/y son coordenadas locales corregidas (anclaje original + offset), pueden quedar fuera de la celda; null si no definido. Tamaño fijo de celda, recorte sin invadir vecinas, recuento de píxeles visibles recortados. Animaciones contienen ID, nombre, direcciones, fps efectivo, secuencia y duración efectiva en ms, incluidas repeticiones. No es aún un importador específico de SEALSWORN. Hasta 4096 PNG individuales; máximo 65536 celdas en exportación. Sin perfiles ni exportación de varios recursos todavía.

## Pruebas manuales
1. Importar una hoja asimétrica; aplicar cuadrícula rectangular.
2. Exportar celda seleccionada sin desplazamiento: tamaño y RGBA correctos.
3. Desplazar +X, -Y y exportar: comprobar recorte y transparencia.
4. Exportar hoja: celdas vecinas intactas.
5. Exportar todos los frames; comprobar nombres, dimensiones y JSON.
6. Anclar y desplazar; comprobar coordenada exportada y offset_baked.
7. Crear clip con varias direcciones, repeticiones y duración individual; exportar animaciones y comprobar JSON.
8. Exportar dos veces a misma carpeta padre: dos directorios distintos, original intacto.
9. Cancelar diálogo/carpeta: no crear salida. Simular destino sin permisos: error visible.
10. Guardar y reabrir proyecto; repetir exportación. Verificar que originales no cambiaron.
CI y pruebas Windows pendientes. La vista previa de recorte antes de exportar, cancelación dedicada, perfiles y lotes se completarán en entrega 3.

## 0.5.9
Named profiles stored locally; batch includes sheets with a grid (GIF/animation requires clips). Batch writes to unique staging directory and publishes only after success; cancellation cleans staging on normal exit. GIF exports one file per requested direction, fixed canvas, nearest neighbor scale 1/2/3/4/8, speed 25–400%, repeat 0=infinite (NETSCAPE count), alpha <128 transparent or solid RGB compositing. Palette exact up to 255 opaque colors per frame; fallback fixed RGB palette; 10ms minimum delay, cumulative rounding. Limits 4096 frames, 100M scaled pixels per GIF, 4096 axis, 4M pixels/frame, 1h. Preview is corrected RGBA not quantized output; first direction when all selected. Test external GIF viewer (timing, transparency, repeated frames), batch cancellation, profile persistence and clipping before acceptance. Hard crash recovery and deeper verification remain 0.6.0.

## 0.6.0
Every export has integrity.json with SHA-256 and size for payload files; batch root covers nested manifests. Verify from Archivo > Verificar exportación. Recovery scans only marked staging folders in selected parent, skips locked markers, renames rather than deleting. Crash in the narrow interval after marker removal and before publication can leave an unmarked staging directory; inspect manually. Hashes detect accidental corruption, not malicious replacement of manifest. File count in completion excludes integrity manifests.
