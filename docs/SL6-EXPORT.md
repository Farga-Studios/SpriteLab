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
