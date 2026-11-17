using System.Globalization;
using System.Text.Json;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class SeedService(AppDb db, IWebHostEnvironment env, IConfiguration cfg, ILogger<SeedService> log, FileStorage files)
{
    string Ruta(string f) => Path.Combine(env.ContentRootPath, "Seed", f);

    public async Task Ejecutar()
    {
        await db.Database.MigrateAsync();
        if (db.Database.IsNpgsql() && File.Exists(files.RutaDb) && await CopiaDatos.Vacia(db))
            await ImportarSqlite();

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

    record SeedAdviento(int Anio, string? Frase, List<AdvientoCasilla> Casillas);

    /// <summary>Lee Seed/adviento.json (null si no existe).</summary>
    async Task<SeedAdviento?> LeerSeedAdviento()
    {
        var ruta = Ruta("adviento.json");
        if (!File.Exists(ruta)) return null;
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(ruta));
        var raiz = doc.RootElement;
        int anio = raiz.GetProperty("anio").GetInt32();
        var casillas = raiz.GetProperty("casillas").EnumerateArray().Select(c => new AdvientoCasilla
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
        }).ToList();
        return new(anio, raiz.TryGetProperty("frase", out var f) ? f.GetString() : null, casillas);
    }

    /// <summary>Crea las casillas del calendario de adviento de Seed/adviento.json si aún no existen para ese año.</summary>
    async Task CargarAdviento()
    {
        var seed = await LeerSeedAdviento();
        if (seed == null || await db.Adviento.AnyAsync(c => c.Anio == seed.Anio)) return;
        if (seed.Frase != null && await db.Configs.FindAsync(ConfigClaves.AdvientoFrase) == null)
            db.Configs.Add(new Config { Clave = ConfigClaves.AdvientoFrase, Valor = seed.Frase });
        db.Adviento.AddRange(seed.Casillas);
        await db.SaveChangesAsync();
        log.LogInformation("Calendario de adviento {Anio} creado", seed.Anio);
    }

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

    const string GlobosTitulo = "Explota los globos";
    const string GlobosIntro = "Hoy toca ser rápida. Explota los globos antes de que se escapen; si se te escapan demasiados, vuelta a empezar.\nAlgunos son corazones dorados y esconden una palabra: duran poquísimo, así que estate atenta. Al final tendrás que escribir la frase que forman.";

    const string ArkanoidTitulo = "Tira los bloques";
    const string ArkanoidIntro = "Hoy toca pulso firme. Mueve la pala con el dedo y no dejes caer la pelota: hay que tirar todos los bloques.\nBriskaBird andará revoloteando por ahí; si le das con la pelota, suelta una palabra. Al final tendrás que escribir la frase que forman.";

    const string ComecocosTitulo = "Vamos a hacer la compra";
    const string ComecocosIntro = "Hoy toca pasillo arriba, pasillo abajo. Lleva el carrito deslizando el dedo y recoge toda la compra sin que te pillen el ticket ni la oferta.\nBriskanguro andará dando saltitos por ahí; si la alcanzas, suelta una palabra. Al final tendrás que escribir la frase que forman.";

    /// <summary>Cambios en casillas del adviento ya creadas (la carga solo ocurre una vez). Cada cambio se aplica
    /// únicamente si la casilla conserva el texto original, para no pisar lo que Javier haya editado en Admin.</summary>
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
        // 2026-11-17: el juego de los globos va en el día 6 (en lugar de su criptofrase); el día 7 sigue siendo foto.
        var g = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 6 && x.Tipo == TipoCasilla.Criptofrase && x.Datos.Contains("Me encanta cuando me acaricias"));
        if (g != null)
        {
            g.Tipo = TipoCasilla.Globos; g.Titulo = GlobosTitulo; g.Intro = GlobosIntro; g.Estado = null; g.Pistas = 0;
            g.Datos = AdvientoService.Serializar(new { frase = AdvientoService.FraseGlobosDefecto });
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día 6 pasa a ser el juego de los globos");
        }
        var f = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 7 && x.Tipo == TipoCasilla.Globos && x.Titulo == GlobosTitulo);
        if (f != null)
        {
            f.Tipo = TipoCasilla.Foto; f.Titulo = "¿Te acuerdas?"; f.Intro = "Mira bien esta foto. ¿Te acuerdas de ese día?"; f.Datos = "{}"; f.Estado = null;
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día 7 vuelve a ser foto");
        }
        // 2026-11-17: murdokus con pistas menos evidentes y criptofrases con más gracia. Cada casilla toma el contenido
        // del seed actual solo si aún contiene un fragmento del texto original (así no se pisa lo editado en Admin).
        // 2026-11-17 (2): días 19 y 23 retocados por Javier; también se actualizan si tienen la versión anterior.
        var originales = new Dictionary<int, string[]>
        {
            [2] = ["Bastó una conversación"], [3] = ["Aroa estaba con un tenedor"], [8] = ["La que llevaba la guitarra"],
            [10] = ["Una casita en el campo y un despachito"], [12] = ["Marcos no suelta la pelota"], [14] = ["Siete en casa, una perra y un montón"],
            [17] = ["Raquel había ido a por el correo"], [19] = ["Cada verano, una semana solo", "tú, yo y la lluvia"], [21] = ["Aroa estaba achuchando"],
            [23] = ["Buena, generosa y amorosa", "que las croquetas, y eso es decir mucho"],
        };
        var seed = await LeerSeedAdviento();
        if (seed == null) return;
        foreach (var (dia, fragmentos) in originales)
        {
            var nueva = seed.Casillas.FirstOrDefault(x => x.Dia == dia);
            var vieja = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == seed.Anio && x.Dia == dia);
            if (nueva == null || vieja == null || vieja.Tipo != nueva.Tipo || !fragmentos.Any(vieja.Datos.Contains)) continue;
            vieja.Titulo = nueva.Titulo; vieja.Intro = nueva.Intro; vieja.Datos = nueva.Datos; vieja.Estado = null; vieja.Pistas = 0;
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día {Dia} actualizado con el contenido nuevo del seed", dia);
        }
        // 2026-11-17: el vale del día 24 es por una serie (la navideña noruega), no por una película. Solo cambia el vale.
        var fin = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 24 && x.Datos.Contains("Vale por una película sin discutir"));
        if (fin != null)
        {
            fin.Datos = fin.Datos.Replace("Vale por una película sin discutir", "Vale por una serie sin discutir");
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: vale del día 24 corregido (serie)");
        }
        // 2026-11-17: el mini Arkanoid va en el día 13 (en lugar de su crucigrama "Junto al fuego"); conserva la letra.
        var a = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 13 && x.Tipo == TipoCasilla.Crucigrama && x.Datos.Contains("CARICIA"));
        if (a != null)
        {
            a.Tipo = TipoCasilla.Arkanoid; a.Titulo = ArkanoidTitulo; a.Intro = ArkanoidIntro; a.Estado = null; a.Pistas = 0;
            a.Datos = AdvientoService.Serializar(new { frase = AdvientoService.FraseArkanoidDefecto });
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día 13 pasa a ser el mini Arkanoid");
        }
        // 2026-11-17: el mini comecocos va en el día 18 (en lugar de su crucigrama "Cómo empezó"); conserva la letra.
        var k = await db.Adviento.FirstOrDefaultAsync(x => x.Anio == 2026 && x.Dia == 18 && x.Tipo == TipoCasilla.Crucigrama && x.Datos.Contains("INTERNET"));
        if (k != null)
        {
            k.Tipo = TipoCasilla.Comecocos; k.Titulo = ComecocosTitulo; k.Intro = ComecocosIntro; k.Estado = null; k.Pistas = 0;
            k.Datos = AdvientoService.Serializar(new { frase = AdvientoService.FraseComecocosDefecto });
            await db.SaveChangesAsync();
            log.LogInformation("Adviento: día 18 pasa a ser el mini comecocos");
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

    /// <summary>
    /// Paso de SQLite a PostgreSQL (noviembre de 2026): copia App_Data/love.db, la pone al día con sus
    /// migraciones, vuelca todas las tablas con los mismos Id y deja el fichero renombrado como copia
    /// (love.db.migrado-AAAAMMDD) para que no se vuelva a importar.
    /// </summary>
    async Task ImportarSqlite()
    {
        var origen = files.RutaDb; var tmp = origen + ".importando";
        log.LogWarning("PostgreSQL vacío y existe {Ruta}: importando la base SQLite", origen);
        foreach (var suf in new[] { "", "-wal", "-shm" })
        {
            if (File.Exists(tmp + suf)) File.Delete(tmp + suf);
            if (File.Exists(origen + suf)) File.Copy(origen + suf, tmp + suf);
        }
        await using (var src = AppDbSqlite.Abrir(tmp))
        {
            await src.Database.MigrateAsync();
            var n = await CopiaDatos.Copiar(src, db);
            log.LogWarning("Importado de SQLite: {Filas} filas ({Detalle})", n.Values.Sum(), string.Join(", ", n.Where(x => x.Value > 0).Select(x => $"{x.Key} {x.Value}")));
        }
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var guardado = $"{origen}.migrado-{DateTime.Now:yyyyMMdd-HHmm}";
        foreach (var suf in new[] { "", "-wal", "-shm" })
        {
            if (File.Exists(origen + suf)) File.Move(origen + suf, guardado + suf);
            if (File.Exists(tmp + suf)) File.Delete(tmp + suf);
        }
    }
}
