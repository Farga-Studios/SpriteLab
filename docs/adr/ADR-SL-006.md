# ADR-SL-006 — Edición, validación y correcciones

**Estado:** Propuesto · **Fecha:** 2026-11-25

## Decisión
Operaciones iniciales: mover anclaje y offset, reordenar, reemplazar/importar frame RGBA, espejar explícitamente, editar duración y extraer/reconstruir cuadrícula. No pintar píxeles ni generar imágenes mediante IA en MVP. Las operaciones se implementan como comandos reversibles, aplicados sobre un documento propietario; deshacer/rehacer por transacción lógica, limpiar redo al editar tras undo. Limitar historial inicialmente a 100 comandos o 128 MiB de datos de historial (lo que se alcance primero), informar cuando se descarta historial antiguo. Guardar no borra historial; reabrir sí. Edición de imagen sobre copia; recortes/desbordamientos se rechazan o requieren exportación distinta, nunca truncado silencioso.

Validación local sin Python: errores estructurales (rectángulos inválidos, dimensiones incompatibles, recursos faltantes, frame vacío no permitido por perfil, alfa incompatible con política); advertencias (contacto de bordes, píxeles ocultos, duplicados exactos, variación de centroide configurable, cierre de ciclo si procede); observaciones artísticas manuales (lateralidad, identidad, alternancia de pies, armas, silueta). Los avisos no son pruebas de defectos. Permitir frames vacíos deliberados mediante perfil, por ejemplo teletransporte. No autoescalar fotogramas por separado ni voltear sin revisar asimetrías. Comparar sólo dentro de una pista y secuencia declarada; excluir pausas intencionadas mediante revisión documentada.

Cada diagnóstico tiene código estable, severidad, recurso/clip/frame ID, mensaje y revisión de origen. Recalcular los afectados tras editar; invalidar aprobación técnica/artística anterior si cambia contenido o contrato relevante. Registrar aceptación de aviso con motivo y revisión. Estado técnico, artístico y prueba en aplicación externa son independientes; la última no se ejecuta en SpriteLab. No bloquear exportación por avisos revisables; sí por errores estructurales. Las correcciones automáticas se muestran antes de aplicar y se pueden deshacer.

## Aceptación
Undo/redo recupera bytes de píxeles y metadatos; reemplazo no cambia fuentes; ataque extendido no provoca desplazamiento al editar anclaje. Detectar frame vacío no permitido y tolerar uno permitido. Aviso de duplicado/centroide no bloquea una animación legítima. Probar cambios durante validación en segundo plano.
