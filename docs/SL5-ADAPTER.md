# SL5 — Adaptador opcional (inicio 0.4.1)
El núcleo y los proyectos no dependen de Python. Infrastructure/Pipeline aísla la detección del intérprete y futura comunicación. PythonProbe comprueba explícitamente un ejecutable seleccionado con --version, sin shell, con tiempo límite y cancelación; no instala paquetes ni invoca el pipeline. La interfaz y preferencias persistentes se incorporarán en 0.4.2.

El contrato exacto de Sprite Pipeline 0.3.0 (CLI, esquema de entrada/salida, códigos y permisos) no está verificado: no se localizó repositorio independiente en ManubeX. No inventar argumentos ni asumir ubicación. Antes de 0.4.3 identificar y validar el contrato real, incluyendo pruebas con un proceso simulado y resultados reales. No cambiar el motor ni otros repositorios. SL4 CI verde comunicado por usuario; pruebas manuales todavía no comunicadas; no declarar SL4 aceptada.

## 0.4.2 — Configuración
Herramientas > Configurar Python. Escribe python o ruta absoluta a python.exe, Comprobar, Guardar. Guardar solo se habilita tras verificación; cambiar texto invalida la comprobación. Preferencia local de usuario en LocalApplicationData/SpriteLab/python.json; no afecta a los proyectos. Probar ruta inexistente, intérprete válido, cancelar y reabrir. No instala Python ni ejecuta el pipeline. 0.4.3 pendiente de contrato real.
