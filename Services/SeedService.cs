using System.Globalization;
using System.Text.Json;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class SeedService(AppDb db, IWebHostEnvironment env, IConfiguration cfg, ILogger<SeedService> log)
{
    string Ruta(string f) => Path.Combine(env.ContentRootPath, "Seed", f);

    public async Task Ejecutar()
    {
        await db.Database.MigrateAsync();

        if (!await db.Usuarios.AnyAsync())
        {
            // Los PIN iniciales no van en el repositorio: appsettings.Local.json (ignorado por git) o variables Pins__Admin / Pins__Raquel.
            var pinAdmin = cfg["Pins:Admin"]; var pinRaquel = cfg["Pins:Raquel"];
            if (string.IsNullOrWhiteSpace(pinAdmin) || string.IsNullOrWhiteSpace(pinRaquel))
                throw new InvalidOperationException("Faltan los PIN iniciales: crea appsettings.Local.json con { \"Pins\": { \"Admin\": \"....\", \"Raquel\": \"....\" } }");
            db.Usuarios.Add(new Usuario { Nombre = "Javi", Rol = Rol.Admin, PinHash = BCrypt.Net.BCrypt.HashPassword(pinAdmin) });
            db.Usuarios.Add(new Usuario { Nombre = "Raquel", Rol = Rol.Raquel, PinHash = BCrypt.Net.BCrypt.HashPassword(pinRaquel) });
        }

        // El usuario administrador se llama Javi (antes "Admin"); renombra en bases ya creadas.
        var adminViejo = await db.Usuarios.FirstOrDefaultAsync(u => u.Rol == Rol.Admin && u.Nombre == "Admin");
        if (adminViejo != null) adminViejo.Nombre = "Javi";

        if (!await db.Configs.AnyAsync())
            db.Configs.AddRange(
                new Config { Clave = ConfigClaves.ObjetivoCompra, Valor = "350" },
                new Config { Clave = ConfigClaves.ImporteVeva, Valor = "300" },
                new Config { Clave = ConfigClaves.ImporteHipoteca, Valor = "900" },
                new Config { Clave = ConfigClaves.Firma, Valor = "Javier" },
                new Config { Clave = ConfigClaves.NombreElla, Valor = "Raquel" },
                new Config { Clave = ConfigClaves.RacionesLista, Valor = "12" },
                new Config { Clave = ConfigClaves.Declaracion, Valor = ConfigClaves.DeclaracionPorDefecto });

        if (!await db.Ideas.AnyAsync())
            db.Ideas.Add(new Idea { Texto = "Pago basket", Nota = "Que el pago del baloncesto se haga solo cada mes, sin tener que acordarnos.", Autor = "Raquel" });

        if (!await db.Peliculas.AnyAsync())
            db.Peliculas.Add(new Pelicula { Titulo = "Full Monty", Nota = "Comedia británica de 1997. Para reírnos juntos en el sofá.", Autor = "Javier" });

        // Los gastos iniciales (compra de 60 € del 8/11/2026) ya no se siembran: Javier los gestiona a mano en la app.

        if (!await db.FechasEspeciales.AnyAsync())
            db.FechasEspeciales.AddRange(
                new FechaEspecial { Mes = 5, Dia = 20, Nombre = "Nuestro aniversario", Emoji = "💍", Tema = "aniversario", AnioOrigen = 2024, TextoContador = "{n} años juntos",
                    Texto = "Feliz aniversario, mi amor.\nUn 20 de mayo dijimos que sí a esto, y desde entonces cada día me lo confirma.\nGracias por elegirme. Yo te elijo siempre." },
                new FechaEspecial { Mes = 9, Dia = 3, Nombre = "Tu cumpleaños", Emoji = "🎂", Tema = "cumple", AnioOrigen = 1980, TextoContador = "Hoy cumples {n}",
                    Texto = "¡Feliz cumpleaños, Raquel!\nHoy el mundo celebra que naciste, y yo celebro que te quedaras conmigo.\nQue este año te traiga todo lo que das. Que es mucho." },
                new FechaEspecial { Mes = 2, Dia = 14, Nombre = "San Valentín", Emoji = "💘", Tema = "valentin", CuentaAtras = false,
                    Texto = "Feliz San Valentín.\nHoy es el día oficial, pero yo lo celebro cada mañana que te veo despertar." },
                new FechaEspecial { Mes = 12, Dia = 24, Nombre = "Nochebuena", Emoji = "🌟", CuentaAtras = false,
                    Texto = "Feliz Nochebuena, mi amor.\nDe todas las mesas del mundo me quedo con la que tiene tu silla al lado de la mía." },
                new FechaEspecial { Mes = 12, Dia = 25, Nombre = "Navidad", Emoji = "🎄", CuentaAtras = false,
                    Texto = "Feliz Navidad.\nMi mejor regalo no lleva lazo ni papel: eres tú, y llegaste hace tiempo." },
                new FechaEspecial { Mes = 12, Dia = 31, Nombre = "Nochevieja", Emoji = "🥂", CuentaAtras = false,
                    Texto = "Doce uvas, doce deseos.\nTodos tienen tu nombre.\nFeliz año nuevo, contigo." },
                new FechaEspecial { Mes = 1, Dia = 1, Nombre = "Año nuevo", Emoji = "✨", CuentaAtras = false,
                    Texto = "Empieza el año y lo primero que pienso es que quiero pasarlo entero contigo.\nFeliz año, mi vida." });

        if (!await db.Mensajes.AnyAsync() && File.Exists(Ruta("mensajes.txt")))
        {
            int n = 0;
            foreach (var linea in await File.ReadAllLinesAsync(Ruta("mensajes.txt")))
            {
                if (linea.Length == 0 || !char.IsDigit(linea[0])) continue;
                var p = linea.Split('|', 3);
                var fecha = p[0].Split('-');
                db.Mensajes.Add(new Mensaje
                {
                    Mes = int.Parse(fecha[0]), Dia = int.Parse(fecha[1]),
                    Tipo = p[1] == "P" ? TipoMensaje.Poema : TipoMensaje.Frase,
                    Texto = p[2].Replace(" / ", "\n").Trim()
                });
                n++;
            }
            log.LogInformation("Cargados {n} mensajes", n);
        }

        await db.SaveChangesAsync();

        if (!await db.Recetas.AnyAsync() && File.Exists(Ruta("recetas.json")))
            await CargarRecetas(await File.ReadAllTextAsync(Ruta("recetas.json")));

        await EscalarRecetas();
        await CargarAdviento();
        await CorregirAdviento();
        await SembrarListas();
        await LimpiarMesesAnteriores();
    }

    /// <summary>Una sola vez: pasa todas las recetas a las raciones de la lista (12) multiplicando sus cantidades.</summary>
    async Task EscalarRecetas()
    {
        if (await db.Configs.AnyAsync(c => c.Clave == ConfigClaves.RecetasEscaladas)) return;
        var cfgRaciones = (await db.Configs.FindAsync(ConfigClaves.RacionesLista))?.Valor;
        var objetivo = int.TryParse(cfgRaciones, out var v) && v > 0 ? v : ListaCompraService.RacionesPorDefecto;
        int n = 0;
        foreach (var r in await db.Recetas.Include(x => x.Ingredientes).ToListAsync())
        {
            if (r.Raciones == objetivo || r.Raciones < 1) continue;
            var f = (decimal)objetivo / r.Raciones;
            foreach (var ri in r.Ingredientes)
                if (ri.Cantidad is decimal c) ri.Cantidad = Math.Round(c * f, 2);
            r.Raciones = objetivo;
            n++;
        }
        db.Configs.Add(new Config { Clave = ConfigClaves.RecetasEscaladas, Valor = "1" });
        await db.SaveChangesAsync();
        if (n > 0) log.LogInformation("Escaladas {n} recetas a {r} raciones", n, objetivo);
    }

    record IngSeed(string n, decimal? c, string? u, string z, bool d);
    record RecetaSeed(string nombre, int raciones, string? etiquetas, string? preparacion, List<IngSeed> ingredientes);

    public async Task<int> CargarRecetas(string json)
    {
        var lista = JsonSerializer.Deserialize<List<RecetaSeed>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var ings = await db.Ingredientes.ToListAsync();
        int n = 0;
        foreach (var r in lista)
        {
            if (await db.Recetas.AnyAsync(x => x.Nombre == r.nombre)) continue;
            var receta = new Receta { Nombre = r.nombre, Raciones = r.raciones, Etiquetas = r.etiquetas, Preparacion = r.preparacion };
            int orden = 0;
            foreach (var i in r.ingredientes)
            {
                var ing = ings.FirstOrDefault(x => string.Equals(x.Nombre, i.n, StringComparison.OrdinalIgnoreCase));
                if (ing == null)
                {
                    ing = new Ingrediente { Nombre = i.n, Zona = Enum.Parse<Zona>(i.z, true), Despensa = i.d };
                    db.Ingredientes.Add(ing);
                    ings.Add(ing);
                }
                receta.Ingredientes.Add(new RecetaIngrediente { Ingrediente = ing, Cantidad = i.c, Unidad = i.u, Orden = orden++ });
            }
            db.Recetas.Add(receta);
            n++;
        }
        await db.SaveChangesAsync();
        return n;
    }

    /// <summary>Crea las casillas del calendario de adviento de Seed/adviento.json si aún no existen para ese año.</summary>
    async Task CargarAdviento()
    {
        var ruta = Ruta("adviento.json");
        if (!File.Exists(ruta)) return;
        using var doc = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(ruta));
        var raiz = doc.RootElement;
        int anio = raiz.GetProperty("anio").GetInt32();
        if (await db.Adviento.AnyAsync(c => c.Anio == anio)) return;
        if (raiz.TryGetProperty("frase", out var frase) && await db.Configs.FindAsync(ConfigClaves.AdvientoFrase) == null)
            db.Configs.Add(new Config { Clave = ConfigClaves.AdvientoFrase, Valor = frase.GetString() ?? "" });
        foreach (var c in raiz.GetProperty("casillas").EnumerateArray())
        {
            db.Adviento.Add(new AdvientoCasilla
            {
                Anio = anio,
                Dia = c.GetProperty("dia").GetInt32(),
                Tipo = Enum.Parse<TipoCasilla>(c.GetProperty("tipo").GetString()!),
                Titulo = c.GetProperty("titulo").GetString() ?? "",
                Intro = c.TryGetProperty("intro", out var i) ? i.GetString() : null,
                Letra = c.TryGetProperty("letra", out var l) ? l.GetString() : null,
                Pregunta = c.TryGetProperty("pregunta", out var p) ? p.GetString() : null,
                Respuesta = c.TryGetProperty("respuesta", out var r) ? r.GetString() : null,
                Datos = c.TryGetProperty("datos", out var d) ? d.GetRawText() : "{}",
            });
        }
        await db.SaveChangesAsync();
        log.LogInformation("Calendario de adviento {Anio} creado", anio);
    }

    /// <summary>Cambios en casillas del adviento ya creadas (la carga solo ocurre una vez). Cada cambio se aplica
    /// únicamente si la casilla conserva el texto original, para no pisar lo que Javier haya editado en Admin.</summary>
    /// <summary>Listas que Javier dejó apuntadas (2026-11-10): Viaje con lo que siempre se olvida y Camping vacía para completar.</summary>
    async Task SembrarListas()
    {
        if (await db.Configs.AnyAsync(c => c.Clave == ConfigClaves.ListasSembradas)) return;
        var viaje = new Lista { Nombre = "Viaje", Emoji = "🧳" };
        int i = 0;
        foreach (var t in new[] { "Especias", "Infusiones", "Adaptador de corriente" }) viaje.Elementos.Add(new ListaElemento { Texto = t, Orden = i++ });
        db.Listas.AddRange(viaje, new Lista { Nombre = "Camping", Emoji = "⛺" });
        db.Configs.Add(new Config { Clave = ConfigClaves.ListasSembradas, Valor = "1" });
        await db.SaveChangesAsync();
    }

    async Task CorregirAdviento()
    {
        // 2026-11-11: en el murdoku del día 3 la sospechosa es Aroa, no Miranda.
        var c = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 3 && x.Tipo == TipoCasilla.Murdoku);
        if (c != null && c.Datos.Contains("\"Marcos\", \"Miranda\", \"Briska\""))
        {
            c.Datos = c.Datos.Replace("Miranda", "Aroa");
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día 3 con Aroa en lugar de Miranda");
        }
    }

    /// <summary>La app empieza en noviembre de 2026: los meses anteriores quedan en blanco (sin fijos automáticos).</summary>
    async Task LimpiarMesesAnteriores()
    {
        var inicio = GastoService.InicioApp;
        var fijos = await db.Gastos.Where(g => g.EsFijo && g.Fecha < inicio).ToListAsync();
        var meses = await db.MesesInicializados.Where(m => m.Anio * 100 + m.Mes < inicio.Year * 100 + inicio.Month).ToListAsync();
        if (fijos.Count == 0 && meses.Count == 0) return;
        db.Gastos.RemoveRange(fijos); db.MesesInicializados.RemoveRange(meses);
        await db.SaveChangesAsync();
        log.LogInformation("Meses anteriores a {Inicio} limpiados: {Fijos} fijos", inicio, fijos.Count);
    }
}
