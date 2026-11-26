# SL2 — Lista de aceptación (0.2.1.3)

Estado: candidata, no aprobada. Registrar versión/commit, Windows, DPI y resultado; usar copias de proyectos. El usuario confirmó CI de 0.2.1.2, no de esta candidata.

## Comprobaciones automáticas
- CI Release, auditoría, tests, smoke test, portable Windows x64.
- Fixtures históricos esquemas 1–4, migración a 5, PNG intacto, backup intacto, referencias y tiempos.
- Rechazo de versiones futuras y referencias inválidas.
- Reloj de duración variable, retrasos, bucle, pausa en última pose.

## Comprobaciones manuales
- [ ] Arrancar portable limpio, importar PNG transparente y cuadrículas rectangular/cuadrada.
- [ ] Crear clips de una, cuatro y ocho pistas con etiquetas libres; cambiar de recurso y pista.
- [ ] Editar secuencia: repetir, excluir, reordenar; comprobar tiempos individuales.
- [ ] Reproducir con/sin bucle; pausar en última pose y reanudar; parar; anterior/siguiente; selección directa.
- [ ] Cambiar FPS y duración, confirmar que cada pista conserva sus tiempos tras guardar/reabrir.
- [ ] Activar Onion Skin anterior/siguiente; variar opacidad; comprobar límites y bucle.
- [ ] Zoom/pan del visor y zoom de hoja; teclado sin interceptar escritura en campos.
- [ ] Guardar y reabrir copia de proyecto antiguo, verificar fuentes intactas y backup.
- [ ] Probar cerrar sin guardar, guardar, cancelar y rechazar cambios de cuadrícula con referencias.
- [ ] Probar ventana mínima, DPI 100/150%, controles accesibles y ausencia de recortes.
- [ ] Anotar defectos y repetir tras corrección.

## Alcance
La fase SL2 edita secuencias de fotogramas originales inmutables, identificados en esquema 5. La interfaz usa una proyección de celdas; el Core valida referencias y conserva duración por aparición. Edición de rectángulos arbitrarios, anclajes, offsets, historial y persistencia de derivados corresponden a SL4. El inicio de SL3 ya existe en main, pero queda congelado. No cerrar SL2 hasta tener resultados reales.
