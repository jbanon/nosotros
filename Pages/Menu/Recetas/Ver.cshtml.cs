using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Menu.Recetas;

public class VerModel(AppDb db, ListaCompraService lista) : PageModel
{
    public Receta Receta { get; set; } = null!;
    public bool EnSemana { get; set; }

    public async Task<IActionResult> OnGet(int id)
    {
        var r = await db.Recetas.Include(x => x.Ingredientes.OrderBy(i => i.Orden)).ThenInclude(i => i.Ingrediente).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        Receta = r;
        var lunes = Reloj.Lunes(Reloj.Hoy);
        EnSemana = await db.SemanaRecetas.AnyAsync(s => s.Semana.LunesInicio == lunes && s.RecetaId == id);
        return Page();
    }

    public async Task<IActionResult> OnPostSemana(int id)
    {
        var s = await lista.SemanaDe(Reloj.Lunes(Reloj.Hoy));
        var sr = s.Recetas.FirstOrDefault(x => x.RecetaId == id);
        if (sr == null) s.Recetas.Add(new SemanaReceta { RecetaId = id });
        else db.SemanaRecetas.Remove(sr);
        await db.SaveChangesAsync();
        await lista.Regenerar(s.Id);
        return RedirectToPage(new { id });
    }
}
