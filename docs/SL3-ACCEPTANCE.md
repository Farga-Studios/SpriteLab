# SL3 — Aceptación 0.3.0

Estado: candidata. CI 0.2.7.1 verde según usuario, no sustituye al CI de 0.3.0. Probar con copias de proyectos.

## Validación
- [ ] Hoja sin cuadrícula: validar deshabilitado.
- [ ] Celdas vacías, borde y lado correcto, sprite interior sin aviso.
- [ ] Duplicados exactos; diferencias de color/alfa/posición no son duplicados.
- [ ] Área visible y cuatro márgenes correctos en Fotograma.
- [ ] Desplazamiento grande y cambio de tamaño dentro de una pista; movimiento pequeño permitido.
- [ ] Repetición intencional de celda y direcciones de distinta longitud válidas.
- [ ] Aviso por celda transparente usada en animación y etiquetas similares.
- [ ] Filtros, leyenda, marcas múltiples, zoom, desactivar marcas.
- [ ] Seleccionar aviso navega a celda; cambiar hoja/cuadrícula/clip invalida resultados.
- [ ] Exportar JSON y TXT, también con filtro activo y sin avisos; confirmar totales y referencias.
- [ ] Cancelar exportación; proteger manifiesto; exportación no marca proyecto como modificado.
- [ ] Hoja con más de 1000 hallazgos: recuentos completos y aviso de omisión.
- [ ] Cuadrícula de más de 65.536 celdas: rechazo explícito sin informe ni marcas obsoletas.
- [ ] Probar hoja real de trabajo, tiempo de respuesta y ausencia de bloqueo perceptible prolongado.
- [ ] Ventana normal/maximizada y splitters: lista accesible y controles legibles.

## Regresión SL2
- [ ] Importar dos PNG, crear clip, editar secuencia, reproducir y onion skin.
- [ ] Guardar, cerrar, reabrir; confirmar imágenes originales intactas.

CI: build, tests, audit, smoke, portable. Registrar fallos y observaciones. No iniciar SL4 hasta aceptación explícita.

## Evidencia 0.3.0 (26/11/2026)
CI verde confirmado por usuario. 6/29 correctas (validación de sprites completa), 0 fallos, 23 pendientes. Solicita sugerencias de cuadrícula y recursos de prueba preparados con instrucciones. SL3 permanece abierta. Retest 0.3.0.1.

## Aceptación funcional 0.3.0.1
Usuario comunica todas las pruebas correctas. Solicita revisión visual del asistente, botones con iconos/tooltips y desplazamiento temporal. SL3 funcionalmente aceptada; 0.3.0.2 revisión de usabilidad antes de SL4. CI nuevo y retest específico pendientes.
