# Nosotros (love)

App privada para dos: página de amor con un mensaje para cada día del año, menú semanal con
lista de la compra por zonas, y control de gastos mensual. Pensada para el móvil.

Diseño detallado en [DISEÑO.md](DISEÑO.md).

## Stack

- ASP.NET Core Razor Pages (.NET 10), EF Core + SQLite, sin frameworks CSS ni JS.
- Datos en `App_Data/love.db`; adjuntos en `App_Data/adjuntos/` (ambos fuera del repo).
- Login por PIN (cookie 90 días). Usuarios: **Admin** y **Raquel**. Los PIN iniciales se leen de
  `appsettings.Local.json` (ignorado por git) o de las variables `Pins__Admin` / `Pins__Raquel`.
  Solo se usan al crear la base de datos; después se cambian desde Admin → Configuración.

  ```json
  { "Pins": { "Admin": "123456", "Raquel": "1234" } }
  ```

## Arrancar en local

```bash
export PATH=$PATH:~/.dotnet DOTNET_ROOT=~/.dotnet
dotnet run
# http://localhost:5100
```

La primera vez se crea la base de datos y se cargan `Seed/mensajes.txt` (366 mensajes),
las fechas especiales y `Seed/recetas.json`.

## Estructura

```
Pages/Amor        página de Raquel (tema por época, mensaje del día, contadores)
Pages/Menu        recetas, selección semanal y lista de la compra
Pages/Gastos      gastos del mes, alta con adjunto e histórico por meses (los dos usuarios)
Pages/Ideas       cosas que Raquel quiere que se hagan solas (ideas para automatizar)
Pages/Peliculas   películas que ver juntos, pendientes y vistas
Pages/Gastos      gastos por mes (solo Admin)
Pages/Admin       mensajes, fechas especiales, configuración, copia de seguridad
Services/         TemaService, MensajeService, ListaCompraService, GastoService, PinAuth, SeedService
Seed/             mensajes.txt, recetas.json
deploy/           systemd + nginx + script de publicación
```

## Mensajes

`Seed/mensajes.txt`: una línea por día, formato `MM-DD|F|texto` (frase) o `MM-DD|P|verso / verso / verso` (poema).
Solo se carga si la tabla está vacía; después se editan desde Admin → Frases y poemas.

## Recetas en bloque

Admin → Configuración → Importar recetas (JSON), mismo formato que `Seed/recetas.json`:

```json
{ "nombre": "Lentejas", "raciones": 4, "etiquetas": "comida",
  "ingredientes": [ { "n": "Lentejas", "c": 400, "u": "g", "z": "Otros" },
                    { "n": "Sal", "z": "Otros", "d": true } ],
  "preparacion": "..." }
```

`z` = Fruteria | Charcuteria | Carniceria | Pescaderia | Otros. `d: true` = de despensa (no entra en la lista por defecto).

Las cantidades de cada receta son para sus `raciones`; la lista de la compra las escala siempre a las raciones configuradas en Admin → Configuración (12 por defecto).

## Despliegue

```bash
./deploy/publicar.sh        # publica en /opt/love y arranca el servicio systemd (puerto 5100)
sudo cp deploy/nginx-love.conf /etc/nginx/sites-available/love && sudo ln -s /etc/nginx/sites-available/love /etc/nginx/sites-enabled/
sudo certbot --nginx -d love.winsoft.es
```

Copia de seguridad: Admin → Configuración → Descargar (zip con la base de datos y los adjuntos).
