using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Love.Pages.Menu;

public class ListaModel(AppDb db, ListaCompraService lista) : PageModel
{
    public Semana Semana { get; set; } = null!;
    public DateOnly Lunes { get; set; }
    public string TextoCompartir { get; set; } = "";
    public int Raciones { get; set; }

    public async Task OnGet(DateOnly? lunes)
    {
        Lunes = Reloj.Lunes(lunes ?? Reloj.Hoy);
        Semana = await lista.SemanaDe(Lunes);
        await lista.Regenerar(Semana.Id);
        Semana = await lista.SemanaDe(Lunes);
        Raciones = await lista.Raciones();
        TextoCompartir = ListaCompraService.TextoCompartir(Semana, Raciones);
    }

    public async Task<IActionResult> OnPostComprado(DateOnly lunes, int id)
    {
        var it = await db.ListaItems.FindAsync(id);
        if (it != null) { it.Comprado = !it.Comprado; await db.SaveChangesAsync(); }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostIncluir(DateOnly lunes, int id, bool incluir)
    {
        var it = await db.ListaItems.FindAsync(id);
        if (it != null)
        {
            if (!incluir && it.Manual) db.ListaItems.Remove(it);
            else { it.Incluido = incluir; if (!incluir) it.Comprado = false; }
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostAnadir(DateOnly lunes, string nombre, string? cantidad, Zona zona)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre != "")
        {
            var s = await lista.SemanaDe(lunes);
            var ing = await db.Ingredientes.FirstOrDefaultAsync(i => i.Nombre.ToLower() == nombre.ToLower());
            decimal? cant = null; string? unidad = null;
            if (!string.IsNullOrWhiteSpace(cantidad))
            {
                var partes = cantidad.Trim().Split(' ', 2);
                if (decimal.TryParse(partes[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var c)) { cant = c; unidad = partes.Length > 1 ? partes[1] : null; }
                else unidad = cantidad.Trim();
            }
            s.Lista.Add(new ListaItem { Nombre = ing?.Nombre ?? nombre, IngredienteId = ing?.Id, Zona = ing?.Zona ?? zona, Cantidad = cant, Unidad = unidad, Manual = true, Incluido = true });
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostLimpiar(DateOnly lunes)
    {
        var s = await lista.SemanaDe(lunes);
        foreach (var it in s.Lista.Where(i => i.Comprado)) { if (it.Manual) db.ListaItems.Remove(it); else it.Comprado = false; }
        await db.SaveChangesAsync();
        return RedirectToPage(new { lunes });
    }
}
