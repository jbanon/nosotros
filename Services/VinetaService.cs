using System.Globalization;
using System.Text.Json;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>Colección de viñetas: reparto por días, elección de juego, importación de carpetas catalogadas.</summary>
public class VinetaService(AppDb db, ConfigService config, FileStorage files, IWebHostEnvironment env, ILogger<VinetaService> log)
{
    public string CarpetaEntrada => Path.GetFullPath(Path.Combine(env.ContentRootPath, "App_Data", "entrada"));

    public static readonly string[] Taxonomia =
    [
        "amor", "ternura", "deseo", "compromiso", "apoyo", "ánimo", "calma", "gratitud", "concordia", "entendimiento",
        "reconciliación", "perdón", "complicidad", "humor", "recuerdo", "echar de menos", "futuro", "aventura", "familia", "regalo"
    ];

    // ---------- Reparto ----------
    async Task<int> PorSemana() => int.TryParse(await config.Get(ConfigClaves.VinetasPorSemana, "3"), out var n) ? Math.Clamp(n, 0, 7) : 3;
    async Task<int> JuegoDe10() => int.TryParse(await config.Get(ConfigClaves.VinetasJuegoDe10, "5"), out var n) ? Math.Clamp(n, 0, 10) : 5;
    async Task<DateOnly> ConocidasDesde() =>
        DateOnly.TryParseExact(await config.Get(ConfigClaves.VinetasConocidasDesde, "2027-02-01"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : new DateOnly(2027, 2, 1);

    /// <summary>Días de la semana (0=lunes) en que toca viñeta, fijos para cada semana.</summary>
    public static HashSet<int> DiasDeLaSemana(DateOnly lunes, int n)
    {
        var dias = Enumerable.Range(0, 7).ToArray();
        var rnd = new Random(lunes.DayNumber * 7919 + 17);
        for (int i = dias.Length - 1; i > 0; i--) { int j = rnd.Next(i + 1); (dias[i], dias[j]) = (dias[j], dias[i]); }
        return dias.Take(n).ToHashSet();
    }

    public async Task<bool> TocaViñeta(DateOnly fecha)
    {
        var n = await PorSemana();
        if (n == 0) return false;
        var lunes = Reloj.Lunes(fecha);
        return DiasDeLaSemana(lunes, n).Contains(fecha.DayNumber - lunes.DayNumber);
    }

    /// <summary>La viñeta de un día. Si no existe aún y toca, se elige; con <paramref name="persistir"/> se guarda la asignación.</summary>
    public async Task<VinetaDia?> DelDia(DateOnly fecha, bool persistir)
    {
        var existente = await db.VinetaDias.Include(v => v.Vineta).FirstOrDefaultAsync(v => v.Fecha == fecha);
        if (existente != null) return existente;
        if (fecha < GastoService.InicioApp) return null;

        var v = await db.Vinetas.FirstOrDefaultAsync(x => x.Activa && x.FechaFija == fecha);
        if (v == null)
        {
            if (!await TocaViñeta(fecha)) return null;
            v = await Elegir(fecha);
            if (v == null) return null;
        }
        var juego = await ElegirJuego(fecha, v);
        var vd = new VinetaDia { Fecha = fecha, VinetaId = v.Id, Vineta = v, Juego = juego };
        if (persistir)
        {
            v.MostradaEl = fecha;
            db.VinetaDias.Add(vd);
            await db.SaveChangesAsync();
            log.LogInformation("Viñeta {Id} asignada al {Fecha} con juego {Juego}", v.Id, fecha, juego);
        }
        return vd;
    }

    async Task<Vineta?> Elegir(DateOnly fecha)
    {
        var desde = await ConocidasDesde();
        var rnd = new Random(fecha.DayNumber * 104729 + 3);
        var enAdviento = db.Adviento.Where(c => c.VinetaId != null).Select(c => c.VinetaId!.Value);
        var nuevas = await db.Vinetas.Where(x => x.Activa && x.UsoPortada && x.FechaFija == null && x.MostradaEl == null && (!x.Conocida || fecha >= desde) && !enAdviento.Contains(x.Id)).ToListAsync();
        if (nuevas.Count > 0) return nuevas[rnd.Next(nuevas.Count)];
        // Todas vistas: repetir la que haga más tiempo que no sale (nunca en los últimos 60 días)
        var limite = fecha.AddDays(-60);
        var viejas = await db.Vinetas.Where(x => x.Activa && x.UsoPortada && x.FechaFija == null && x.MostradaEl != null && x.MostradaEl < limite).OrderBy(x => x.MostradaEl).Take(5).ToListAsync();
        return viejas.Count > 0 ? viejas[rnd.Next(viejas.Count)] : null;
    }

    public static bool TieneFraseLarga(string? s, int minLetras) => (s ?? "").Count(char.IsLetter) >= minLetras;
    public static int Palabras(string? s) => (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    public async Task<List<JuegoVineta>> JuegosViables(Vineta v)
    {
        var res = new List<JuegoVineta> { JuegoVineta.Rompecabezas, JuegoVineta.Revelar };
        if (TieneFraseLarga(v.FraseAutor, 12) || TieneFraseLarga(v.FraseMia, 12)) res.Add(JuegoVineta.Criptofrase);
        if (Palabras(v.FraseMia) >= 4 || Palabras(v.FraseAutor) >= 4) res.Add(JuegoVineta.OrdenaFrase);
        if (await db.VinetaDias.CountAsync(d => d.Juego == JuegoVineta.Ninguno || d.ResueltaEl != null) >= 6) res.Add(JuegoVineta.Parejas);
        return res;
    }

    async Task<JuegoVineta> ElegirJuego(DateOnly fecha, Vineta v)
    {
        if (!v.UsoJuego) return JuegoVineta.Ninguno;
        var rnd = new Random(fecha.DayNumber * 7 + v.Id * 13 + 1);
        if (rnd.Next(10) >= await JuegoDe10()) return JuegoVineta.Ninguno;
        var viables = await JuegosViables(v);
        if (v.JuegoSugerido != JuegoVineta.Ninguno && viables.Contains(v.JuegoSugerido)) return v.JuegoSugerido;
        return viables[rnd.Next(viables.Count)];
    }

    public Task<int> Corazones() => db.VinetaDias.SumAsync(d => d.Corazones);

    /// <summary>
    /// Asigna las viñetas de los días pasados que quedaron sin abrir (desde la primera asignación hasta hoy),
    /// para que nada se pierda si Raquel no entra unos días: lo directo va al álbum y los juegos quedan por jugar.
    /// </summary>
    public async Task Completar(DateOnly hoy)
    {
        var primera = await db.VinetaDias.MinAsync(d => (DateOnly?)d.Fecha);
        if (primera == null) return;
        var existentes = (await db.VinetaDias.Where(d => d.Fecha <= hoy).Select(d => d.Fecha).ToListAsync()).ToHashSet();
        int n = 0;
        for (var d = primera.Value; d <= hoy && n < 120; d = d.AddDays(1), n++)
            if (!existentes.Contains(d)) await DelDia(d, persistir: true);
    }

    /// <summary>Juegos de días pasados (o de hoy) aún sin resolver, del más reciente al más antiguo.</summary>
    public Task<List<VinetaDia>> Pendientes(DateOnly hoy) =>
        db.VinetaDias.Include(d => d.Vineta).Where(d => d.Fecha <= hoy && d.Juego != JuegoVineta.Ninguno && d.ResueltaEl == null)
          .OrderByDescending(d => d.Fecha).ToListAsync();

    /// <summary>Viñetas que Raquel ya ha visto (sin juego, con el juego resuelto, o regaladas desde el adviento), de la más reciente a la más antigua.</summary>
    public async Task<List<VinetaDia>> Desveladas(DateOnly hoy)
    {
        var dias = await db.VinetaDias.Include(d => d.Vineta).Where(d => d.Fecha <= hoy && (d.Juego == JuegoVineta.Ninguno || d.ResueltaEl != null)).ToListAsync();
        var conDia = dias.Select(d => d.VinetaId).ToHashSet();
        // Regaladas fuera del reparto (p. ej. al resolver una casilla del adviento): tienen MostradaEl pero no VinetaDia
        var sueltas = await db.Vinetas.Where(v => v.MostradaEl != null && v.MostradaEl <= hoy && !v.Dias.Any()).ToListAsync();
        dias.AddRange(sueltas.Where(v => !conDia.Contains(v.Id)).Select(v => new VinetaDia { Fecha = v.MostradaEl!.Value, VinetaId = v.Id, Vineta = v, ResueltaEl = DateTime.UtcNow }));
        return dias.OrderByDescending(d => d.Fecha).ThenByDescending(d => d.Id).ToList();
    }

    /// <summary>Raquel puede cargar la imagen si la viñeta ya ha salido (aunque el juego esté a medias: hace falta para jugar).</summary>
    public async Task<bool> PuedeVer(int vinetaId, DateOnly hoy, bool admin) =>
        admin || await db.VinetaDias.AnyAsync(d => d.VinetaId == vinetaId && d.Fecha <= hoy) || await db.Vinetas.AnyAsync(v => v.Id == vinetaId && v.MostradaEl != null && v.MostradaEl <= hoy);

    // ---------- Desbloqueos por corazones ----------
    public record Logro(string Clave, string Nombre, string Emoji, int Corazones, bool Conseguido, string? Texto);

    public async Task<List<Logro>> Logros()
    {
        var c = await Corazones();
        async Task<int> N(string k, int d) => int.TryParse(await config.Get(k, d.ToString()), out var n) ? n : d;
        int g = await N(ConfigClaves.LogroGrande, 3), v = await N(ConfigClaves.LogroVale, 6), ca = await N(ConfigClaves.LogroCarta, 10);
        return
        [
            new("grande", "El rompecabezas grande", "🧩", g, c >= g, "Un 4×4 con la viñeta que tú elijas del álbum."),
            new("vale", "Un vale sorpresa", "🎟️", v, c >= v, await config.Get(ConfigClaves.LogroValeTexto, "Vale por un desayuno en la cama, con el periódico y sin prisa.")),
            new("carta", "Una carta", "💌", ca, c >= ca, await config.Get(ConfigClaves.LogroCartaTexto, "")),
        ];
    }

    // ---------- Importación de carpetas catalogadas (App_Data/entrada/*/catalogo.json) ----------
    public record Carpeta(string Nombre, string Ruta, int Total, int Pendientes);

    public async Task<List<Carpeta>> CarpetasImportables()
    {
        var res = new List<Carpeta>();
        if (!Directory.Exists(CarpetaEntrada)) return res;
        var importados = (await db.Vinetas.Where(v => v.Origen != null).Select(v => v.Origen!).ToListAsync()).ToHashSet();
        foreach (var dir in Directory.GetDirectories(CarpetaEntrada).OrderBy(d => d))
        {
            var cat = Path.Combine(dir, "catalogo.json");
            if (!File.Exists(cat)) continue;
            try
            {
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(cat));
                var ficheros = doc.RootElement.GetProperty("vinetas").EnumerateArray().Select(v => Path.GetFileName(dir) + "/" + v.GetProperty("fichero").GetString()).ToList();
                res.Add(new(Path.GetFileName(dir), dir, ficheros.Count, ficheros.Count(f => !importados.Contains(f))));
            }
            catch (Exception e) { log.LogWarning("catalogo.json ilegible en {Dir}: {Msg}", dir, e.Message); }
        }
        return res;
    }

    public static JuegoVineta ParseJuego(string? s)
    {
        s = (s ?? "").ToLowerInvariant();
        if (s.Contains("rompecabezas")) return JuegoVineta.Rompecabezas;
        if (s.Contains("pareja")) return JuegoVineta.Parejas;
        if (s.Contains("revelar")) return JuegoVineta.Revelar;
        if (s.Contains("cripto")) return JuegoVineta.Criptofrase;
        if (s.Contains("ordena")) return JuegoVineta.OrdenaFrase;
        return JuegoVineta.Ninguno;
    }

    /// <summary>Importa las viñetas de una carpeta con catalogo.json que aún no estén en la colección. Devuelve cuántas.</summary>
    public async Task<int> Importar(string nombreCarpeta)
    {
        var dir = Path.GetFullPath(Path.Combine(CarpetaEntrada, nombreCarpeta));
        if (!dir.StartsWith(CarpetaEntrada) || !File.Exists(Path.Combine(dir, "catalogo.json"))) return 0;
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "catalogo.json")));
        var raiz = doc.RootElement;
        var autor = raiz.TryGetProperty("autor", out var a) ? a.GetString() : "72 Kilos";
        var libro = raiz.TryGetProperty("libro", out var l) ? l.GetString() : nombreCarpeta;
        var conocida = raiz.TryGetProperty("conocida_por_raquel", out var c) && c.GetBoolean();
        var importados = (await db.Vinetas.Where(v => v.Origen != null).Select(v => v.Origen!).ToListAsync()).ToHashSet();
        int n = 0;
        foreach (var e in raiz.GetProperty("vinetas").EnumerateArray())
        {
            var fichero = e.GetProperty("fichero").GetString() ?? "";
            var origen = nombreCarpeta + "/" + fichero;
            var ruta = Path.Combine(dir, fichero);
            if (importados.Contains(origen) || !File.Exists(ruta)) continue;
            var ext = Path.GetExtension(ruta).ToLowerInvariant();
            var rel = "vinetas/" + Guid.NewGuid().ToString("N") + (ext == "" ? ".png" : ext);
            Directory.CreateDirectory(Path.Combine(files.Raiz, "vinetas"));
            File.Copy(ruta, Path.Combine(files.Raiz, rel));
            string? S(string k) => e.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null;
            string? Lista(string k) => e.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.Array ? string.Join(", ", x.EnumerateArray().Select(i => i.GetString())) : null;
            db.Vinetas.Add(new Vineta
            {
                Path = rel, Autor = autor ?? "72 Kilos", Libro = libro, Origen = origen, Conocida = conocida,
                FraseAutor = string.IsNullOrWhiteSpace(S("frase_autor")) ? null : S("frase_autor"),
                Intenciones = Lista("intencion"), Tono = S("tono"), Momentos = Lista("momentos"), Descripcion = S("descripcion"),
                JuegoSugerido = ParseJuego(S("juego_sugerido")),
                EspacioFrase = e.TryGetProperty("espacio_para_tu_frase", out var ef) && ef.ValueKind == JsonValueKind.True,
            });
            n++;
        }
        await db.SaveChangesAsync();
        log.LogInformation("Importadas {N} viñetas de {Carpeta}", n, nombreCarpeta);
        return n;
    }
}
