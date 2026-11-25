# ADR-SL-004 — Interfaz y experiencia de usuario

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Decisión
Una ventana con explorador a la izquierda, visor central, inspector derecho y línea de tiempo inferior. Panel de diagnósticos inferior con navegación a frame. Menú Archivo, Edición, Ver, Animación y Herramientas. Paneles redimensionables; etiquetas y atajos descubribles. No construir sistema de docking complejo ni editor de pintura completo inicialmente.

Vista hoja: cuadrícula superpuesta opcional y selección por celda. Vista animación: transparencia ajedrezada, fondo claro/oscuro, zoom entero con nearest neighbor, desplazamiento de cámara, anclaje y límites visibles. Modo comparación de dos frames y onion skin después de reproducción básica. Mostrar resolución nativa y zoom; ninguna vista modifica la imagen por sí sola.

Línea de tiempo: pista/dirección, thumbnails, selección, avance/retroceso, play/pause, loop, velocidad y duración de preview por frame. Usar reloj monotónico y acumulación de tiempo; no depender de FPS de render. Pausar al editar; al reanudar conservar selección. Inspector: rectángulo, anclaje, offset, duración, marcadores y diagnósticos. Mostrar cambios pendientes y confirmación antes de cerrar sin guardar.

Atajos iniciales: Ctrl+O importar/abrir según contexto, Ctrl+S guardar, Ctrl+Z y Ctrl+Y historial, espacio reproducción cuando foco no es campo de texto, flechas navegar. Accesibilidad: foco visible, nombres de controles, no usar sólo color para severidad, soporte de escalado DPI y navegación por teclado. Idioma inicial español; recursos localizables cuando se añadan cadenas, sin traducción múltiple obligatoria.

## Aceptación
Importar una hoja y visualizar una animación sin Python. Seleccionar un aviso y saltar al frame correcto. Cambiar dirección y zoom sin alterar el proyecto. Revisar a 100 % y ampliado, probar DPI 100/150 %, teclado y ausencia de bloqueo durante importación/cancelación.
