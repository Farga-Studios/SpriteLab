# SL3 — Validación local

## 0.2.2: criterios y alcance
- EMPTY: todos los alfa de la celda son 0; RGB oculto no cuenta.
- EDGE: al menos un píxel con alfa mayor que 0 toca uno o más límites; informar los lados. No demuestra recorte: una pose válida puede tocar el borde.
- Una celda vacía puede ser intencional; no es un error bloqueante.
- Analizar toda la cuadrícula; máximo 1000 avisos navegables, más aviso de omisión cuando corresponda. Recuentos completos.
- Seleccionar un aviso lleva a su celda. Cambiar de hoja o cuadrícula invalida el resultado.
- No modificar PNG, clips, ni esquema 5. Sin dependencia del motor ni Python.

## Aceptación 0.2.2
CI Release, tests, smoke y artefacto portable. Manual: hoja con celda vacía, sprite interior y sprite tocando borde; comprobar recuentos, lados y navegación. Repetir con cuadrícula rectangular y otra hoja. Confirmar que la validación no modifica proyecto. Los detectores de duplicados, alineación, consistencia de clips y filtros son siguientes incrementos.

## 0.2.3 — Duplicados exactos
Comparar celdas no vacías de la misma hoja: igualdad RGBA en coordenadas locales; ignorar RGB si alfa=0. Hash FNV-1a y comparación exacta posterior; referencia a primera celda idéntica. Las repeticiones de una misma celda en una pista son válidas y no se inspeccionan aquí. Una celda puede tener EDGE y DUPLICATE; por ello los recuentos de categorías pueden solaparse. CleanCells cuenta únicamente celdas sin hallazgos. El límite de 1000 aplica a hallazgos, no al análisis. Manual: dos sprites idénticos, uno con color/alfa/posición diferente, celdas vacías y una repetición intencional de pista. No se eliminan ni fusionan celdas. Sin detección aproximada ni entre recursos por ahora. CI de 0.2.2 y 0.2.3 pendiente de confirmación.

## 0.2.4 — Dimensiones y alineación
Área visible: rectángulo mínimo de alfa > 0, coordenadas locales, márgenes. Disponible en Fotograma tras validar. Comparar únicamente ocurrencias consecutivas no vacías de cada pista de la hoja; no comparar entre direcciones, saltar repeticiones y huecos, no evaluar cierre de bucle. Umbrales iniciales heurísticos: centro horizontal/base vertical >= max(2 px, 20% del eje de celda); ancho/alto >= max(2 px, 30%). Avisos ALIGNMENT y SIZE, no errores: ataques, saltos y poses pueden cambiar naturalmente. No es detección de anclaje anatómico. Un límite compartido de 1000 avisos y recuentos completos; resultados invalidados tras cambios de proyecto. Manual: validar poses estables y desplazadas, dimensiones visibles y márgenes; cambiar secuencia y confirmar invalidación. CI pendiente.

## 0.2.5 — Clips y direcciones
Avisos de ocurrencias transparentes agrupadas por pista, etiquetas similares ignorando mayúsculas y espacios exteriores, pista none junto a direcciones explícitas y comprobaciones defensivas de referencias/tiempos. Las etiquetas libres, distintas longitudes y FPS y repeticiones intencionadas siguen permitidas. Se limita al recurso activo. La validación estructural del guardado sigue siendo independiente. CI y revisión manual pendientes.

## 0.2.6 — Diagnósticos visuales
Filtros por categoría, leyenda y marcas opcionales sobre hoja, independientes de la cuadrícula. Varias categorías de una celda se representan como segmentos de color en su borde superior. Lista y marcas reflejan el filtro; resumen global y aviso LIMIT permanecen. Las marcas solo cubren los hallazgos retenidos (máximo 1000), no los omitidos. Selección de aviso mantiene navegación existente. Invalidar resultados y marcas al cambiar hoja, cuadrícula o proyecto. Manual: filtrar cada categoría, alternar marcas, zoom, selección, cambio de recurso y combinación de categorías. CI pendiente.

## 0.2.7 — Informes
Exportar informe JSON (contrato versión 1) o texto UTF-8 desde el panel de diagnósticos. Instantánea de hoja activa al validar: versión, fecha UTC, nombres de proyecto/recurso, dimensiones, cuadrícula, totales por categoría, avisos retenidos y omitidos; referencias de celdas desde 1, sin rutas absolutas ni PNG. No depende del filtro visual. No incluye los detalles omitidos por límite; sí recuentos completos. Exportar incluso sin avisos, invalidar al cambiar proyecto/hoja/cuadrícula/secuencia. Selector de archivo con confirmación de sobrescritura y protección del nombre del manifiesto. Manual: ambos formatos, filtro activo, cancelación, informe limpio, invalidación, intento de sobrescribir manifiesto. CI pendiente.

## 0.2.7.1 — Corrección de compilación
CI #36 (0.2.6) falló en MainWindow.axaml.cs línea 989 por comillas escapadas dentro de una expresión interpolada (CS1056 y errores sintácticos derivados). Extraer el recuento a una variable local. Aplicar sobre 0.2.7 sin revertir informes. CI nuevo pendiente; no avanzar a 0.3.0 hasta verificar.
