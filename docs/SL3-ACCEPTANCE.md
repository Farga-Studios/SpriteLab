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
