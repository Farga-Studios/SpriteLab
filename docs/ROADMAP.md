# Hoja de ruta

| Fase | Versión objetivo | Entrega y puerta de salida |
|---|---|---|
| SL0 | 0.0.x | SDK y paquetes verificados, solución mínima, ventana, pruebas base, ADR aprobados. |
| SL1 | 0.1.0 | Importar PNG, cuadrícula configurable, selección, proyecto local round-trip, fuente protegida. |
| SL2 | 0.2.0 | Clips y direcciones libres, reproducción, zoom, timeline, duración variable. |
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
