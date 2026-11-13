using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Arte;

public class IndexModel(AppDb db, ArteService arte) : PageModel
{
    public List<Obra> Obras { get; set; } = new();
    public Dictionary<int, DateOnly> Calendario { get; set; } = new();
    public List<ArteService.ObraSeed> Seed { get; set; } = new();
    public int? Importadas { get; set; }
    public List<Capitulo> Capitulos { get; set; } = new();
    public Dictionary<int, DateOnly> CalCapitulos { get; set; } = new();

    public async Task OnGet(int? importadas)
    {
        Importadas = importadas;
        Calendario = await arte.Calendario();
        Obras = (await db.Obras.Include(o => o.Paradas).ToListAsync())
            .OrderBy(o => Calendario.TryGetValue(o.Id, out var f) ? f : DateOnly.MaxValue).ThenBy(o => o.Orden).ToList();
        Seed = await arte.EstadoSeed();
        CalCapitulos = await arte.CalendarioCapitulos();
        Capitulos = await db.Capitulos.Include(c => c.Obras).OrderBy(c => c.Publicacion == null).ThenBy(c => c.Publicacion).ThenByDescending(c => c.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostImportar() => RedirectToPage(new { importadas = await arte.ImportarSeed() });

    /// <summary>Mover una obra en la cola (solo afecta a las que no tienen domingo fijado).</summary>
    public async Task<IActionResult> OnPostMover(int id, int dir)
    {
        var cola = await db.Obras.Where(o => o.Activa && o.Publicacion == null && o.CapituloId == null).OrderBy(o => o.Orden).ThenBy(o => o.Id).ToListAsync();
        var i = cola.FindIndex(o => o.Id == id); var j = i + dir;
        if (i >= 0 && j >= 0 && j < cola.Count)
        {
            (cola[i], cola[j]) = (cola[j], cola[i]);
            for (int k = 0; k < cola.Count; k++) cola[k].Orden = k + 1;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVistas()
    {
        foreach (var o in await db.Obras.Where(o => o.RespuestaRaquel != null && !o.RespuestaVista).ToListAsync()) o.RespuestaVista = true;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
