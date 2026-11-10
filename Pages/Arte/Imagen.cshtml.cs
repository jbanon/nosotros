using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Arte;

/// <summary>Imagen de una obra: t=r (reducida, 1600 px) o t=g (grande, 4096 px).</summary>
public class ImagenModel(AppDb db, FileStorage files, ArteService arte) : PageModel
{
    public async Task<IActionResult> OnGet(int id, string? t)
    {
        var o = await db.Obras.FindAsync(id);
        if (o == null || !await arte.PuedeVer(id, Reloj.Hoy, User.EsAdmin())) return NotFound();
        var rel = t == "g" ? o.ImagenPath : o.ReducidaPath ?? o.ImagenPath;
        if (rel == null) return NotFound();
        var p = files.RutaAbsoluta(rel);
        if (!System.IO.File.Exists(p)) return NotFound();
        Response.Headers.CacheControl = "private, max-age=2592000, immutable";
        return PhysicalFile(p, "image/jpeg");
    }
}
