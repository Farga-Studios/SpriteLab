# ADR-SL-001 — Arquitectura y tecnología

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Contexto
SpriteLab es una herramienta externa y genérica para sprites 2D. SEALSWORN es un caso de uso, no una dependencia. El propietario del proyecto y ChatGPT revisan las decisiones; no se exige auditoría de otros agentes.

## Decisión
C# y .NET 10 LTS como propuesta inicial para un proyecto nuevo (sustituye la propuesta previa de .NET 8); Avalonia UI y MVVM para escritorio, Windows primero. Usar un SDK fijado mediante global.json y versiones NuGet fijadas tras una prueba de compilación. No fijar versiones sin verificar compatibilidad. SkiaSharp se evaluará para lectura y tratamiento de PNG; no acoplar el modelo a tipos de Skia o Avalonia. No introducir un contenedor DI, base de datos, bus de eventos o sistema de plugins hasta justificarlo.

Tres proyectos productivos inicialmente, con solución .sln y pruebas separadas:
- SpriteLab.Core: modelo, operaciones de edición, historial, reglas de validación; sin UI ni E/S directa. Expone contratos de repositorio, decodificación y ejecución externa cuando sean necesarios.
- SpriteLab.Infrastructure: PNG, JSON, archivos, adaptador Python; referencia Core.
- SpriteLab.App: vistas, ViewModels, composición de dependencias; referencia Core e Infrastructure. Los ViewModels invocan operaciones, no manipulan buffers directamente.

El Core es el único propietario del estado editable; vistas y previsualizadores reciben snapshots. Los trabajos costosos se ejecutan fuera del hilo UI, con cancelación, progreso cuando sea posible y resultados versionados: un resultado obsoleto no modifica una revisión nueva. No introducir paralelismo de escritura sobre la sesión.

## Alternativas
WPF: viable en Windows pero limita portabilidad. MonoGame: no aporta valor para paneles de escritorio. Cuatro capas productivas desde el inicio: mayor ceremonia; separar Application en el futuro si Core crece y lo justifica.

## Consecuencias
Sin referencias a Tower.Engine, Tower.GameCore, Tower.Graphics, GraphicsLab, MonoGame ni repositorios del juego. Python es opcional y externo. La compatibilidad Linux/macOS es objetivo futuro, no una garantía actual. El nombre interno se propone como SpriteLab (sin prefijo Tower); nombre comercial pendiente.

## Aceptación
Una solución nueva compila y ejecuta una ventana vacía en Windows sin Python; grafo de referencias conforme; Core se prueba sin UI; abrir/cerrar un PNG no deja recursos bloqueados. Revisar SDK y paquetes antes de aprobar SL0.
