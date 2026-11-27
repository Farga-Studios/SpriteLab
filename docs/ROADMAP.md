# Hoja de ruta

| Fase | Versión objetivo | Entrega y puerta de salida |
|---|---|---|
| SL0 | 0.0.x | SDK y paquetes verificados, solución mínima, ventana, pruebas base, ADR aprobados. |
| SL1 | 0.1.0 | Importar PNG, cuadrícula configurable, selección, proyecto local round-trip, fuente protegida. |
| SL2 | 0.2.0 | Clips y direcciones libres, reproducción, zoom, timeline, duración variable. Aceptada en 0.2.1.9 (26/11/2026). |
| SL3 | 0.3.0 | Validación local, diagnósticos navegables y revisión manual. |
| SL4 | 0.4.0 | Anclajes, offsets, reemplazo, reordenar, undo/redo, comparación básica. |
| SL5 | 0.5.0 | Procesamiento nativo C#, operaciones por celda, progreso y recursos derivados. |
| SL6 | 0.6.0 | Exportación PNG/JSON, recuperación, pruebas de integridad y flujo completo. |
| SL7 | 1.0.0 | Estabilización, paquete portable Windows y prueba en entorno limpio. |

## Límites
Cada fase debe cumplir ADR 007; no fechas ni estimaciones cerradas sin medir. El primer flujo útil es SL1–SL2; evitar ampliar alcance hasta probarlo. El piloto artístico de Sprite Pipeline es independiente y no bloquea SL0–SL4. SL5 se implementa de forma nativa, sin dependencia de Python. Integración con motor, shaders, escenarios reales, IA generativa, pintura avanzada, editor de partículas y soporte oficial multiplataforma quedan fuera del MVP; valorar después mediante ADR nuevo, sin modificar el núcleo por adelantado.

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

SL3 0.2.7: export validation reports, JSON v1 and text; next stabilization and acceptance 0.3.0. CI and manual validation pending.

SL3 0.3.0 acceptance candidate: CI 0.2.7.1 green confirmed by user; new CI and manual acceptance pending. No SL4 implementation yet.

SL3 functional acceptance reported by user for 0.3.0.1. 0.3.0.2 usability pass: vector toolbar, scrubber, grid assistant; verify CI and targeted manual checks before SL4.

## SL4 — 0.4.0 acceptance candidate (27/11/2026)
0.3.0.3–0.3.8 implemented in main. Acceptance candidate 0.4.0 includes regression fixes and checklist docs/SL4-ACCEPTANCE.md. Do not mark SL4 accepted until CI and manual checks pass. Replacement refers to an occurrence within the same sheet; external replacement is out of current scope. Export applies offsets/anchors in SL6. Optional Python integration follows SL5, then SL6 export and SL7 stabilization. The proposed detailed 27-point roadmap is a planning aid, not an additional approved scope.

## 0.4.1 — SL5 foundation and custom window chrome
0.4.0 CI green reported by user; manual acceptance remains pending. At user request, integrate main caption and begin optional adapter architecture. See docs/SL5-ADAPTER.md. 0.4.2 Python settings and 0.4.3 actual contract integration remain pending. Retest window drag/double click, maximize/restore, minimize, resize edges, menu access, Alt+F4 and close with dirty project on Windows. No new CI verified yet.

## 0.4.2
0.4.1 accepted by user (todo ok). Python configuration implemented and testable. 0.4.3 remains blocked by unverified Pipeline CLI contract; no repository or code search match found. CI and manual verification pending.

## SL5 — replanteamiento aprobado
0.4.2 CI verde comunicado por el usuario. 0.4.3 sustituye la integración Python por procesamiento nativo C#. 0.4.4 herramientas y vista previa; 0.4.5 progreso/cancelación/errores; 0.4.6 incorporación de resultados como recursos derivados; 0.5.0 aceptación. No alterar fuentes originales. CI nuevo pendiente.

## 0.4.6 — Native processing workflow
0.4.4–0.4.6 implemented together. Tools > Procesamiento de imágenes. Compare source/result, cancel processing, import a separate PNG resource. No Python dependencies. CI/manual acceptance pending; SL5 0.5.0 follows.

