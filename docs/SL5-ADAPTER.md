# SL5 — Decisión de arquitectura nativa

El usuario aprobó abandonar la integración Python tras 0.4.2. El núcleo usa RGBA compacto, sin dependencias de UI ni procesos externos. SpriteProcessor.Run recibe cuadrícula, buffer original, operación y selección; devuelve un buffer nuevo, recuentos y progreso. Cancelación cooperativa por fila. Las operaciones conservan tamaño y celdas; no modifican el proyecto ni los PNG fuente. 0.4.4 expondrá vista previa y operaciones; 0.4.5 gestionará ejecución y cancelación; 0.4.6 incorporará resultados derivados sin sobrescribir fuentes. La configuración Python retirada no se migra ni elimina del disco. CI y pruebas de aceptación pendientes.

## 0.4.4–0.4.6
La ventana procesa la imagen original, no la composición con offsets/anclajes. Vista comparativa y zoom; selección por celda, fila, hoja o rango. Cancelación cooperativa. Un resultado sin cambios no se importa. Cambiar opciones invalida la vista anterior. El nuevo recurso tiene ID y PNG propios y conserva cuadrícula; no hereda clips, offsets ni anclajes. Guardar el proyecto persiste ambas fuentes. No sobrescribe el original. Pruebas en SL5-ACCEPTANCE.md.
