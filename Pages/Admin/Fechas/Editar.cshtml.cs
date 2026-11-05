using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Admin.Fechas;

public class EditarModel(AppDb db) : PageModel
{
    [BindProperty] public FechaEspecial Fecha { get; set; } = new();
    public bool Nueva => Fecha.Id == 0;

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i) { var f = await db.FechasEspeciales.FindAsync(i); if (f == null) return NotFound(); Fecha = f; }
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(Fecha.Nombre) || string.IsNullOrWhiteSpace(Fecha.Texto)) { ModelState.AddModelError("", "Nombre y texto son obligatorios"); return Page(); }
        if (Fecha.Mes < 1 || Fecha.Mes > 12 || Fecha.Dia < 1 || Fecha.Dia > 31) { ModelState.AddModelError("", "Fecha no válida"); return Page(); }
        if (string.IsNullOrWhiteSpace(Fecha.Tema)) Fecha.Tema = null;
        if (string.IsNullOrWhiteSpace(Fecha.TextoContador)) Fecha.TextoContador = null;
        Fecha.Texto = Fecha.Texto.Replace("\r\n", "\n").Trim();
        if (Fecha.Id == 0) db.FechasEspeciales.Add(Fecha); else db.FechasEspeciales.Update(Fecha);
        await db.SaveChangesAsync();
        return Redirect("/Admin/Fechas");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var f = await db.FechasEspeciales.FindAsync(id);
        if (f != null) { db.FechasEspeciales.Remove(f); await db.SaveChangesAsync(); }
        return Redirect("/Admin/Fechas");
    }
}
