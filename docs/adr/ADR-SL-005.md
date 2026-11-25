# ADR-SL-005 — Integración opcional con Sprite Pipeline

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Contrato inspeccionado
Fuente: paquete Sprite Pipeline 0.3.0, README.md, examples/template.json, scripts/sprite_pipeline.py y references/quality-and-corrections.md. El CLI admite `pack`, `validate`, `preview` con `--manifest` y `--out`; usa schema_version 1, profile.cell cuadrado, profile.target_height, max_extent, ground, alpha y animations con source, columns, rows, directions. Identificadores de personaje/animación restringidos a [a-z0-9_]+. Las direcciones admitidas son front, left, right, back, front-left, front-right, back-left, back-right. No admite etiquetas libres ni celdas rectangulares. `preview_columns` y `preview_ms` son parámetros de previsualización. `validate`/`preview` leen hojas terminadas sin aplicar correcciones; `pack` aplica transiciones, reemplazos y sombras según configuración. `input_mode: packed` evita extracción pero no todas las correcciones de pack.

## Decisión
Adaptador opcional en Infrastructure, interfaz interna IPipelineRunner. Configurar ruta absoluta de intérprete y script de confianza desde preferencias; verificar dependencias mediante prueba controlada. No descargar ni ejecutar scripts adjuntos a proyectos. No usar shell, concatenación de argumentos ni comandos proporcionados por manifiestos; usar ProcessStartInfo.ArgumentList y redirigir stdout/stderr de forma asíncrona. Python 3.10+ según README; no instalar dependencias automáticamente.

Para cada ejecución: snapshot, manifiesto de intercambio separado del proyecto, copiar entradas/reemplazos a staging exclusivo con rutas relativas verificadas, crear salida nueva distinta de carpeta del manifiesto, ejecutar con directorio controlado, capturar salida, ofrecer cancelación y tiempo límite configurable (terminar árbol de procesos cuando sea posible). No usar ubicación del juego. Al finalizar validar rutas, tamaños y esquema antes de importar. No adoptar automáticamente resultados ni borrar fuentes; mostrar diferencias y confirmar. Descartar resultados obsoletos si cambió revisión. Conservar registro local sin secretos y limpiar temporales sólo propios.

Códigos observados: 0 sin errores de validación; 1 errores de validación (pueden existir salidas parciales); 2 ciertos errores de entrada/configuración. Cualquier otro código, ausencia de report.json, JSON inválido o cancelación = ejecución fallida/incompleta, no aprobación. El script no garantiza informe en toda excepción. `report.json` contiene schema_version, character, command, sources, fit_factors, art_approved, engine_tested, animations y passed; cada animación contiene errors/warnings/frames y, cuando procede, alpha_levels/quality_findings. SHA-256 de fuentes/reemplazos; no de todas las salidas. No deducir aprobación artística del campo passed.

El adaptador v1 sólo permite exportar contratos representables: celdas cuadradas, nombres seguros, direcciones reconocidas, pistas uniformes y política alfa válida. Si no es representable, explicar y mantener operativa la edición local. body_anchors en coordenadas de fuente recortada; offsets y replacements son por clave fila,columna; replacements de tamaño celda RGBA; sombras requieren preserve y no input_mode packed. No trasladar anclajes locales sin transformación comprobada. Las opciones del manifiesto que no se entiendan se preservan al importar/exportar el manifiesto original o se rechaza conversión con pérdida explícita.

## Alternativas
Incorporar Python al proceso o portar todos los algoritmos: rechazado inicialmente. La aplicación básica debe funcionar sin Python. El pipeline conserva su propio ciclo de versiones.

## Aceptación
Pruebas con ejecutable simulado: 0/1/2/otro, timeout, cancelación, salidas parciales, report inválido, rutas con espacios, intento de traversal, resultado obsoleto. Prueba real con los ejemplos 0.3.0 en SL5; no se declara compatible hasta ejecutarla. Verificar Windows y versiones soportadas.
