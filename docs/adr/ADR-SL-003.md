# ADR-SL-003 — Formato de proyectos y exportación

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Decisión
Proyecto local como carpeta: project.spritelab.json (schema_version: 1), sources/ (copias de PNG originales), edits/ (derivados), previews/ (regenerable y no necesario para abrir), exports/ (artefactos explícitos). Rutas relativas normalizadas, sin acceso fuera de la carpeta por defecto; rechazar rutas absolutas, .. y escapes mediante enlaces/reparse points. Importar copia los archivos externos, no los modifica. No usar ZIP como almacenamiento editable en la primera versión.

Guardar con archivo temporal en el mismo directorio y reemplazo atómico del JSON cuando el sistema lo permita; escribir primero los derivados con nombres únicos, después el manifiesto que los referencia. No borrar archivos antiguos durante un guardado. Mantener copia del último manifiesto válido; recuperación explícita, sin sobrescribir la versión dañada. Nunca declarar guardado exitoso hasta confirmar escritura. Detectar modificación externa desde la última carga y pedir decisión antes de guardar. Una sesión editable por proyecto inicialmente.

JSON UTF-8, IDs estables, campos explícitos, schema_version independiente de la versión de aplicación. Rechazar escritura sobre versiones futuras desconocidas. Migración futura: copia previa y migración explícita a otra ubicación. Guardado de proyecto no equivale a exportación de sprites.

Exportación PNG RGBA sin redimensionar por defecto, cuadrícula fija con tamaño de celda elegido, fila por pista y columnas ordenadas; validar capacidad y recortes. Si pistas tienen longitud desigual, exigir regla de relleno explícita o exportar por pista. Generar opcionalmente JSON de mapeo con IDs, rectángulos, anclajes y tiempos de preview. Nunca sobreescribir entradas originales; colisiones de salida requieren confirmación, preferir carpeta nueva. Exportar mediante staging; publicar sólo si todas las salidas son válidas. No prometer transacción de directorio en todos los sistemas de archivos.

## Ejemplo conceptual mínimo
```json
{"schema_version":1,"project_id":"UUID","name":"Example","assets":[],"clips":[]}
```
El esquema definitivo se implementa y prueba con ADR 002 durante SL1; el ejemplo no es todavía un archivo aceptado por una aplicación existente.

## Aceptación
Round-trip de proyecto; ruta con espacios y Unicode; archivo faltante, JSON corrupto, versión futura y fallo de disco no destruyen fuentes ni último guardado. Exportación reproducible por contenido de píxeles; comprobar dimensiones, canal alfa y orden de frames. Prueba de intento de exportación sobre fuente y salida parcial.
