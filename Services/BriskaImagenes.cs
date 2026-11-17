namespace Love.Services;

/// <summary>Las imágenes de Briska (PNG recortados con fondo transparente) que hay en wwwroot/img/briska.
/// Se listan del disco, sin base de datos: basta con copiar un PNG nuevo a la carpeta para que salga.</summary>
public class BriskaImagenes(IWebHostEnvironment env)
{
    public const string Carpeta = "img/briska";
    readonly object candado = new();
    IReadOnlyList<string> cache = [];
    DateTime cacheEn = DateTime.MinValue;

    /// <summary>Rutas web de todas las imágenes, ordenadas por nombre; se vuelve a mirar el disco cada minuto.</summary>
    public IReadOnlyList<string> Rutas()
    {
        lock (candado)
        {
            if ((DateTime.UtcNow - cacheEn).TotalSeconds < 60) return cache;
            var dir = Path.Combine(env.WebRootPath, "img", "briska");
            cache = Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.png").Select(f => Path.GetFileName(f)).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(f => $"/{Carpeta}/{f}").ToList()
                : [];
            cacheEn = DateTime.UtcNow;
            return cache;
        }
    }

    public string Json() => AdvientoService.Serializar(Rutas());
}
