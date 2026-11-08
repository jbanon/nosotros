using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Admin.Lecturas;

public class EditarModel(AppDb db) : PageModel
{
    [BindProperty] public Lectura Lectura { get; set; } = new();
    [BindProperty] public IFormFile? Fichero { get; set; }
    public bool Nueva => Lectura.Id == 0;

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i)
        {
            var l = await db.Lecturas.FindAsync(i);
            if (l == null) return NotFound();
            Lectura = l;
        }
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (Fichero != null && Fichero.Length > 0 && Fichero.Length < 2_000_000)
        {
            using var sr = new StreamReader(Fichero.OpenReadStream());
            Lectura.Texto = await sr.ReadToEndAsync();
        }
        if (string.IsNullOrWhiteSpace(Lectura.Titulo)) { ModelState.AddModelError("Lectura.Titulo", "Ponle un título"); return Page(); }
        if (string.IsNullOrWhiteSpace(Lectura.Texto)) { ModelState.AddModelError("Lectura.Texto", "Falta el texto"); return Page(); }

        Lectura l;
        if (Lectura.Id == 0) { l = new Lectura(); db.Lecturas.Add(l); }
        else l = await db.Lecturas.FindAsync(Lectura.Id) ?? throw new InvalidOperationException();
        l.Titulo = Lectura.Titulo.Trim();
        l.Autor = string.IsNullOrWhiteSpace(Lectura.Autor) ? null : Lectura.Autor.Trim();
        l.Fuente = string.IsNullOrWhiteSpace(Lectura.Fuente) ? null : Lectura.Fuente.Trim();
        l.Dedicatoria = string.IsNullOrWhiteSpace(Lectura.Dedicatoria) ? null : Lectura.Dedicatoria.Replace("\r", "").Trim();
        l.Texto = Lectura.Texto.Replace("\r", "").Trim();
        l.Publicada = Lectura.Publicada;
        await db.SaveChangesAsync();
        return Redirect($"/Lecturas/Leer/{l.Id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var l = await db.Lecturas.FindAsync(id);
        if (l != null) { db.Lecturas.Remove(l); await db.SaveChangesAsync(); }
        return Redirect("/Admin/Lecturas");
    }
}
