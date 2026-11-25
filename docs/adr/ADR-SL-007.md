# ADR-SL-007 — Estrategia de pruebas

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Decisión
xUnit para Core e Infrastructure; pruebas UI manuales inicialmente y automatizadas cuando el coste esté justificado. Pruebas pequeñas deterministas; fixtures sintéticos generados en código, más fixtures reales de uso autorizado y sin rutas personales. No copiar assets del juego al repositorio público sin permiso. Comparar dimensiones, píxeles decodificados y metadatos, no bytes PNG cuando cambie el encoder. No incluir archivos temporales en Git.

Matriz mínima: geometría y coordenadas; importación cuadrada/rectangular; alfa recto y preservación de semitransparencia; reproducción con tiempos distintos; historial y frames compartidos; serialización/versiones; guardado fallido; paths peligrosos; diagnósticos y revisiones obsoletas; exportación; adaptador Python con proceso simulado y posteriormente real. Cada defecto corregido añade regresión cuando sea posible.

CI cuando exista repositorio: restore, build Release, test en Windows; pruebas de Core en Linux si el SDK y dependencias lo permiten. Sin Python para suite base; integración real separada con entorno reproducible. Prueba manual de apertura/reproducción/exportación en Windows limpio para cada candidata. Métricas de rendimiento iniciales no contractuales: fixture de 256 frames 96×96, medir memoria y latencia de importación/reproducción; definir presupuesto tras baseline, no afirmar resultados no medidos.

## Criterio de cierre de fase
Compila, pruebas del alcance pasan, flujo manual relevante documentado con versión y entorno, defectos conocidos visibles, changelog actualizado. No exigir cobertura arbitraria; priorizar riesgos de pérdida de datos. No etiquetar una fase como aprobada sin resultados reales.

## Aceptación
Suite base ejecutable con `dotnet test` sin Python y sin recursos del juego; fixtures repetibles; evidencia de fallos de E/S y cancelación. Todavía no existen pruebas C# ejecutadas: este ADR sólo define la estrategia.
