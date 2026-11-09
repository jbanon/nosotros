using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Menu.Recetas;

public class AgenteFotoModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var t = await db.TareasAgente.FindAsync(id);
        if (t?.FotoPath == null) return NotFound();
        var p = files.RutaAbsoluta(t.FotoPath);
        var mime = Path.GetExtension(p).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", ".pdf" => "application/pdf", _ => "image/jpeg" };
        return System.IO.File.Exists(p) ? PhysicalFile(p, mime) : NotFound();
    }
}
