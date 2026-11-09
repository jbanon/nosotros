using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Adviento;

public class FotoModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var c = await db.Adviento.FindAsync(id);
        if (c?.FotoPath == null) return NotFound();
        if (!User.EsAdmin() && !AdvientoService.Abierta(c, Reloj.Hoy)) return NotFound();
        var p = files.RutaAbsoluta(c.FotoPath);
        var mime = Path.GetExtension(p).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
        return System.IO.File.Exists(p) ? PhysicalFile(p, mime) : NotFound();
    }
}
