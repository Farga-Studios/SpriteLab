# SL6 — Aceptación 0.6.0
Estado: candidato, CI y ejecución manual pendientes. Usar copia de proyecto y TestAssets/SL3.
1. Exportar celda sin corrección; comprobar PNG y JSON.
2. Exportar con +X/-Y; comprobar recorte y transparencia.
3. Exportar hoja rectangular; verificar vecinos intactos.
4. Exportar individuales; nombres y dimensiones.
5. Anclaje + desplazamiento: coordenadas locales correctas y offset_baked.
6. Animación con repeticiones y duraciones distintas: JSON correcto.
7. GIF de una dirección, todas las direcciones, escalas 1/2/3/4/8, velocidad y repetición; abrir en visor externo.
8. GIF con transparencia parcial y fondo sólido; comprobar ausencia de rastros entre frames y cuantización.
9. Perfil guardar, reabrir, recuperar, eliminar; probar archivo corrupto.
10. Vista previa, cambiar parámetros; confirmar resultado final.
11. Lote con varias hojas, incluidas hojas sin clips o cuadrícula: comprobar exclusión según modo.
12. Cancelar lote durante trabajo; comprobar que no se publica carpeta final.
13. Exportar dos veces; comprobar nombres independientes.
14. Verificar exportación intacta desde Archivo; modificar PNG y repetir, debe fallar.
15. Restaurar PNG, eliminar un archivo o añadir otro; debe fallar.
16. Verificar lote completo y subcarpeta individual.
17. Simular carpeta padre sin permisos; error sin modificar origen.
18. Simular cierre inesperado durante exportación grande; reabrir, recuperar carpeta incompleta desde Archivo. Confirmar que archivos parciales se conservan y no se marcan válidos.
19. Comprobar SHA-256 de fuentes antes y después, guardar y reabrir proyecto.
20. Probar sin Python instalado. Repetir pruebas SL4/SL5 pendientes.
No dar SL6 por aceptada hasta CI verde y pruebas manuales. GIF preview no reproduce exactamente cuantización; verificador comprueba hashes, no firma criptográfica. 
