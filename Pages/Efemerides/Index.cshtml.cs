using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Love.Data;

namespace Love.Pages.Efemerides;

/// <summary>"Tal día como hoy": los artículos de las efemérides de un día (por defecto, hoy; nunca días futuros salvo Javi).</summary>
public class IndexModel(EfemeridesService efes, AppDb db) : PageModel
{
    public DateOnly Fecha { get; set; }
    public List<EfemeridesService.Item> Items { get; set; } = new();
    public bool Preparando { get; set; }
    public TareaAgente? UltimaTarea { get; set; }
    public bool EsAdmin => User.EsAdmin();

    public async Task<IActionResult> OnGet(DateOnly? fecha)
    {
        Fecha = fecha ?? Reloj.Hoy;
        if (Fecha > Reloj.Hoy && !EsAdmin) return Redirect("/Efemerides");
        Items = await efes.Del(Fecha);
        Preparando = await efes.Preparando(Fecha);
        if (EsAdmin)
        {
            var k = EfemeridesService.Clave(Fecha);
            UltimaTarea = await db.TareasAgente.Where(t => t.Tipo == TipoTareaAgente.Efemerides && t.Entrada == k).OrderByDescending(t => t.Id).FirstOrDefaultAsync();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostRehacer(DateOnly fecha)
    {
        if (!EsAdmin) return Forbid();
        await efes.Rehacer(fecha, User.Identity?.Name ?? "Javi");
        return Redirect($"/Efemerides?fecha={fecha:yyyy-MM-dd}");
    }
}
