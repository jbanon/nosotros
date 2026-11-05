using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Gastos;

public class AdjuntoModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var g = await db.Gastos.FindAsync(id);
        if (g?.AdjuntoPath == null) return NotFound();
        var p = files.RutaAbsoluta(g.AdjuntoPath);
        if (!System.IO.File.Exists(p)) return NotFound();
        var mime = g.AdjuntoMime ?? "application/octet-stream";
        return mime.StartsWith("image/") || mime == "application/pdf" ? PhysicalFile(p, mime) : PhysicalFile(p, mime, g.AdjuntoNombre);
    }
}
