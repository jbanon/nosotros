# .arq — memoria de trabajo del proyecto

Directorio mantenido por Claude (el asistente que programa esta app). Sirve para que, tras un
`/clear` o en una sesión nueva, pueda retomar el hilo sin releer todo el código ni la
conversación anterior.

| Archivo | Qué contiene | Cuándo se toca |
|---|---|---|
| `proyecto.md` | Qué es la app, para quién, stack, cómo arrancar, probar y desplegar, dónde están los secretos, mapa de archivos | Cuando cambia algo estructural |
| `decisiones.md` | Decisiones cerradas con Javier y su porqué. No se reabren sin preguntar | Cuando Javier decide algo nuevo o cambia de criterio |
| `tareas.md` | Histórico de tareas resueltas (fecha, qué, commit) y los hilos abiertos | Al terminar cada tarea |

Reglas para mantenerlo:
- Al empezar una sesión: leer los tres archivos. `DISEÑO.md` (raíz) es el diseño funcional
  detallado; se consulta solo si la tarea toca esa parte.
- Al terminar una tarea: añadir una línea en `tareas.md`, mover o borrar el hilo abierto
  correspondiente, y anotar en `decisiones.md` cualquier criterio nuevo. Breve: una o dos líneas
  por tarea; el detalle está en el commit y en el código.
- Nada de secretos (PIN, tokens, claves). Van en `appsettings.Local.json` o en el entorno del
  servidor, nunca aquí.
- El directorio se versiona con el resto del repo, así el agente del servidor también lo ve.
