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
