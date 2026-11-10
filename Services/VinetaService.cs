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
        var nuevas = await db.Vinetas.Where(x => x.Activa && x.UsoPortada && x.FechaFija == null && x.MostradaEl == null && (!x.Conocida || fecha >= desde)).ToListAsync();
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

    /// <summary>Viñetas que Raquel ya ha visto (sin juego o con el juego resuelto), de la más reciente a la más antigua.</summary>
    public Task<List<VinetaDia>> Desveladas(DateOnly hoy) =>
        db.VinetaDias.Include(d => d.Vineta).Where(d => d.Fecha <= hoy && (d.Juego == JuegoVineta.Ninguno || d.ResueltaEl != null))
          .OrderByDescending(d => d.Fecha).ToListAsync();

    public async Task<bool> PuedeVer(int vinetaId, DateOnly hoy, bool admin) =>
        admin || await db.VinetaDias.AnyAsync(d => d.VinetaId == vinetaId && d.Fecha <= hoy && (d.Juego == JuegoVineta.Ninguno || d.ResueltaEl != null));

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
