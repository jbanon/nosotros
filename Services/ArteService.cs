using System.Text.Json;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Love.Services;

/// <summary>"Disfruta la vista": obras con recorrido de detalles, una por semana (domingo), sin caducar.</summary>
public class ArteService(AppDb db, FileStorage files, IWebHostEnvironment env, ILogger<ArteService> log)
{
    public const int LadoGrande = 4096;
    public const int LadoReducido = 1600;
    /// <summary>Primer domingo de publicación.</summary>
    public static readonly DateOnly Estreno = new(2026, 11, 15);

    public string CarpetaEntrada => Path.GetFullPath(Path.Combine(env.ContentRootPath, "App_Data", "entrada", "arte"));

    public static DateOnly DomingoDe(DateOnly d) => d.AddDays(-(int)d.DayOfWeek);

    /// <summary>
    /// Fecha de publicación efectiva de las obras sueltas (las de un capítulo salen con él): la fijada, o el
    /// siguiente domingo libre siguiendo el orden de la cola. Los domingos de los capítulos cuentan como ocupados.
    /// </summary>
    public async Task<Dictionary<int, DateOnly>> Calendario()
    {
        var obras = await db.Obras.Where(o => o.Activa && o.CapituloId == null).OrderBy(o => o.Orden).ThenBy(o => o.Id).ToListAsync();
        var res = new Dictionary<int, DateOnly>();
        var ocupados = obras.Where(o => o.Publicacion != null).Select(o => DomingoDe(o.Publicacion!.Value))
                            .Concat((await CalendarioCapitulos()).Values).ToHashSet();
        foreach (var o in obras.Where(o => o.Publicacion != null)) res[o.Id] = DomingoDe(o.Publicacion!.Value);
        var d = Estreno;
        foreach (var o in obras.Where(o => o.Publicacion == null))
        {
            while (ocupados.Contains(d)) d = d.AddDays(7);
            res[o.Id] = d; ocupados.Add(d); d = d.AddDays(7);
        }
        return res;
    }

    /// <summary>Domingo de cada capítulo activo con fecha (sin fecha = en preparación).</summary>
    public async Task<Dictionary<int, DateOnly>> CalendarioCapitulos() =>
        (await db.Capitulos.Where(c => c.Activo && c.Publicacion != null).Select(c => new { c.Id, c.Publicacion }).ToListAsync())
            .ToDictionary(c => c.Id, c => DomingoDe(c.Publicacion!.Value));

    /// <summary>Obras sueltas ya publicadas a fecha <paramref name="hoy"/>, de la más reciente a la más antigua.</summary>
    public async Task<List<(Obra obra, DateOnly fecha)>> Publicadas(DateOnly hoy)
    {
        var cal = await Calendario();
        var ids = cal.Where(kv => kv.Value <= hoy).Select(kv => kv.Key).ToList();
        var obras = await db.Obras.Include(o => o.Paradas).Where(o => ids.Contains(o.Id)).ToListAsync();
        return obras.Select(o => (o, cal[o.Id])).OrderByDescending(x => x.Item2).ToList();
    }

    /// <summary>Capítulos ya publicados, del más reciente al más antiguo, con sus obras y paradas.</summary>
    public async Task<List<(Capitulo cap, DateOnly fecha)>> CapitulosPublicados(DateOnly hoy)
    {
        var cal = (await CalendarioCapitulos()).Where(kv => kv.Value <= hoy).ToDictionary();
        var ids = cal.Keys.ToList();
        var caps = await db.Capitulos.Include(c => c.Obras.Where(o => o.Activa)).ThenInclude(o => o.Paradas).Where(c => ids.Contains(c.Id)).ToListAsync();
        return caps.Select(c => (c, cal[c.Id])).OrderByDescending(x => x.Item2).ToList();
    }

    public async Task<(Obra obra, DateOnly fecha)?> DeLaSemana(DateOnly hoy) =>
        (await Publicadas(hoy)).Cast<(Obra, DateOnly)?>().FirstOrDefault(x => x!.Value.Item2 == DomingoDe(hoy));

    /// <summary>Una obra suelta se ve desde su domingo; la de un capítulo, desde el del capítulo.</summary>
    public async Task<bool> PuedeVer(int obraId, DateOnly hoy, bool admin)
    {
        if (admin) return true;
        var capId = await db.Obras.Where(o => o.Id == obraId).Select(o => o.CapituloId).FirstOrDefaultAsync();
        if (capId is int c) return await PuedeVerCapitulo(c, hoy, false);
        return (await Calendario()).TryGetValue(obraId, out var f) && f <= hoy;
    }

    public async Task<bool> PuedeVerCapitulo(int capId, DateOnly hoy, bool admin) =>
        admin || (await CalendarioCapitulos()).TryGetValue(capId, out var f) && f <= hoy;

