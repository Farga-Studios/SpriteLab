# Pruebas guiadas de SL3 — 0.3.0.1

Descarga los PNG de esta carpeta (en GitHub, abre cada archivo y pulsa Download raw file), o descarga el ZIP del repositorio. Usa un proyecto de pruebas nuevo; no sobrescribas proyectos reales. Todos los sprites son figuras sintéticas de prueba, no arte del juego.

## A. Comprobación rápida de los detectores ya aprobados
Importa cada PNG, pestaña Hoja, ancho y alto 48, Aplicar cuadrícula, Validar hoja. Numeración desde 1, de izquierda a derecha.

| PNG | Resultado esperado |
|---|---|
| 01_correcto | 4 celdas, 0 vacías, 0 bordes, 0 duplicadas; sin clips, 0 avisos |
| 02_celdas_vacias | vacías 2 y 4 |
| 03_recortes | celda 1 arriba, 2 derecha, 3 abajo, 4 izquierda |
| 04_duplicados | celda 3 idéntica a 1; celda 4 diferente |
| 05_desalineados | crea clip desde fila 1; valida; revisa cambios de centro/base entre 2–3 y 3–4; el sprite desplazado toca también bordes |
| 06_dimensiones | crea clip desde fila 1; valida; cambios de tamaño entre 2–3 y 3–4 |

## B. Animaciones pendientes
1. Importa 07_animaciones.png, cuadrícula 48×48. Selecciona celda 1 y Crear clip de la fila. Nombra el clip Prueba.
2. Valida: aviso TRACK_TRANSPARENT para celda 3. Es normal que también aparezcan duplicados entre filas.
3. Selecciona fila 1, Asignar fila, etiqueta front. Selecciona celda de fila 2 y asigna etiqueta FRONT. Valida: TRACK_LABEL por etiquetas similares.
4. Selecciona pista front, pausa, selecciona miniatura y pulsa Duplicar. Reproduce y valida: repetir una celda no constituye un aviso de animación por sí solo.
5. En una pista, cambia FPS o duración individual y añade/quita un fotograma. Deja la otra pista con distinta longitud. Valida: no se exige igualdad de longitudes ni tiempos.

## C. Interfaz
Con 07 y los clips anteriores, filtra Todos, Vacías, Bordes, Duplicados, Tamaño y alineación, Animaciones. Prueba también 03 y 05 para ver sus categorías. Las marcas corresponden al filtro; alterna checkbox, cambia zoom y selecciona un aviso. Comprueba que lleva a su celda. Cambia de hoja o modifica un clip: se limpian los avisos y se deshabilita exportar. Amplía/reduce panel con separador y comprueba legibilidad.

## D. Informes
Valida 07; selecciona filtro Animaciones y exporta JSON y TXT a una carpeta de pruebas. Abre ambos: totales globales, fecha, dimensiones, referencias desde 1; no solo avisos filtrados. Importa 01 sin clips y exporta: 0 avisos. Cancela un diálogo de exportación y confirma que no se crea archivo. Intenta guardar con nombre project.spritelab.json en una carpeta de pruebas: debe rechazarlo. Exportar no marca el proyecto como modificado.

## E. Estabilidad
Importa 01 y 07, alterna recursos, crea/edita/reproduce clips, activa onion skin, guarda/cierra/reabre. Comprueba que PNG originales no cambian. Valida además una hoja real y observa si se bloquea prolongadamente.
- 10_estres.png: 320×240, celda 8×8, 1200 vacías, 1000 avisos retenidos, 200 omitidos.
- 11_limite_cuadricula.png: 257×256, celda 1×1, 65792 celdas; validar debe rechazar y no dejar marcas ni informe.

## F. Asistente nuevo
Importa 09 sin aplicar cuadrícula. En Hoja pulsa Sugerir cuadrícula. Busca 48×48, 5×3, comprueba vista previa, aplica. Repite con 08: 48×64, 5×3. Cancela una sugerencia y confirma que no cambia la cuadrícula. Con clip asociado no debe cambiarse a otra dimensión. La primera sugerencia es aproximada; si ninguna encaja, usa configuración manual.

Registra CI, resultado de cada bloque y cualquier discrepancia. Los seis checks de validación de 0.3.0 ya están aprobados; retest A es opcional salvo regresiones.
