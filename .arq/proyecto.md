# Nosotros — información general

## Qué es
App privada, solo para móvil, para Javier (`jbanon`, javierbanon@gmail.com) y su pareja Raquel.
Es un regalo personal: los textos, el tono y el diseño importan tanto como el código.
Módulos: página "Para ti" con mensaje diario y declaración; menú semanal con lista de la compra
por zonas; gastos del mes con histórico y tickets; Ideas para automatizar; Películas que ver;
Lecturas que Javier le deja a Raquel; Calendario de adviento (diciembre); agente `claude -p` para
fichas de películas y recetas; Viñetas (ilustraciones con frase de Javi, reparto semanal, juegos y álbum);
"Disfruta la vista" (una obra de arte por semana con recorrido de detalles); Admin.

Fechas clave: juntos desde 20/05/2024; cumpleaños de Raquel 03/09/1980.

## Stack
- ASP.NET Core Razor Pages, .NET 10, EF Core + PostgreSQL 16 (Npgsql; cadena `ConnectionStrings:Love` fuera del
  repo: `/etc/love.env` en producción, `appsettings.Local.json` en local). Hasta 2026-11-13 fue SQLite.
  Sin frameworks CSS/JS: `wwwroot/css/app.css` y `wwwroot/js/app.js`, versionados por hash
  (`asp-append-version`).
- Auth por cookie con PIN. Usuarios `Javi` (rol Admin) y `Raquel`. `User.EsAdmin()` en
  `Services/PinAuth.cs`. La carpeta `/Admin` exige el rol Admin; todo lo demás lo ven los dos.
- Migraciones EF en `Data/Migrations` (Postgres). `Data/MigracionesSqlite` + `AppDbSqlite` son de la base antigua:
  solo para importar un love.db y para la copia de seguridad. **Cada cambio de modelo lleva DOS migraciones**
  (si no, la copia de seguridad falla): `--context AppDb -o Data/Migrations` y
  `NombreSqlite --context AppDbSqlite -o Data/MigracionesSqlite --namespace Love.Data.MigracionesSqlite`. Al arrancar: `MigrateAsync` + `SeedService.Ejecutar()`
  (idempotente: ahí van los arreglos de datos que deben aplicarse una vez en producción,
  guardados con una clave en la tabla Config).
- Tabla `Config` clave/valor (`ConfigClaves` en `Models/Entidades.cs`): objetivo de compra,
  importes fijos, firma, nombre, raciones de la lista (12), declaración, etc.

## Mapa rápido
- `Models/Entidades.cs` — todas las entidades. `Data/AppDb.cs` — DbContext.
- `Services/` — lógica: `ListaCompraService` (lista por zonas, escalado a raciones),
  `GastoService` (mes, histórico), `SugerenciaService` (recetas según historial),
  `RecurrenteService` (productos de ticket recurrentes), `LecturaFormato` (texto → HTML),
  `SeedService`, `ConfigService`, `MensajeService`, `PinAuth`, `AdvientoService` (apertura por
  fecha, cifrado de criptofrases, rejilla de crucigramas, comprobaciones y pistas), `AgenteService`
  (BackgroundService que lanza `claude -p`; prompts, esquemas JSON, póster de Wikipedia, crear receta),
  `VinetaService` (reparto semanal con semilla, juegos viables, importación de catálogos), `ArteService`
  (calendario de domingos, imágenes en dos tamaños con ImageSharp, importación de `Seed/arte.json`).
- `Pages/Amor` (página de Raquel), `Pages/Menu` (Semana, Lista, Recetas; `Recetas/Agente` pide recetas
  al agente), `Pages/Peliculas` (Index, Ficha, Poster), `Pages/Vinetas` (álbum, Ver, Imagen, Hoy=juego), `Pages/Admin/Vinetas`, `Pages/Arte` (Index, Ver=visor,
  Imagen) y `Pages/Admin/Arte` (calendario, editor de paradas; JS en `wwwroot/js/arte.js` y `arte-visor.js`), `Pages/Gastos`
  (Index, Editar, Historico, Recurrentes), `Pages/Ideas`, `Pages/Peliculas`, `Pages/Lecturas`,
  `Pages/Admin` (Index, Config, Mensajes, Fechas, Lecturas, Adviento), `Pages/Adviento` (Index,
  Dia con handlers JSON, Foto; JS en `wwwroot/js/adviento.js`), `Pages/Login`.
- `Pages/Shared/_Layout.cshtml` — barra inferior (distinta para Raquel y para el admin).
- `Seed/` — 366 mensajes, recetas iniciales y `adviento.json` (24 casillas de 2026). `deploy/` — service, nginx y `publicar.sh`.

## Arrancar y probar en local
```bash
export PATH=$PATH:~/.dotnet:~/.dotnet/tools DOTNET_ROOT=~/.dotnet   # .NET está en ~/.dotnet
dotnet build && dotnet run        # http://localhost:5100
dotnet ef migrations add Nombre -o Data/Migrations
```
- En segundo plano: `nohup dotnet run --no-build > /tmp/love.log &`. Para parar, en un Bash
  aparte: `pkill -f "[n]et10.0/Love"` (si el comando contiene la cadena buscada, pkill mata la
  propia shell).
- Pruebas con curl y tarros de cookies: login en `/Login` con el PIN (token antiforgery
  raspado de la página), un tarro para Javi y otro para Raquel, y comprobar el HTML de cada
  página. Los handlers son `?handler=X` en la URL del `action` del formulario.
- No hay navegador ni Chromium utilizable aquí; el diseño se valida a ciegas y Javier manda
  capturas.
- PIN locales y cadena de Postgres en `appsettings.Local.json` (ignorado). Postgres local instalado con
  `sudo LOVE_ENV=App_Data/love.env ./deploy/instalar-postgres.sh` (la contraseña está en ese fichero).
  Migraciones: `ConnectionStrings__Love=... dotnet ef migrations add Nombre --context AppDb -o Data/Migrations`.
- Agente: en local usa `~/.local/bin/claude` con la sesión de Javier. Si se arranca la app desde
  Claude Code, el servicio ya limpia `CLAUDECODE`/`CLAUDE_CODE_ENTRYPOINT`. Log: "Agente: tarea N".

## Producción
- Servidor Seppala 88.99.84.194, dominio `https://nosotros.winsoft.es`, código en
  `/srv/nosotros`, app publicada en `/opt/love`, servicio systemd `love.service` como `www-data`,
  PIN y entorno en `/etc/love.env`. HTTPS no es alcanzable desde esta máquina de desarrollo.
- Agente en producción: `/etc/love.env` necesita `Agente__Claude=/home/dev/.local/bin/claude` y
  `CLAUDE_CODE_OAUTH_TOKEN=…` (de `claude setup-token`); ver hilo abierto en `tareas.md`.
- Despliega otro agente de Claude Code que vive en ese servidor. Tras cada push, el mensaje
  para Javier termina con: `cd /srv/nosotros && git pull && ./deploy/publicar.sh`.
- Ese agente también hace commits (carpeta `deploy/`): antes de hacer push, `git fetch` y
  `git rebase origin/main`.

## Git
- Repo `git@github.com:jbanon/nosotros.git`, rama `main`. Javier dice "Ok. Push" cuando quiere
  que suba; no subir sin que lo pida. Mensajes de commit en castellano, descriptivos.
- Todo commit termina con las líneas `Co-Authored-By` y `Claude-Session` que indica el entorno.
