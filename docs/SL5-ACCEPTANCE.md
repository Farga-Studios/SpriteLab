# SL5 — Pruebas de 0.5.0
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
13. Cambiar operación/alcance: se limpia el resultado y el progreso; rango y alfa solo activos cuando corresponden.
14. Codificar resultado grande y cancelar o cerrar durante la codificación; no se incorpora recurso parcial.
15. Guardar un resultado con alfa parcial y RGB invisible; validar RGBA del PNG derivado (el decodificador de entrada puede normalizar RGB invisible).
16. Probar umbrales 0, 1, 255 y progreso en más de 100 celdas; la interfaz sigue respondiendo.
No marcar SL5 aceptada hasta CI y pruebas manuales. Registrar también las pruebas SL4 que estén pendientes.
