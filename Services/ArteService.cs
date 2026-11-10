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
    /// Fecha de publicación efectiva: la fijada, o el siguiente domingo libre siguiendo el orden de la cola.
    /// </summary>
    public async Task<Dictionary<int, DateOnly>> Calendario()
    {
        var obras = await db.Obras.Where(o => o.Activa).OrderBy(o => o.Orden).ThenBy(o => o.Id).ToListAsync();
        var res = new Dictionary<int, DateOnly>();
        var ocupados = obras.Where(o => o.Publicacion != null).Select(o => DomingoDe(o.Publicacion!.Value)).ToHashSet();
        foreach (var o in obras.Where(o => o.Publicacion != null)) res[o.Id] = DomingoDe(o.Publicacion!.Value);
        var d = Estreno;
        foreach (var o in obras.Where(o => o.Publicacion == null))
        {
            while (ocupados.Contains(d)) d = d.AddDays(7);
            res[o.Id] = d; ocupados.Add(d); d = d.AddDays(7);
        }
        return res;
    }

    /// <summary>Obras ya publicadas a fecha <paramref name="hoy"/>, de la más reciente a la más antigua.</summary>
    public async Task<List<(Obra obra, DateOnly fecha)>> Publicadas(DateOnly hoy)
    {
        var cal = await Calendario();
        var ids = cal.Where(kv => kv.Value <= hoy).Select(kv => kv.Key).ToList();
        var obras = await db.Obras.Include(o => o.Paradas).Where(o => ids.Contains(o.Id)).ToListAsync();
        return obras.Select(o => (o, cal[o.Id])).OrderByDescending(x => x.Item2).ToList();
    }

    public async Task<(Obra obra, DateOnly fecha)?> DeLaSemana(DateOnly hoy) =>
        (await Publicadas(hoy)).Cast<(Obra, DateOnly)?>().FirstOrDefault(x => x!.Value.Item2 == DomingoDe(hoy));

    public async Task<bool> PuedeVer(int obraId, DateOnly hoy, bool admin) =>
        admin || (await Calendario()).TryGetValue(obraId, out var f) && f <= hoy;

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

    public async Task<List<ObraSeed>> EstadoSeed()
    {
        using var doc = LeerSeed();
        if (doc == null) return new();
        var importadas = (await db.Obras.Where(o => o.Origen != null).Select(o => o.Origen!).ToListAsync()).ToHashSet();
        return doc.RootElement.GetProperty("obras").EnumerateArray().Select(o =>
        {
            var clave = o.GetProperty("clave").GetString()!; var fichero = o.GetProperty("fichero").GetString()!;
            return new ObraSeed(clave, fichero, File.Exists(Path.Combine(CarpetaEntrada, fichero)), importadas.Contains(clave));
        }).ToList();
    }

    /// <summary>Importa las obras de Seed/arte.json cuya imagen esté en App_Data/entrada/arte y que no existan aún.</summary>
    public async Task<int> ImportarSeed()
    {
        using var doc = LeerSeed();
        if (doc == null) return 0;
        var importadas = (await db.Obras.Where(o => o.Origen != null).Select(o => o.Origen!).ToListAsync()).ToHashSet();
        int orden = (await db.Obras.MaxAsync(o => (int?)o.Orden) ?? 0), n = 0;
        foreach (var e in doc.RootElement.GetProperty("obras").EnumerateArray())
        {
            var clave = e.GetProperty("clave").GetString()!;
            var ruta = Path.Combine(CarpetaEntrada, e.GetProperty("fichero").GetString()!);
            if (importadas.Contains(clave) || !File.Exists(ruta)) continue;
            string? S(string k) => e.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null;
            await using var fs = File.OpenRead(ruta);
            var (g, r, w, h) = await GuardarImagen(fs);
            var o = new Obra
            {
                Origen = clave, Titulo = S("titulo") ?? clave, Autor = S("autor"), Anio = S("anio"), Museo = S("museo"),
                FuenteUrl = S("fuente"), Licencia = S("licencia"), Intro = S("intro"), FraseMia = S("frase"),
                ImagenPath = g, ReducidaPath = r, Ancho = w, Alto = h, Orden = ++orden,
                Publicacion = DateOnly.TryParse(S("publicacion"), out var pub) ? pub : null,
            };
            int i = 0;
            foreach (var p in e.GetProperty("paradas").EnumerateArray())
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
        return n;
    }
}
