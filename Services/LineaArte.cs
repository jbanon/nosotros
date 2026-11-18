using System.Text.Json;
using System.Text.RegularExpressions;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// "Línea del tiempo del arte": cada semana, 5 obras famosas (dominio público, de Seed/linea-arte.json)
/// que hay que colocar en orden cronológico una a una; acertar el autor (entre 4) da un punto extra.
/// Las mismas 5 para los dos, para poder comparar. Una partida por semana (Javi, sin límite).
/// </summary>
public class LineaArte(AppDb db, IWebHostEnvironment env, FileStorage files, IHttpClientFactory http, ILogger<LineaArte> log)
{
    public const string Clave = "linea";
    public const int Cartas = 5;
    /// <summary>Puntos máximos: 4 colocaciones (la primera va sola) + 5 autores.</summary>
    public const int Maximo = Cartas - 1 + Cartas;
    static readonly DateOnly Inicio = new(2026, 11, 16);

    public record Obra(string Titulo, string Autor, int Anio, string Wiki, string Img)
    {
        public string Id => Regex.Replace(Wiki.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }
    public record Carta(string Id, string Titulo, string Autor, int Anio, string[] Autores);

    static List<Obra>? catalogo;
    static readonly object cerrojo = new();

    public List<Obra> Catalogo()
    {
        lock (cerrojo)
        {
            if (catalogo != null) return catalogo;
            var ruta = Path.Combine(env.ContentRootPath, "Seed", "linea-arte.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
            catalogo = doc.RootElement.GetProperty("obras").EnumerateArray()
                .Select(o => new Obra(o.GetProperty("titulo").GetString()!, o.GetProperty("autor").GetString()!, o.GetProperty("anio").GetInt32(),
                                      o.GetProperty("wiki").GetString()!, o.GetProperty("img").GetString()!))
                .OrderBy(o => o.Anio).ToList();
            return catalogo;
        }
    }

    public static DateOnly Lunes(DateOnly d) => Reloj.Lunes(d);

    /// <summary>Las 5 cartas de la semana, en el orden en que salen, con 4 autores a elegir cada una.</summary>
    public List<Carta> Semana(DateOnly lunes)
    {
        var cat = Catalogo();
        var semana = Math.Max(0, (lunes.DayNumber - Inicio.DayNumber) / 7);
        // Baraja fija: cada semana coge las siguientes de la baraja (tardan ~17 semanas en repetirse)
        var baraja = Enumerable.Range(0, cat.Count).OrderBy(i => new Random(2026 * 1000 + i).Next()).ToList();
        var elegidas = new List<Obra>();
        for (int k = 0; elegidas.Count < Cartas && k < cat.Count * 2; k++)
        {
            var o = cat[baraja[(semana * Cartas + k) % cat.Count]];
            // Sin empates raros: años separados al menos 6 y autores distintos
            if (elegidas.Any(e => Math.Abs(e.Anio - o.Anio) < 6 || e.Autor == o.Autor)) continue;
            elegidas.Add(o);
        }
        var rnd = new Random(semana * 7919 + 17);
        var autores = cat.GroupBy(o => o.Autor).Select(g => (autor: g.Key, anio: g.Average(x => x.Anio))).ToList();
        return elegidas.Select(o =>
        {
            // Tres autores de una época parecida (para que no sea regalado), al azar entre los ocho más cercanos
            var otros = autores.Where(a => a.autor != o.Autor).OrderBy(a => Math.Abs(a.anio - o.Anio)).Take(8)
                .OrderBy(_ => rnd.Next()).Take(3).Select(a => a.autor);
            var ops = otros.Append(o.Autor).OrderBy(_ => rnd.Next()).ToArray();
            return new Carta(o.Id, o.Titulo, o.Autor, o.Anio, ops);
        }).ToList();
    }

    public Task<JuegoPartida?> DeSemana(int usuario, DateOnly lunes) =>
        db.JuegoPartidas.Where(p => p.UsuarioId == usuario && p.Juego == Clave && p.Fecha == lunes).OrderByDescending(p => p.Id).FirstOrDefaultAsync();

    /// <summary>Ruta local de la imagen de una obra; la descarga de Wikimedia la primera vez.</summary>
    public async Task<string?> Imagen(string id, CancellationToken ct)
    {
        var o = Catalogo().FirstOrDefault(x => x.Id == id);
        if (o == null) return null;
        var dir = Path.Combine(files.Raiz, "linea");
        var ruta = Path.Combine(dir, id + (o.Img.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg"));
        if (File.Exists(ruta)) return ruta;
        try
        {
            Directory.CreateDirectory(dir);
            var cli = http.CreateClient(); cli.Timeout = TimeSpan.FromSeconds(30);
            cli.DefaultRequestHeaders.UserAgent.ParseAdd("NosotrosApp/1.0 (app familiar privada)");
            var bytes = await cli.GetByteArrayAsync(o.Img, ct);
            await File.WriteAllBytesAsync(ruta + ".tmp", bytes, ct);
            File.Move(ruta + ".tmp", ruta, true);
            return ruta;
        }
        catch (Exception e) { log.LogWarning("Línea del arte: no se pudo bajar {Img}: {Msg}", o.Img, e.Message); return null; }
    }
}
