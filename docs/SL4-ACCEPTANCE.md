# SpriteLab 0.4.0 — Aceptación SL4
Estado: candidata, no aceptada. Usar un proyecto nuevo y los PNG sintéticos de TestAssets/SL3. No usar proyectos de producción.

## Preparación
1. Actualizar main y descargar el artefacto 0.4.0 de Actions cuando esté verde (o compilar Release).
2. Importar 01_correcto.png, 05_desalineados.png, 07_animaciones.png y 08_rectangular.png.
3. Cuadrícula 48×48 en las tres primeras; 48×64 en 08. Guardar en una carpeta nueva. Conservar copia o SHA256 de sources.

## Checklist
| ID | Acción | Resultado esperado |
|---|---|---|
| A01 | Abrir asistente 09_deteccion_automatica.png | Paleta consistente, zoom y aplicar/cancelar correctos |
| A02 | Abrir Vista, seleccionar celda 2 | Guías iniciales coherentes con casillas; inspector indica 0,0 |
| A03 | Aplicar X=5 Y=-4 | Sprite a derecha/arriba; inspector refleja cambio |
| A04 | Arrastrar con botón izquierdo en modo mover | Coordenadas enteras, una operación de historial |
| A05 | Arrastrar botón central | Solo pan, no cambio de offset |
| A06 | Restablecer | Posición original, edición eliminada |
| A07 | Escribir X inválido o fuera de límites | Sin mutación |
| A08 | Alternar centro/base/límites y actualizar | Guías aparecen/desaparecen |
| A09 | Guías personalizadas X=12 Y=30; zoom y pan | Guías fijas a celda, no al sprite |
| A10 | Asignar anclaje centro inferior | Cruz naranja en 24,48 |
| A11 | Aplicar desplazamiento | Cruz acompaña al sprite |
| A12 | Quitar anclaje | Offset permanece |
| A13 | Activar snap y arrastrar cerca de guía | Ajuste dentro de 3 píxeles |
| A14 | Desactivar snap | Movimiento libre entero |
| A15 | En 05 alinear centro y base visibles | Poses centradas y base común |
| A16 | En 07 alinear celda transparente | Celda omitida y recuento correcto |
| A17 | Alinear por anclaje | Anclaje asignado llega al centro inferior |
| A18 | Selección 1,3-4 y aplicar X/Y | Solo esas celdas editadas |
| A19 | Incremento relativo X=2 Y=-1 | Diferencias individuales conservadas |
| A20 | Asignar/quitar anclajes a selección | Operación conjunta |
| A21 | Selección inválida 0 o rango invertido | Ninguna mutación |
| A22 | Seleccionar fila y dirección activa | Lista correcta, sin duplicados |
| A23 | Crear clip de 01 y fijar duración 250ms en aparición 2 | Duración individual visible |
| A24 | Reemplazar aparición 2 con celda 4 | Misma duración y longitud |
| A25 | Cancelar reemplazo o elegir misma celda | Sin operación nueva |
| A26 | Deshacer y rehacer reemplazo | Secuencia y duración restauradas |
| A27 | Deshacer y rehacer desplazamiento, anclaje y lote | Cada acción restaurada de forma atómica |
| A28 | Tras deshacer, hacer nueva edición | Rehacer deshabilitado |
| A29 | Reproducir y activar onion al pausar | Posiciones corregidas; edición bloqueada durante reproducción |
| A30 | Guardar, cerrar y reabrir | Offsets/anclajes/secuencias persisten |
| A31 | Comparar fuentes SHA256 antes/después | PNG idénticos |
| A32 | Abrir proyecto anterior sin ediciones | Compatible, offsets cero |
| A33 | Cambiar cuadrícula con anclaje/offset/clip | Bloqueado |
| A34 | Cambiar recurso | Historial y selección múltiple reiniciados |
| A35 | Probar 08 con 48×64 | Límites independientes X/Y |
| A36 | Validar hoja y navegar aviso | Diagnósticos originales siguen operativos |
| A37 | Guardar, editar, deshacer y volver a guardar | Resultado correcto; indicador puede seguir sucio conservadoramente |
| A38 | Cerrar con cambios sin guardar | Confirmación de salida |

## Puerta de salida
CI: pendiente; el conector no devolvió checks ni ejecuciones para el commit anterior (no equivale a éxito). Compilación, tests, smoke y paquete portable deben estar verdes. Manual: 0/38 verificados en esta candidata. Registrar incidencias, corregir y repetir los afectados. No pasar a SL5 hasta aceptación explícita.
