# Hoja de ruta

| Fase | Versión objetivo | Entrega y puerta de salida |
|---|---|---|
| SL0 | 0.0.x | SDK y paquetes verificados, solución mínima, ventana, pruebas base, ADR aprobados. |
| SL1 | 0.1.0 | Importar PNG, cuadrícula configurable, selección, proyecto local round-trip, fuente protegida. |
| SL2 | 0.2.0 | Clips y direcciones libres, reproducción, zoom, timeline, duración variable. Aceptada en 0.2.1.9 (26/11/2026). |
| SL3 | 0.3.0 | Validación local, diagnósticos navegables y revisión manual. |
| SL4 | 0.4.0 | Anclajes, offsets, reemplazo, reordenar, undo/redo, comparación básica. |
| SL5 | 0.5.0 | Adaptador opcional Python, pruebas simuladas y con Pipeline 0.3.0. |
| SL6 | 0.6.0 | Exportación PNG/JSON, recuperación, pruebas de integridad y flujo completo. |
| SL7 | 1.0.0 | Estabilización, paquete portable Windows y prueba en entorno limpio. |

## Límites
Cada fase debe cumplir ADR 007; no fechas ni estimaciones cerradas sin medir. El primer flujo útil es SL1–SL2; evitar ampliar alcance hasta probarlo. El piloto artístico de Sprite Pipeline es independiente y no bloquea SL0–SL4. SL5 depende de contrato del pipeline validado. Integración con motor, shaders, escenarios reales, IA generativa, pintura avanzada, editor de partículas y soporte oficial multiplataforma quedan fuera del MVP; valorar después mediante ADR nuevo, sin modificar el núcleo por adelantado.

## Próxima acción
SL2 candidate 0.2.0: verify CI, manual workflow and outstanding architecture items. SL3 work may begin in parallel without marking SL2 accepted. No tocar juego, GraphicsLab ni dashboard.

SL3 initial 0.2.1: local alpha and border diagnostics with cell navigation. SL2 acceptance remains pending.

Freeze additional SL3 work until SL2 acceptance. 0.2.1.1 consolidates SL2 without rolling back existing SL3 code or version numbering.

SL2 acceptance candidate 0.2.1.3: await CI and manual checklist, do not expand SL3 until accepted.

## Estado vigente — 26/11/2026
SL2 aceptada en 0.2.1.9: CI verde comunicado por el usuario, 46/47 comprobaciones correctas en 0.2.1.8 y 9/9 en el retest final, incluida la cuadrícula rectangular. SL3 desbloqueada; revisar la implementación existente de diagnósticos alpha/bordes y definir criterios de aceptación antes de ampliar detectores. Mantener esquema 5 y fuentes originales inmutables. No tocar otros proyectos.

SL3 0.2.2: consolidate read-only empty/edge detectors, exact counts and bounded list. Next: duplicates after CI and targeted manual checks. UI redesign deferred; functional review after SL4 and aesthetic review during SL7 are proposed, not gates for SL3.

SL3 0.2.3: exact in-sheet duplicate diagnostics; next alignment metrics. CI and manual validation pending.

SL3 0.2.4: visible bounds and advisory track alignment/size checks; next clip/direction consistency. CI and manual validation pending.

SL3 0.2.5: clip consistency diagnostics. Next visual filtering/navigation. CI pending.

## SL4 — propuesta de editor de corrección (pendiente de diseño)
Editor no destructivo con desplazamiento por arrastre y X/Y, guías horizontales/verticales configurables, centro y línea de suelo, anclajes, ajuste magnético, onion skin, alineación asistida por centro/base/anclaje, edición múltiple, deshacer/rehacer y navegación desde diagnósticos de SL3. Conservar originales y aplicar correcciones en exportación. Definir modelo y ADR antes de implementar; guías geométricas no equivalen a detección anatómica. Pintura avanzada fuera del MVP. Revisión funcional de UI tras SL4 y estética en SL7.

SL3 0.2.6: visual diagnostic filters and sheet markers. Next validation reports. CI and manual validation pending.
