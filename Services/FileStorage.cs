namespace Love.Services;

public class FileStorage(IWebHostEnvironment env, IConfiguration cfg)
{
    public string Raiz => Path.GetFullPath(cfg["Rutas:Adjuntos"] ?? Path.Combine(env.ContentRootPath, "App_Data", "adjuntos"));
    public string RutaDb => Path.GetFullPath(cfg["Rutas:Db"] ?? Path.Combine(env.ContentRootPath, "App_Data", "love.db"));

    static readonly HashSet<string> Permitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".pdf", ".txt", ".csv", ".xlsx", ".docx"];

    /// <summary>Guarda el fichero (las fotos ya llegan reducidas desde el navegador). Devuelve ruta relativa y mime.</summary>
    public async Task<(string path, string mime)> Guardar(IFormFile f, string subcarpeta)
    {
        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
        if (!Permitidas.Contains(ext)) ext = f.ContentType.StartsWith("image/") ? ".jpg" : ".bin";
        var dir = Path.Combine(Raiz, subcarpeta);
        Directory.CreateDirectory(dir);
        var rel = Path.Combine(subcarpeta, Guid.NewGuid().ToString("N") + ext).Replace('\\', '/');
        using (var fs = File.Create(Path.Combine(Raiz, rel)))
            await f.CopyToAsync(fs);
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif",
            ".pdf" => "application/pdf", _ => string.IsNullOrEmpty(f.ContentType) ? "application/octet-stream" : f.ContentType
        };
        return (rel, mime);
    }

    public string RutaAbsoluta(string rel)
    {
        var p = Path.GetFullPath(Path.Combine(Raiz, rel));
        if (!p.StartsWith(Raiz)) throw new UnauthorizedAccessException();
        return p;
    }

    public void Borrar(string? rel)
    {
        if (string.IsNullOrEmpty(rel)) return;
        try { File.Delete(RutaAbsoluta(rel)); } catch { }
    }
}
