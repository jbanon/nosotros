using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Admin.Mensajes;

public class EditarModel(AppDb db) : PageModel
{
    [BindProperty] public Mensaje Mensaje { get; set; } = new();
    public bool Nuevo => Mensaje.Id == 0;

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i) { var m = await db.Mensajes.FindAsync(i); if (m == null) return NotFound(); Mensaje = m; }
        else Mensaje = new Mensaje { Mes = Reloj.Hoy.Month, Dia = Reloj.Hoy.Day };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(Mensaje.Texto)) { ModelState.AddModelError("Mensaje.Texto", "Escribe algo"); return Page(); }
        if (Mensaje.Mes < 1 || Mensaje.Mes > 12 || Mensaje.Dia < 1 || Mensaje.Dia > 31) { ModelState.AddModelError("Mensaje.Dia", "Fecha no válida"); return Page(); }
        Mensaje.Texto = Mensaje.Texto.Replace("\r\n", "\n").Trim();
        if (Mensaje.Id == 0) db.Mensajes.Add(Mensaje); else db.Mensajes.Update(Mensaje);
        await db.SaveChangesAsync();
        return Redirect($"/Admin/Mensajes?mes={Mensaje.Mes}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var m = await db.Mensajes.FindAsync(id);
        if (m != null) { db.Mensajes.Remove(m); await db.SaveChangesAsync(); return Redirect($"/Admin/Mensajes?mes={m.Mes}"); }
        return Redirect("/Admin/Mensajes");
    }
}
