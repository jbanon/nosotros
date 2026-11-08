using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Lecturas;

public class LeerModel(AppDb db) : PageModel
{
    public Lectura Lectura { get; set; } = null!;

    public async Task<IActionResult> OnGet(int id)
    {
        var l = await db.Lecturas.FindAsync(id);
        if (l == null || (!l.Publicada && !User.EsAdmin())) return NotFound();
        Lectura = l;
        return Page();
    }

    public async Task<IActionResult> OnPostLeida(int id, bool leida)
    {
        var l = await db.Lecturas.FindAsync(id);
        if (l != null) { l.Leida = leida; l.LeidaEl = leida ? DateTime.UtcNow : null; await db.SaveChangesAsync(); }
        return Redirect(leida ? "/Lecturas" : $"/Lecturas/Leer/{id}");
    }
}
