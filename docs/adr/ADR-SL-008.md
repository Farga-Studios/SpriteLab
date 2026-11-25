# ADR-SL-008 — Versionado y distribución

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Decisión
Versiones 0.x.y hasta estabilizar flujos esenciales; x para incremento funcional, y para corrección compatible; 1.0.0 cuando importación, reproducción, edición, guardado, validación y exportación estén probados y documentados. Etiquetas Git v0.1.0, etc.; changelog con cambios, correcciones, incompatibilidades y limitaciones. Formato de proyecto y contrato del adaptador versionados por separado. Sprite Pipeline 0.3.0 no determina versión de SpriteLab.

Repositorio independiente privado inicialmente, nombre propuesto SpriteLab. No usar organización del juego obligatoriamente. Ramas cortas o trabajo local con commits pequeños; no imponer GitFlow. El propietario aprueba decisiones y entregas; no se requiere revisión de agentes externos. No publicar código, binarios o assets sin su autorización. Licencia y nombre público pendientes antes de cualquier publicación.

Distribución Windows x64 primero: evaluar `dotnet publish -c Release -r win-x64 --self-contained true` sobre proyecto App, ZIP portable con archivos necesarios, README, licencias de dependencias y checksum SHA-256. No prometer single-file ni instalador hasta probar dependencias nativas. Verificar arranque en equipo limpio sin SDK/Python; sin autoactualizador inicial. No descargar Python ni ejecutar migraciones de proyecto automáticamente. Linux/macOS, firma e instalador después si hay demanda.

## Aceptación
Build Release reproducible con SDK fijado, versión visible en About, ZIP probado, checksum comprobable, licencias inventariadas, restauración de proyecto desde copia. No se ha publicado ni creado repositorio en esta etapa.
