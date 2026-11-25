# ADR-SL-002 — Modelo de datos

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Contexto
El pipeline existente usa celdas cuadradas, filas por dirección, columnas por pose y nombres concretos. SpriteLab debe aceptar otros contratos y no confundir los tiempos de previsualización con eventos del motor.

## Decisión
- Project: UUID, nombre, versión del formato, colección de recursos, configuración de validación y referencias opcionales a perfiles.
- SpriteAsset: UUID, nombre visible, imagen fuente inmutable, tamaño de celda (ancho y alto), rectángulos de origen por frame y metadatos.
- Clip: UUID, nombre libre, lista de pistas; una pista tiene etiqueta de dirección libre (incluso «none») y secuencia ordenada de referencias a frames.
- Frame: UUID, rectángulo original en píxeles, contenido derivado opcional, anclaje local, desplazamiento, duración de preview positiva en ms. Repetir referencias es válido; una edición de aparición debe hacer copia antes de mutar un frame compartido.
- Marker opcional: nombre y frame asociado, sin semántica de gameplay. Eliminar un frame debe advertir si tiene marcadores; IDs estables al reordenar.

Coordenadas: origen arriba izquierda, X derecha, Y abajo; rectángulos semiabiertos [x,x+w) × [y,y+h). Índices desde cero. Anclaje local en coordenadas de bordes del frame (0..w, 0..h); posición de dibujo = origen de escena - anclaje + offset. Coordenadas del pipeline para body_anchors son distintas (fuente recortada antes de escala, después del volteo); no convertir automáticamente. Valores enteros inicialmente. Duraciones de preview no se exportan como tiempos reales de combate.

La imagen derivada conserva RGBA recto y los píxeles originales permanecen inmutables. Los snapshots tienen revisión monotónica de sesión. Selección, zoom y reproducción son estado de vista, no datos de negocio. El documento no conoce el motor ni el formato de contenido del juego.

## Alternativas y consecuencias
Modelo fijo de ocho direcciones: rechazado. Permitir pistas de distinta longitud simplifica uso genérico, pero exportar a una cuadrícula rectangular exige un mapeo explícito (ADR 003). No imponer altura de referencia, paleta o anclaje común a todos los clips.

## Aceptación
Importar una cuadrícula rectangular y otra cuadrada; clips de una, cuatro y ocho direcciones y etiquetas libres; reordenar sin cambiar IDs; probar anclaje de un arma extendida, duraciones variables y frames compartidos sin alteración accidental.
