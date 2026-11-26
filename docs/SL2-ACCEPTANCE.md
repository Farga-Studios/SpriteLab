# SL2 — Aceptación 0.2.1.8

Estado: candidata pendiente de CI y pruebas manuales. No cerrar SL2 hasta confirmar los resultados. Usar copias de proyectos y registrar versión, Windows y DPI.

## Evidencia anterior: 0.2.1.3
El usuario confirmó CI verde y realizó pruebas manuales. De 58 comprobaciones: 52 correctas, 3 fallos, 3 pendientes. Fallos: cuadrícula rectangular, selección de celdas, recuperación de animación tras inspeccionar hoja. Observaciones adicionales: segundo PNG, pérdida temporal de dirección al renombrar, campos de cuadrícula rechazados, deformaciones de paneles, edición de secuencia poco intuitiva, onion poco visible, falta Guardar como y confirmación persistente. No trasladar automáticamente resultados a la nueva versión.

## Retest prioritario
- [ ] Importar primer y segundo PNG, configurar cuadrículas independientes y alternar recursos.
- [ ] Probar cuadrícula rectangular válida (por ejemplo hoja 192×384 con celdas 48×64); confirmar rechazo de dimensiones que no dividen exactamente la hoja y restauración de valores.
- [ ] Aplicar las mismas dimensiones con clip activo: conservar selección. Intentar cambiarlas con clip asociado: bloquear sin pérdida.
- [ ] Seleccionar celda dentro del clip: mantener pista; fuera del clip: salir del clip, volver a seleccionarlo y reproducir.
- [ ] Deseleccionar clip explícitamente; volver a seleccionarlo.
- [ ] Renombrar recurso con pista seleccionada: conservar clip, dirección y posición.
- [ ] Crear clips y direcciones, cambiar entre tres clips varias veces sin cerrar la aplicación.
- [ ] Arrastrar miniatura visible, mover mediante flechas, duplicar, añadir por número de celda, quitar; no quitar último fotograma.
- [ ] Asignar tiempos distintos a ocurrencias repetidas; comprobar que los tiempos siguen a la ocurrencia al mover/duplicar.
- [ ] Reproducir con/sin bucle, pausa en último frame, reanudar y parar. Comprobar que edición se desactiva durante reproducción.
- [ ] Probar espacio y flechas sin interferir con campos de texto.
- [ ] Inspector Hoja/Vista/Frame; comprobar que los controles de tiempos están en la línea de tiempo.
- [ ] Onion anterior rojo, siguiente verde, ambos, sin tintes, opacidad numérica, ocultación durante reproducción; comprobar ausencia de errores.
- [ ] Zoom y pan; probar extremos de splitters, ventana mínima, maximizada y DPI 100/150%; restablecer distribución.
- [ ] Guardar, verificar indicador, modificar, guardar como en carpeta vacía, reabrir ambas versiones y verificar independencia.
- [ ] Guardar como a carpeta ocupada: rechazo sin sobrescritura; cancelar diálogo sin cambios.
- [ ] Reabrir proyecto con clips/direcciones/tiempos y secuencias editadas; verificar PNG originales intactos.
- [ ] Cerrar con cambios: guardar, descartar, cancelar.

## CI y alcance
Confirmar build Release, pruebas, auditoría, smoke test, portable Windows x64. Migraciones 1–4 a 5 cubiertas por tests. No se afirma aceptación de esta versión hasta contar con resultados. Se mantiene cuadrícula exacta e inmutable mientras tenga referencias. Árbol de recursos, inserción directa desde hoja y auto-scroll durante arrastre siguen pendientes como mejoras de UX. Anclajes, offsets, edición de imágenes e historial corresponden a SL4. SL3 congelado.

## Revisión final 0.2.1.9
0.2.1.8: CI verde confirmado por usuario, 46 correctas, 0 fallos, 1 pendiente (cuadrícula rectangular). Retest 0.2.1.9: guardar proyecto con nombre y carpeta automática; cancelar; sobrescribir proyecto válido solo tras confirmar y comprobar .bak; rechazar carpeta ajena ocupada; renombrar nombre visible y guardar/reabrir sin mover carpeta; verificar etiquetas Fotograma/Sin dirección y números/duraciones de miniaturas; cuadrícula rectangular válida. CI y aceptación pendientes. No cerrar SL2 todavía.
