using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>Calendario de adviento: casillas por año, apertura por fecha y lógica de los pasatiempos.</summary>
public class AdvientoService(AppDb db)
{
    static readonly JsonSerializerOptions JsonOpc = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Año del calendario vigente: en enero sigue siendo el de diciembre pasado.</summary>
    public static int AnioDe(DateOnly hoy) => hoy.Month == 1 ? hoy.Year - 1 : hoy.Year;

    /// <summary>Entre el 1 de diciembre y el 6 de enero el calendario está "en temporada".</summary>
    public static bool EnTemporada(DateOnly hoy) => hoy.Month == 12 || (hoy.Month == 1 && hoy.Day <= 6);

    public static DateOnly FechaDe(AdvientoCasilla c) => new(c.Anio, 12, c.Dia);
    public static bool Abierta(AdvientoCasilla c, DateOnly hoy) => hoy >= FechaDe(c);

    public Task<List<AdvientoCasilla>> Casillas(int anio) => db.Adviento.Where(c => c.Anio == anio).OrderBy(c => c.Dia).ToListAsync();
    public Task<AdvientoCasilla?> Casilla(int anio, int dia) => db.Adviento.FirstOrDefaultAsync(c => c.Anio == anio && c.Dia == dia);

    /// <summary>Orden fijo pero "desordenado" en que se pintan las casillas, como en un calendario de verdad.</summary>
    public static readonly int[] OrdenVisual = [7, 19, 3, 12, 1, 16, 9, 21, 5, 14, 10, 2, 23, 8, 18, 4, 13, 20, 6, 11, 22, 15, 17, 24];

    public static readonly string[] MensajesCerrada =
    [
        "Paciencia, Amor, que te conozco 😏",
        "Todavía no. Briska vigila esta casilla 🐾",
        "Esta casilla está en la parra, como Naia 🎸",
        "Ni Elsa corriendo como un gamo llega antes 🏃‍♀️",
        "Las clones dicen que aún no toca 💃💃",
        "Aroa la está achuchando para que se abra antes, pero no 🧸",
        "Marcos dice que él la abriría ya. Marcos se equivoca 🏀",
        "Casi, casi… pero no. Vuelve ese día ♥",
    ];

