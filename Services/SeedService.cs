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
            db.Usuarios.Add(new Usuario { Nombre = "Admin", Rol = Rol.Admin, PinHash = BCrypt.Net.BCrypt.HashPassword(pinAdmin) });
            db.Usuarios.Add(new Usuario { Nombre = "Raquel", Rol = Rol.Raquel, PinHash = BCrypt.Net.BCrypt.HashPassword(pinRaquel) });
        }

        if (!await db.Configs.AnyAsync())
            db.Configs.AddRange(
                new Config { Clave = ConfigClaves.ObjetivoCompra, Valor = "350" },
                new Config { Clave = ConfigClaves.ImporteVeva, Valor = "300" },
                new Config { Clave = ConfigClaves.ImporteHipoteca, Valor = "900" },
                new Config { Clave = ConfigClaves.Firma, Valor = "Javier" },
                new Config { Clave = ConfigClaves.NombreElla, Valor = "Raquel" },
                new Config { Clave = ConfigClaves.RacionesLista, Valor = "12" });

        if (!await db.Ideas.AnyAsync())
            db.Ideas.Add(new Idea { Texto = "Pago basket", Nota = "Que el pago del baloncesto se haga solo cada mes, sin tener que acordarnos.", Autor = "Raquel" });

        if (!await db.Peliculas.AnyAsync())
            db.Peliculas.Add(new Pelicula { Titulo = "Full Monty", Nota = "Comedia británica de 1997. Para reírnos juntos en el sofá.", Autor = "Javier" });

        // Gastos iniciales (compras ya hechas antes de estrenar la app). Idempotente: no se duplican.
        var gastosIniciales = new[]
        {
            new Gasto { Fecha = new DateOnly(2026, 11, 8), Tipo = TipoGasto.Compra, Importe = 60, Nota = "Filetes, magro y croquetas" },
        };
        foreach (var g in gastosIniciales)
            if (!await db.Gastos.AnyAsync(x => x.Fecha == g.Fecha && x.Tipo == g.Tipo && x.Importe == g.Importe && x.Nota == g.Nota))
                db.Gastos.Add(g);

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
}
