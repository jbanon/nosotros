using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Ideas;

public class EditarModel(AppDb db) : PageModel
{
    [BindProperty] public Idea Idea { get; set; } = new();

    public async Task<IActionResult> OnGet(int id)
    {
        var i = await db.Ideas.FindAsync(id);
        if (i == null) return NotFound();
        Idea = i;
        return Page();
    }

    public async Task<IActionResult> OnPost(int id)
    {
        var i = await db.Ideas.FindAsync(id);
        if (i == null) return NotFound();
        var texto = (Idea.Texto ?? "").Trim();
        if (texto == "") { ModelState.AddModelError("Idea.Texto", "Escribe la idea"); Idea.Id = id; return Page(); }
        i.Texto = texto;
        i.Nota = string.IsNullOrWhiteSpace(Idea.Nota) ? null : Idea.Nota.Trim();
        i.Resumen = string.IsNullOrWhiteSpace(Idea.Resumen) ? null : Idea.Resumen.Replace("\r", "").Trim();
        if (Idea.Hecha != i.Hecha) { i.Hecha = Idea.Hecha; i.HechaEl = Idea.Hecha ? DateTime.UtcNow : null; }
        await db.SaveChangesAsync();
        return Redirect(i.Hecha ? "/Ideas/Hechas" : "/Ideas");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var i = await db.Ideas.FindAsync(id);
        if (i != null) { db.Ideas.Remove(i); await db.SaveChangesAsync(); }
        return Redirect("/Ideas");
    }
}
