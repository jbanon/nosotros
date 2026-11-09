using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Peliculas;

public class PosterModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var p = await db.Peliculas.FindAsync(id);
        if (p?.PosterPath == null) return NotFound();
        var ruta = files.RutaAbsoluta(p.PosterPath);
        var mime = Path.GetExtension(ruta).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        return System.IO.File.Exists(ruta) ? PhysicalFile(ruta, mime) : NotFound();
    }
}
