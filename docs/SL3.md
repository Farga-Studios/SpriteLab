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
