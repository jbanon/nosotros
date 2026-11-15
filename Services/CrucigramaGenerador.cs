using System.Text.Json;

namespace Love.Services;

/// <summary>
/// Crucigrama del día generado con el banco de <c>Seed/crucigrama.json</c> (palabra, pista). La semilla
/// sale de la fecha (y de un número para "otro"), así el mismo día siempre da la misma rejilla.
/// </summary>
public class CrucigramaGenerador(IWebHostEnvironment env)
{
    public const int Max = 11;      // lado máximo, para que quepa en el móvil
    public const int Objetivo = 12; // palabras buscadas

    public record Def(int Num, string Dir, int Fila, int Col, int Largo, string Pista, string Palabra);
    public record Rejilla(int Filas, int Cols, List<Def> Defs);

    List<(string palabra, string pista)>? banco;

    List<(string palabra, string pista)> Banco() => banco ??= JsonSerializer
        .Deserialize<List<string[]>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Seed", "crucigrama.json")))!
        .Select(x => (Criptofrase.SoloLetras(x[0]), x[1])).Where(x => x.Item1.Length >= 3 && x.Item1.Length <= Max).ToList();

    public static int Semilla(DateOnly dia, int n) => dia.DayNumber * 31 + n * 7919;

    readonly System.Collections.Concurrent.ConcurrentDictionary<int, Rejilla> cache = new();

    public Rejilla Generar(int semilla) => cache.GetOrAdd(semilla, Crear);

    /// <summary>Letra correcta por celda "f,c".</summary>
    public Dictionary<string, string> Solucion(int semilla)
    {
        var d = new Dictionary<string, string>();
        foreach (var x in Generar(semilla).Defs)
            for (int i = 0; i < x.Largo; i++) d[x.Dir == "V" ? $"{x.Fila + i},{x.Col}" : $"{x.Fila},{x.Col + i}"] = x.Palabra[i].ToString();
        return d;
    }

    Rejilla Crear(int semilla)
    {
        var rnd = new Random(semilla);
        // Una pista por palabra (si hay varias, al azar)
        var palabras = Banco().GroupBy(x => x.palabra).Select(g => g.ElementAt(rnd.Next(g.Count()))).ToList();
        Rejilla? mejor = null; double puntos = -1;
        for (int intento = 0; intento < 40; intento++)
        {
            var orden = palabras.OrderBy(_ => rnd.Next()).ToList();
            var r = Intento(orden, rnd);
            // más palabras y más cruces, sin pasarse de alargada
            double p = r.Defs.Count * 10 + r.Defs.Sum(d => d.Largo) / (double)(r.Filas * r.Cols) * 20 - Math.Abs(r.Filas - r.Cols);
            if (p > puntos) { puntos = p; mejor = r; }
            if (r.Defs.Count >= Objetivo && intento >= 10) break;
        }
        return mejor!;
    }

    record Pos(string Palabra, string Pista, int F, int C, bool V);

    static Rejilla Intento(List<(string palabra, string pista)> orden, Random rnd)
    {
        var g = new Dictionary<(int, int), (char l, bool h, bool v)>();
        var puestas = new List<Pos>();
        // primera: larga y en horizontal
        var primera = orden.Where(x => x.palabra.Length is >= 6 and <= 9).First();
        Poner(g, puestas, new Pos(primera.palabra, primera.pista, 0, 0, false));
        foreach (var (pal, pista) in orden)
        {
            if (puestas.Count >= Objetivo) break;
            if (puestas.Any(p => p.Palabra == pal)) continue;
            var opciones = new List<(Pos p, int cruces)>();
            foreach (var ((f, c), cel) in g.ToList())
                for (int i = 0; i < pal.Length; i++)
                {
                    if (pal[i] != cel.l) continue;
                    if (!cel.v) Probar(g, new Pos(pal, pista, f - i, c, true), opciones);
                    if (!cel.h) Probar(g, new Pos(pal, pista, f, c - i, false), opciones);
                }
            if (opciones.Count == 0) continue;
            var max = opciones.Max(o => o.cruces);
            var elegidas = opciones.Where(o => o.cruces == max).ToList();
            Poner(g, puestas, elegidas[rnd.Next(elegidas.Count)].p);
        }
        int f0 = g.Keys.Min(k => k.Item1), c0 = g.Keys.Min(k => k.Item2);
        int filas = g.Keys.Max(k => k.Item1) - f0 + 1, cols = g.Keys.Max(k => k.Item2) - c0 + 1;
        var inicios = puestas.Select(p => (p.F - f0, p.C - c0)).Distinct().OrderBy(x => x.Item1).ThenBy(x => x.Item2)
            .Select((x, i) => (x, i + 1)).ToDictionary(x => x.x, x => x.Item2);
        var defs = puestas.Select(p => new Def(inicios[(p.F - f0, p.C - c0)], p.V ? "V" : "H", p.F - f0, p.C - c0, p.Palabra.Length, p.Pista, p.Palabra))
            .OrderBy(d => d.Dir).ThenBy(d => d.Num).ToList();
        return new Rejilla(filas, cols, defs);
    }

    static IEnumerable<(int f, int c)> Celdas(Pos p) => Enumerable.Range(0, p.Palabra.Length).Select(i => p.V ? (p.F + i, p.C) : (p.F, p.C + i));

    static void Poner(Dictionary<(int, int), (char l, bool h, bool v)> g, List<Pos> puestas, Pos p)
    {
        int i = 0;
        foreach (var k in Celdas(p))
        {
            var prev = g.TryGetValue(k, out var x) ? x : (p.Palabra[i], false, false);
            g[k] = (p.Palabra[i], prev.Item2 || !p.V, prev.Item3 || p.V);
            i++;
        }
        puestas.Add(p);
    }

    static void Probar(Dictionary<(int, int), (char l, bool h, bool v)> g, Pos p, List<(Pos, int)> opciones)
    {
        var (df, dc) = p.V ? (1, 0) : (0, 1);
        var antes = (p.F - df, p.C - dc); var despues = (p.F + df * p.Palabra.Length, p.C + dc * p.Palabra.Length);
        if (g.ContainsKey(antes) || g.ContainsKey(despues)) return;
        int cruces = 0, i = 0;
        var celdas = Celdas(p).ToList();
        foreach (var (f, c) in celdas)
        {
            if (g.TryGetValue((f, c), out var cel))
            {
                if (cel.l != p.Palabra[i] || (p.V ? cel.v : cel.h)) return;
                cruces++;
            }
            else
            {
                // sin tocar de lado a otra palabra
                var lados = p.V ? new[] { (f, c - 1), (f, c + 1) } : new[] { (f - 1, c), (f + 1, c) };
                if (lados.Any(g.ContainsKey)) return;
            }
            i++;
        }
        if (cruces == 0 || cruces == p.Palabra.Length) return;
        var todas = g.Keys.Concat(celdas).ToList();
        if (todas.Max(k => k.Item1) - todas.Min(k => k.Item1) >= Max || todas.Max(k => k.Item2) - todas.Min(k => k.Item2) >= Max) return;
        opciones.Add((p, cruces));
    }
}
