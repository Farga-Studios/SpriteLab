# SpriteLab — Manual de usuario
## Instalación
Windows x64: ejecuta el instalador y elige si deseas un acceso directo. Alternativamente extrae el ZIP portable completo en una carpeta nueva. No necesita Python ni el SDK .NET. Guarda los proyectos fuera de la carpeta de la aplicación. Para desinstalar usa Aplicaciones instaladas de Windows; conserva tus carpetas de proyectos.
## Primer proyecto
1. Archivo > Importar PNG (Ctrl+I). Selecciona una hoja de sprites.
2. En Hoja indica ancho y alto de celda en píxeles; Aplicar cuadrícula. La imagen debe dividirse exactamente. El asistente propone tamaños, no detecta anatomía.
3. Selecciona una celda en el visor. Usa zoom, ajuste entero y rueda para inspeccionar.
4. Archivo > Guardar proyecto (Ctrl+S); elige nombre y carpeta contenedora. SpriteLab mantiene copias de las fuentes y un manifiesto. Abrir proyecto (Ctrl+O) recupera la carpeta del proyecto.
## Animaciones
Selecciona una celda de una fila y Animación > Crear clip de la fila. Pon nombre al clip. Asigna filas a direcciones libres (front, back, left, right, etc.). Selecciona dirección y reproduce con Espacio. Flechas recorren fotogramas; Bucle controla repetición. FPS fija la duración base; Duración (ms) cambia una aparición individual. Deja vacío para volver al FPS. Editar secuencia acepta celdas numeradas desde 1, repetidas si se desea. Añade, sustituye, duplica, elimina o reordena apariciones desde la línea de tiempo.
## Correcciones
En Vista puedes mover el sprite sin cambiar el PNG, introducir desplazamientos X/Y, activar snap, anclajes y guías. X positivo derecha, Y positivo abajo. Los anclajes se definen en coordenadas locales de la celda. Alineación automática compara área visible o ancla al centro inferior. Edición múltiple admite rangos como 1,3-5,8. Deshacer/Rehacer permite revisar cambios. Onion skin muestra poses vecinas. Guarda el proyecto para conservar metadatos.
## Diagnósticos
Herramientas > Validar hoja (F8). Comprueba celdas transparentes, bordes, duplicados exactos, tamaño/alineación y secuencias. Selecciona aviso para navegar a su celda. No todos los avisos son errores; revisar manualmente. Exporta informe TXT o JSON.
## Procesamiento
Herramientas > Procesamiento de imágenes. Elige limpiar RGB transparente, umbral alfa, voltear horizontal/vertical. Selecciona celda, fila, hoja o rango. Genera vista previa y compara. Añadir como recurso nuevo crea otra hoja, sin modificar la original ni copiar automáticamente animaciones.
## Exportación
Archivo > Exportar sprites y animaciones (Ctrl+E). Elige celda, individuales, hoja, animaciones o GIF. Guarda perfiles con nombre. Vista previa muestra correcciones; el GIF final cuantiza colores y alfa. GIF: dirección, escala entera, velocidad, fondo, repeticiones. Lote exporta hojas compatibles. Selecciona carpeta padre: se crea una nueva carpeta sin sobrescritura. Los desplazamientos se aplican una sola vez, los píxeles visibles que salen de la celda se recortan. JSON incluye coordenadas, anclajes corregidos, secuencias y tiempos. No volver a aplicar offset_baked en el juego.
## Integridad y recuperación
Archivo > Verificar exportación comprueba integrity.json. Si se interrumpe una exportación, Archivo > Recuperar exportaciones incompletas aparta las carpetas temporales reconocidas sin borrarlas. No sustituye una copia de seguridad. Conserva copias externas de tus proyectos.
## Preferencias y ayuda
Edición > Preferencias permite español, inglés o idioma del sistema (reinicia para aplicar). Ayuda > Atajos de teclado muestra acciones. Consulta Acerca de para versión instalada. Las ventanas propias comparten barra y cierre; Esc cierra diálogos, Tab navega.
## Problemas frecuentes
- Cuadrícula inválida: comprueba dimensiones y divisibilidad.
- No se puede cambiar la cuadrícula: quita clips asociados y restablece correcciones.
- Exportación bloqueada: verifica cuadrícula, clips, permisos y espacio libre.
- GIF pierde colores: el formato limita paleta y transparencia; usa PNG para fidelidad.
- Proyecto no abre: conserva el original y consulta el mensaje de error; no borres el manifiesto.
- Si informas de un fallo, incluye versión, pasos, resolución de pantalla y ejemplo reproducible sin datos privados.