    /// <summary>Primer domingo futuro sin capítulo ni obra con fecha fijada (las obras de la cola se apartan).</summary>
    public async Task<DateOnly> ProximoDomingoLibre(DateOnly hoy, int? excepto = null)
    {
        var fijos = (await db.Obras.Where(o => o.Activa && o.CapituloId == null && o.Publicacion != null).Select(o => o.Publicacion!.Value).ToListAsync())
            .Select(DomingoDe).Concat((await CalendarioCapitulos()).Where(kv => kv.Key != excepto).Select(kv => kv.Value)).ToHashSet();
        var d = DomingoDe(hoy).AddDays(7);
        if (d < Estreno) d = Estreno;
        while (fijos.Contains(d)) d = d.AddDays(7);
        return d;
    }

    /// <summary>Texto de un capítulo a HTML: "## " sección (con ancla), "### " subtítulo, "- " lista, **negrita**, *cursiva*.</summary>
    public static string TextoHtml(string texto, out List<(string id, string titulo)> secciones)
    {
        secciones = new();
        var sb = new System.Text.StringBuilder();
        static string Inline(string t)
        {
            var h = System.Net.WebUtility.HtmlEncode(t.Trim());
            h = System.Text.RegularExpressions.Regex.Replace(h, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            return System.Text.RegularExpressions.Regex.Replace(h, @"(?<![*\w])\*(?!\s)(.+?)(?<!\s)\*(?![*\w])", "<em>$1</em>");
        }
        foreach (var raw in texto.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var b = raw.Trim('\n');
            if (string.IsNullOrWhiteSpace(b)) continue;
            if (b.StartsWith("## "))
            {
                var t = b[3..].Trim(); var id = "s" + (secciones.Count + 1);
                secciones.Add((id, t));
                sb.Append($"<h2 id=\"{id}\">").Append(Inline(t)).Append("</h2>");
            }
            else if (b.StartsWith("### "))
            {
                // "### Título" y, si sigue en la línea de abajo sin línea en blanco, su párrafo
                var nl = b.IndexOf('\n');
                sb.Append("<h3>").Append(Inline(nl < 0 ? b[4..] : b[4..nl])).Append("</h3>");
                if (nl >= 0) sb.Append("<p>").Append(Inline(b[(nl + 1)..].Replace('\n', ' '))).Append("</p>");
            }
            else if (b.StartsWith("- "))
            {
                sb.Append("<ul>");
                foreach (var item in System.Text.RegularExpressions.Regex.Split(b, @"(?:^|\n)- ").Where(x => !string.IsNullOrWhiteSpace(x)))
                    sb.Append("<li>").Append(Inline(item.Replace('\n', ' '))).Append("</li>");
                sb.Append("</ul>");
            }
            else if (b.StartsWith("> ")) sb.Append("<blockquote>").Append(Inline(string.Join(' ', b.Split('\n').Select(l => l.TrimStart('>', ' '))))).Append("</blockquote>");
            else sb.Append("<p>").Append(Inline(b.Replace('\n', ' '))).Append("</p>");
        }
        return sb.ToString();
    }

    // ---------- Imágenes ----------
    /// <summary>Guarda la imagen en dos tamaños (máx. 4096 y 1600 px por el lado largo). Devuelve rutas y dimensiones de la grande.</summary>
    public async Task<(string grande, string reducida, int ancho, int alto)> GuardarImagen(Stream origen)
    {
        using var img = await Image.LoadAsync(origen);
        img.Mutate(x => x.AutoOrient());
        Directory.CreateDirectory(Path.Combine(files.Raiz, "arte"));
        var id = Guid.NewGuid().ToString("N");
        string Guardar(int lado, string sufijo, int calidad)
        {
            using var copia = img.Clone(x => { if (Math.Max(img.Width, img.Height) > lado) x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(lado, lado), Sampler = KnownResamplers.Lanczos3 }); });
            var rel = $"arte/{id}-{sufijo}.jpg";
            copia.SaveAsJpeg(Path.Combine(files.Raiz, rel), new JpegEncoder { Quality = calidad });
            return rel;
        }
        var g = Guardar(LadoGrande, "g", 86);
        var r = Guardar(LadoReducido, "r", 82);
        var escala = Math.Min(1.0, (double)LadoGrande / Math.Max(img.Width, img.Height));
        return (g, r, (int)Math.Round(img.Width * escala), (int)Math.Round(img.Height * escala));
    }

    // ---------- Seed ----------
    public record ObraSeed(string Clave, string Fichero, bool Existe, bool Importada);

    JsonDocument? LeerSeed()
    {
        var ruta = Path.Combine(env.ContentRootPath, "Seed", "arte.json");
        return File.Exists(ruta) ? JsonDocument.Parse(File.ReadAllText(ruta)) : null;
    }

    /// <summary>Obras del seed: las sueltas y las de cada capítulo (con la clave del capítulo).</summary>
    static IEnumerable<(JsonElement obra, string? capitulo)> ObrasSeed(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("obras", out var sueltas))
            foreach (var o in sueltas.EnumerateArray()) yield return (o, null);
        if (doc.RootElement.TryGetProperty("capitulos", out var caps))
            foreach (var c in caps.EnumerateArray())
                foreach (var o in c.GetProperty("obras").EnumerateArray()) yield return (o, c.GetProperty("clave").GetString());
    }

    public async Task<List<ObraSeed>> EstadoSeed()
    {
        using var doc = LeerSeed();
        if (doc == null) return new();
        var importadas = (await db.Obras.Where(o => o.Origen != null).Select(o => o.Origen!).ToListAsync()).ToHashSet();
        return ObrasSeed(doc).Select(x =>
        {
            var clave = x.obra.GetProperty("clave").GetString()!; var fichero = x.obra.GetProperty("fichero").GetString()!;
            return new ObraSeed(clave, fichero, File.Exists(Path.Combine(CarpetaEntrada, fichero)), importadas.Contains(clave));
        }).ToList();
    }

    static string? S(JsonElement e, string k) => e.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null;

    /// <summary>
    /// Importa de Seed/arte.json los capítulos que no existan aún y las obras (sueltas o de capítulo) cuya imagen esté
    /// en App_Data/entrada/arte. Se puede repetir: lo ya importado se salta (por su clave).
    /// </summary>
    public async Task<int> ImportarSeed()
    {
        using var doc = LeerSeed();
        if (doc == null) return 0;
        // Capítulos (sin fecha: se publican desde Admin cuando estén listos)
        var caps = await db.Capitulos.Where(c => c.Origen != null).ToDictionaryAsync(c => c.Origen!);
        if (doc.RootElement.TryGetProperty("capitulos", out var jc))
            foreach (var c in jc.EnumerateArray())
            {
                var clave = c.GetProperty("clave").GetString()!;
                if (caps.ContainsKey(clave)) continue;
                var texto = S(c, "texto");
                if (S(c, "textoFichero") is string tf && File.Exists(Path.Combine(env.ContentRootPath, "Seed", tf)))
                    texto = await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Seed", tf));
                var cap = new Capitulo { Origen = clave, Artista = S(c, "artista") ?? clave, Titulo = S(c, "titulo") ?? S(c, "artista") ?? clave,
                                         Fechas = S(c, "fechas"), Entradilla = S(c, "entradilla"), Texto = texto ?? "" };
                db.Capitulos.Add(cap); await db.SaveChangesAsync();
                caps[clave] = cap;
                log.LogInformation("Capítulo importado: {Titulo}", cap.Titulo);
            }

        var importadas = (await db.Obras.Where(o => o.Origen != null).Select(o => o.Origen!).ToListAsync()).ToHashSet();
        int orden = (await db.Obras.MaxAsync(o => (int?)o.Orden) ?? 0), n = 0;
        foreach (var (e, capClave) in ObrasSeed(doc))
        {
            var clave = e.GetProperty("clave").GetString()!;
            var ruta = Path.Combine(CarpetaEntrada, e.GetProperty("fichero").GetString()!);
            if (importadas.Contains(clave) || !File.Exists(ruta)) continue;
            await using var fs = File.OpenRead(ruta);
            var (g, r, w, h) = await GuardarImagen(fs);
            var o = new Obra
            {
                Origen = clave, Titulo = S(e, "titulo") ?? clave, Autor = S(e, "autor"), Anio = S(e, "anio"), Museo = S(e, "museo"),
                FuenteUrl = S(e, "fuente"), Licencia = S(e, "licencia"), Intro = S(e, "intro"), FraseMia = S(e, "frase"),
                ImagenPath = g, ReducidaPath = r, Ancho = w, Alto = h, Orden = ++orden,
                Publicacion = capClave == null && DateOnly.TryParse(S(e, "publicacion"), out var pub) ? pub : null,
                CapituloId = capClave != null && caps.TryGetValue(capClave, out var cp) ? cp.Id : null,
            };
            int i = 0;
            if (e.TryGetProperty("paradas", out var ps))
                foreach (var p in ps.EnumerateArray())
                    o.Paradas.Add(new ParadaObra
                    {
                        Orden = i++, X = p.GetProperty("x").GetDouble(), Y = p.GetProperty("y").GetDouble(),
                        Zoom = p.TryGetProperty("zoom", out var z) ? z.GetDouble() : 3,
                        Titulo = p.GetProperty("titulo").GetString() ?? "", Texto = p.GetProperty("texto").GetString() ?? "",
                    });
            db.Obras.Add(o);
            await db.SaveChangesAsync();
            n++;
            log.LogInformation("Obra importada: {Titulo} ({W}x{H}, {N} paradas)", o.Titulo, w, h, o.Paradas.Count);
        }
        // Portadas: la obra indicada en el seed, una vez importada
        if (doc.RootElement.TryGetProperty("capitulos", out var jc2))
            foreach (var c in jc2.EnumerateArray())
                if (S(c, "portada") is string pc && caps.TryGetValue(c.GetProperty("clave").GetString()!, out var cap) && cap.PortadaObraId == null)
                    cap.PortadaObraId = await db.Obras.Where(o => o.Origen == pc).Select(o => (int?)o.Id).FirstOrDefaultAsync();
        await db.SaveChangesAsync();
        return n;
    }
}
