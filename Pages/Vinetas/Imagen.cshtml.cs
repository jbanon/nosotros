using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Vinetas;

public class ImagenModel(AppDb db, FileStorage files, VinetaService vinetas) : PageModel
{
    public async Task<IActionResult> OnGet(int id, bool descargar)
    {
        var v = await db.Vinetas.FindAsync(id);
        if (v == null) return NotFound();
        if (!await vinetas.PuedeVer(id, Reloj.Hoy, User.EsAdmin())) return NotFound();
        var p = files.RutaAbsoluta(v.Path);
        if (!System.IO.File.Exists(p)) return NotFound();
        var ext = Path.GetExtension(p).ToLowerInvariant();
        var mime = ext switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
        Response.Headers.CacheControl = "private, max-age=86400";
        return descargar ? PhysicalFile(p, mime, $"vineta-{id}{ext}") : PhysicalFile(p, mime);
    }
}
