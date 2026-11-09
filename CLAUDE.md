# Nosotros — instrucciones para Claude

Antes de tocar nada, lee `.arq/proyecto.md`, `.arq/decisiones.md` y `.arq/tareas.md`: ahí está
el contexto del proyecto, los criterios cerrados con Javier y en qué se quedó la última sesión.
`DISEÑO.md` es el diseño funcional detallado; consúltalo solo para la parte que toques.

Al terminar cada tarea actualiza `.arq/tareas.md` (una línea en Resueltas, y quita o ajusta el
hilo abierto) y, si Javier ha fijado un criterio nuevo, `.arq/decisiones.md`. Incluye esos
cambios en el mismo commit de la tarea. Sé breve: el detalle vive en el código y en el commit.

Reglas rápidas (detalle en `.arq/`): .NET en `~/.dotnet` (exportar PATH y DOTNET_ROOT); PIN y
secretos nunca en el repo; no hacer push hasta que Javier lo pida; antes de push, fetch + rebase
porque el agente del servidor también hace commits; tras cada push, recordarle el comando de
despliegue `cd /srv/nosotros && git pull && ./deploy/publicar.sh`.
