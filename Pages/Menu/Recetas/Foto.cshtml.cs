using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Menu.Recetas;

public class FotoModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var r = await db.Recetas.FindAsync(id);
        if (r?.FotoPath == null) return NotFound();
        var p = files.RutaAbsoluta(r.FotoPath);
        var mime = Path.GetExtension(p).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
        return System.IO.File.Exists(p) ? PhysicalFile(p, mime) : NotFound();
    }
}