## SL7 — Actualizador compartido (requisito aprobado)
Antes de SpriteLab 1.0.0, añadir SL7 0.6.8: actualización integrada reutilizable por la suite (nombre provisional Suite.Updater). Comprobación automática no bloqueante al inicio y manual desde Ayuda, notas de versión, descarga con progreso y cancelación, instalación tras cerrar, reinicio, validación de firma o manifiesto autenticado y SHA-256, recuperación/rollback, protección de proyectos y preferencias. Separar versión instalada, canal y origen de distribución; no incluir credenciales ni asumir repositorios públicos. Validar actualización desde una versión anterior, interrupciones, paquetes corruptos, permisos y funcionamiento sin conexión antes de RC. Distribución concreta y acceso a releases privados pendientes de decisión. No crear releases públicas sin autorización.

## Suite — requisitos de integración futuros
TileLab: editor de tilesets y salas especiales, compatibilidad TMJ inicial y validación de sockets, colisiones y spawn points. DungeonLab: ejecutar el mismo generador versionado que SEALSWORN, con semilla, zona, reglas y catálogo idénticos; pruebas de equivalencia determinista. Herramientas independientes con adaptadores por juego. Reutilizar actualizador y componentes visuales comunes.

## 0.5.0 — SL5 acceptance candidate
Delivery 1: review processing lifecycle, preserve RGBA in PNG encoder, bound progress notifications and add regression tests. CI and manual acceptance pending. Do not mark SL5 accepted or start SL6 without acceptance decision. Distribution repository proposal remains separate; no repository visibility changed.

## 0.5.5 — Delivery 2
SL5 0.5.0 CI green reported by user, manual acceptance still unconfirmed. Proceed with SL6 at user request. Export core, baked offsets, individual PNGs, sheet and versioned animation JSON implemented; CI and manual validation pending. Delivery 3 adds profiles, preview, batch and extended integrity/recovery. See docs/SL6-EXPORT.md.

## Requisitos adicionales para 1.0.0
Entrega 3 (0.5.9): exportación GIF animado por clip/dirección, opción de exportar todas las direcciones, escala entera nearest-neighbor, tiempos individuales y velocidad, fondo transparente o sólido, repetición y vista previa. Documentar cuantización a 256 colores y transparencia binaria; validar límites de memoria y duración, cancelación e integridad. No modificar fuentes.
Entrega 5 (0.6.5): localización reutilizable por la suite (nombre provisional Suite.Localization). Español e inglés completos antes de 1.0.0, recursos de traducción separados del código, claves estables, detección de cultura del sistema, selector persistente, inglés de respaldo, cambio en caliente cuando sea viable, pruebas de paridad de claves y placeholders. Traducir menús, ventanas, tooltips, errores y ayuda; no traducir nombres de contenido del usuario ni claves JSON ni identificadores persistentes. Validar textos largos y ambas culturas en Windows. Reutilizar el mecanismo en EntityLab, TileLab, DungeonLab y futuras herramientas.

## 0.5.9 — Delivery 3
Profiles, preview, batch and native GIF export implemented. CI and manual validation pending. GIF preview shows RGBA source after baked offsets, not actual GIF quantization; all-directions preview plays first direction. Batch selected-frame mode uses selected cell for active sheet and first cell elsewhere. 0.6.0 verifies full export integrity and recovery.

## 0.6.0 — Delivery 4
SL6 acceptance candidate: integrity manifests, prepublication verification, explicit verification and non-destructive abandoned staging recovery. CI 0.5.9 not confirmed in chat; 0.6.0 CI/manual acceptance pending. No public release. SL7 follows after validation.

## SL7 0.6.1 — Delivery 5 first increment
Shared modal caption, global visual styles, initial ES/EN catalog and persisted language choice. Restart required; dynamic messages and remaining catalog entries still need translation. 0.6.5 is not yet complete. Logo unchanged until original can be reviewed. Windows UI/CI pending; previous SL6 acceptance pending.
