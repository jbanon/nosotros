using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Menu;

public class SemanaModel(AppDb db, ListaCompraService lista) : PageModel
{
    public Semana Semana { get; set; } = null!;
    public List<Receta> Recetas { get; set; } = new();
    public DateOnly Lunes { get; set; }
    public int Raciones { get; set; }

    DateOnly Resolver(DateOnly? lunes) => Reloj.Lunes(lunes ?? Reloj.Hoy);

    public async Task OnGet(DateOnly? lunes)
    {
        Lunes = Resolver(lunes);
        Semana = await lista.SemanaDe(Lunes);
        Recetas = await db.Recetas.OrderBy(r => r.Nombre).ToListAsync();
        Raciones = await lista.Raciones();
    }

    public async Task<IActionResult> OnPostToggle(DateOnly lunes, int recetaId)
    {
        var s = await lista.SemanaDe(lunes);
        var sr = s.Recetas.FirstOrDefault(x => x.RecetaId == recetaId);
        if (sr == null) s.Recetas.Add(new SemanaReceta { RecetaId = recetaId });
        else db.SemanaRecetas.Remove(sr);
        await db.SaveChangesAsync();
        await lista.Regenerar(s.Id);
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostVeces(DateOnly lunes, int recetaId, int delta)
    {
        var s = await lista.SemanaDe(lunes);
        var sr = s.Recetas.FirstOrDefault(x => x.RecetaId == recetaId);
        if (sr != null)
        {
            sr.Veces = Math.Clamp(sr.Veces + delta, 1, 7);
            await db.SaveChangesAsync();
            await lista.Regenerar(s.Id);
        }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostVaciar(DateOnly lunes)
    {
        var s = await lista.SemanaDe(lunes);
        db.SemanaRecetas.RemoveRange(s.Recetas);
        await db.SaveChangesAsync();
        await lista.Regenerar(s.Id);
        return RedirectToPage(new { lunes });
    }
}
