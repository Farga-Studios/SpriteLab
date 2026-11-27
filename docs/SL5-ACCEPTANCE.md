# SL5 — Pruebas de 0.4.6
CI y pruebas manuales pendientes. Usar un proyecto de prueba, 01_correcto.png, 07_animaciones.png y 08_rectangular.png de TestAssets/SL3.
1. Sin cuadrícula, menú procesamiento deshabilitado. Aplicar cuadrícula 48×48.
2. Seleccionar celda 2, Herramientas > Procesamiento de imágenes. Voltear horizontal, celda seleccionada, generar. Comparar ambos visores; solo celda 2 cambia. Zoom funciona en ambos.
3. Voltear vertical, fila; cambiar opción invalida resultado anterior. Generar y comprobar fila.
4. Rango 1,3-4: celda 2 intacta. Rango 0 y rango invertido rechazados.
5. Umbral alfa 128 y valores inválidos; 0 conserva alfa cero y vuelve opaco alfa positivo. Limpiar RGB transparente puede resultar sin cambios según decodificador.
6. Toda hoja; comprobar progreso y cancelar en una hoja grande. No se crea recurso hasta pulsar Añadir.
7. Cerrar sin incorporar: proyecto intacto. Resultado sin cambios: añadir deshabilitado.
8. Incorporar resultado: nuevo recurso con cuadrícula original, ID y nombre independientes. No hereda clips/offsets/anclajes; conservar original.
9. Repetir incorporación: nombre no colisiona. Guardar/reabrir: ambos PNG correctos; comparar SHA256 del original.
10. Rectangular 48×64: volteo dentro de cada celda, no intercambio de celdas.
11. Revisar funcionamiento sin Python; menú de configuración Python ya no existe.
12. Reproducir clip: menú procesamiento deshabilitado. Pausar y volver a abrir.
No marcar SL5 aceptada hasta CI y pruebas manuales.