    // ---------- Normalización ----------
    /// <summary>Mayúsculas sin acentos (la Ñ se conserva).</summary>
    public static string Normalizar(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s.ToUpperInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
            {
                // tilde de la Ñ (U+0303 tras N): se conserva como Ñ
                if (ch == '̃' && sb.Length > 0 && sb[^1] == 'N') sb[^1] = 'Ñ';
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    static string SoloLetras(string s) => new(Normalizar(s).Where(char.IsLetter).ToArray());
    public static bool Coincide(string? a, string? b) => SoloLetras(a ?? "") == SoloLetras(b ?? "") && SoloLetras(a ?? "").Length > 0;

    const string Alfabeto = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    // ---------- Criptofrase ----------
    /// <summary>Clave letra→número, fija para cada casilla.</summary>
    static Dictionary<char, int> ClaveCripto(AdvientoCasilla c)
    {
        var nums = Enumerable.Range(1, Alfabeto.Length).ToArray();
        var rnd = new Random(c.Anio * 1000 + c.Dia * 7 + 13);
        for (int i = nums.Length - 1; i > 0; i--) { int j = rnd.Next(i + 1); (nums[i], nums[j]) = (nums[j], nums[i]); }
        return Alfabeto.Select((l, i) => (l, n: nums[i])).ToDictionary(x => x.l, x => x.n);
    }

    static string Frase(AdvientoCasilla c) => Json(c)["frase"]?.GetValue<string>() ?? "";

    /// <summary>Lo que ve el navegador: palabras como listas de celdas {n} (letra) o {s} (signo).</summary>
    public object VistaCripto(AdvientoCasilla c)
    {
        var clave = ClaveCripto(c);
        var frase = Normalizar(Frase(c));
        var palabras = frase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Select(ch =>
            clave.TryGetValue(ch, out var n) ? (object)new { n } : new { s = ch.ToString() }).ToArray()).ToArray();
        var pista = Json(c)["pista"]?.GetValue<string>();
        return new { palabras, pista, letras = frase.Where(char.IsLetter).Distinct().Count() };
    }

    public bool ComprobarCripto(AdvientoCasilla c, Dictionary<string, string> asignacion)
    {
        var clave = ClaveCripto(c);
        var frase = Normalizar(Frase(c));
        foreach (var ch in frase.Where(char.IsLetter).Distinct())
            if (!asignacion.TryGetValue(clave[ch].ToString(), out var l) || Normalizar(l) != ch.ToString()) return false;
        return true;
    }

    /// <summary>Una pista: la clave de la letra más frecuente que aún no está bien.</summary>
    public object? PistaCripto(AdvientoCasilla c, Dictionary<string, string> asignacion)
    {
        var clave = ClaveCripto(c);
        var frase = Normalizar(Frase(c));
        var pendiente = frase.Where(char.IsLetter).GroupBy(ch => ch).OrderByDescending(g => g.Count())
            .Select(g => g.Key).FirstOrDefault(ch => !asignacion.TryGetValue(clave[ch].ToString(), out var l) || Normalizar(l) != ch.ToString());
        return pendiente == default ? null : new { n = clave[pendiente], letra = pendiente.ToString() };
    }

    public string SolucionCripto(AdvientoCasilla c) => Frase(c);

    // ---------- Crucigrama ----------
    record Palabra(string Texto, string Dir, int Fila, int Col, string Pista);

    static List<Palabra> Palabras(AdvientoCasilla c) =>
        (Json(c)["palabras"] as JsonArray ?? new JsonArray()).Select(p => new Palabra(
            Normalizar(p!["palabra"]!.GetValue<string>()), p["dir"]!.GetValue<string>().ToUpperInvariant(),
            p["fila"]!.GetValue<int>(), p["col"]!.GetValue<int>(), p["pista"]?.GetValue<string>() ?? "")).ToList();

    /// <summary>Rejilla de solución: letra por celda.</summary>
    static Dictionary<(int f, int c), char> Rejilla(List<Palabra> ps)
    {
        var g = new Dictionary<(int, int), char>();
        foreach (var p in ps)
            for (int i = 0; i < p.Texto.Length; i++)
                g[p.Dir == "V" ? (p.Fila + i, p.Col) : (p.Fila, p.Col + i)] = p.Texto[i];
        return g;
    }

    public object VistaCruci(AdvientoCasilla c)
    {
        var ps = Palabras(c);
        var g = Rejilla(ps);
        int filas = g.Keys.Max(k => k.f) + 1, cols = g.Keys.Max(k => k.c) + 1;
        // numeración por orden de lectura de los inicios
        var inicios = ps.Select(p => (p.Fila, p.Col)).Distinct().OrderBy(x => x.Fila).ThenBy(x => x.Col).Select((x, i) => (x, n: i + 1)).ToDictionary(x => x.x, x => x.n);
        var celdas = Enumerable.Range(0, filas).Select(f => Enumerable.Range(0, cols).Select(col =>
            g.ContainsKey((f, col)) ? (object)new { num = inicios.TryGetValue((f, col), out var n) ? n : (int?)null } : null!).ToArray()).ToArray();
        var definiciones = ps.Select(p => new { num = inicios[(p.Fila, p.Col)], dir = p.Dir, fila = p.Fila, col = p.Col, largo = p.Texto.Length, pista = p.Pista })
            .OrderBy(d => d.dir).ThenBy(d => d.num).ToArray();
        var letra = Json(c)["letra"];
        return new { filas, cols, celdas, definiciones, sombreada = letra == null ? null : new { fila = letra["fila"]!.GetValue<int>(), col = letra["col"]!.GetValue<int>() } };
    }

    /// <summary>Comprueba el contenido: dict "f,c" → letra.</summary>
    public bool ComprobarCruci(AdvientoCasilla c, Dictionary<string, string> celdas)
    {
        var g = Rejilla(Palabras(c));
        foreach (var (k, v) in g)
            if (!celdas.TryGetValue($"{k.f},{k.c}", out var l) || Normalizar(l) != v.ToString()) return false;
        return true;
    }

    public object? PistaCruci(AdvientoCasilla c, Dictionary<string, string> celdas)
    {
        var g = Rejilla(Palabras(c));
        var rnd = new Random();
        var mal = g.Where(kv => !celdas.TryGetValue($"{kv.Key.f},{kv.Key.c}", out var l) || Normalizar(l) != kv.Value.ToString()).ToList();
        if (mal.Count == 0) return null;
        var e = mal[rnd.Next(mal.Count)];
        return new { fila = e.Key.f, col = e.Key.c, letra = e.Value.ToString() };
    }

    public object SolucionCruci(AdvientoCasilla c) => Rejilla(Palabras(c)).Select(kv => new { fila = kv.Key.f, col = kv.Key.c, letra = kv.Value.ToString() }).ToArray();

    // ---------- Murdoku ----------
    public object VistaMurdoku(AdvientoCasilla c)
    {
        var j = Json(c);
        return new
        {
            caso = j["caso"]?.GetValue<string>(),
            categorias = j["categorias"],
            pistas = j["pistas"],
            remate = j["remate"]?.GetValue<string>(),
        };
    }

    /// <summary>La acusación: índice elegido en cada categoría.</summary>
    public bool ComprobarMurdoku(AdvientoCasilla c, int[] eleccion)
    {
        var sol = (Json(c)["solucion"] as JsonArray)?.Select(x => x!.GetValue<int>()).ToArray() ?? [];
        return sol.Length == eleccion.Length && sol.Zip(eleccion).All(x => x.First == x.Second);
    }

    public object? PistaMurdoku(AdvientoCasilla c, int[] eleccion)
    {
        var j = Json(c);
        var sol = (j["solucion"] as JsonArray)?.Select(x => x!.GetValue<int>()).ToArray() ?? [];
        var cats = j["categorias"] as JsonArray ?? new JsonArray();
        for (int i = 0; i < sol.Length; i++)
            if (i >= eleccion.Length || eleccion[i] != sol[i])
                return new { categoria = i, indice = sol[i], texto = $"{cats[i]!["nombre"]}: {cats[i]!["items"]![sol[i]]}" };
        return null;
    }

    public string ExplicacionMurdoku(AdvientoCasilla c) => Json(c)["explicacion"]?.GetValue<string>() ?? "";

    // ---------- Foto ----------
    public bool ComprobarFoto(AdvientoCasilla c, string respuesta) => Coincide(c.Respuesta, respuesta);
    public object? PistaFoto(AdvientoCasilla c)
    {
        var r = SoloLetras(c.Respuesta ?? "");
        return r.Length == 0 ? null : new { texto = $"Empieza por {r[0]} y tiene {r.Length} letras" };
    }

    // ---------- Frase final ----------
    /// <summary>Palabras de la frase final con sus letras: las ganadas en su sitio y huecos en las demás.</summary>
    public static List<List<(string? letra, int dia)>> FraseFinal(string frase, List<AdvientoCasilla> casillas)
    {
        var conLetra = casillas.Where(c => c.DaLetra).OrderBy(c => c.Dia).ToList();
        var res = new List<List<(string?, int)>>();
        int i = 0;
        foreach (var palabra in frase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = new List<(string?, int)>();
            foreach (var ch in palabra.Where(char.IsLetter))
            {
                var cas = i < conLetra.Count ? conLetra[i] : null;
                p.Add((cas != null && cas.Resuelta ? cas.Letra!.ToUpperInvariant() : null, cas?.Dia ?? 0));
                i++;
            }
            res.Add(p);
        }
        return res;
    }

    public static JsonNode Json(AdvientoCasilla c)
    {
        try { return JsonNode.Parse(string.IsNullOrWhiteSpace(c.Datos) ? "{}" : c.Datos) ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }

    public static string Serializar(object o) => JsonSerializer.Serialize(o, JsonOpc);
}
