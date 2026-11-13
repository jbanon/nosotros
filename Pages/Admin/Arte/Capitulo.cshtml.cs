using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Arte;

/// <summary>Editar un capítulo (artista + texto + obras) y decidir el domingo en que sale.</summary>
public class CapituloModel(AppDb db, ArteService arte) : PageModel
{
    [BindProperty] public Capitulo C { get; set; } = new();
    public List<Obra> Obras { get; set; } = new();
    public DateOnly ProximoLibre { get; set; }
    public bool Nuevo => C.Id == 0;
    public string? Error { get; set; }

    async Task Cargar(int? id)
    {
        if (id is int i)
        {
            C = await db.Capitulos.FirstOrDefaultAsync(c => c.Id == i) ?? throw new InvalidOperationException();
            Obras = await db.Obras.Include(o => o.Paradas).Where(o => o.CapituloId == i).OrderBy(o => o.Orden).ThenBy(o => o.Id).ToListAsync();
        }
        ProximoLibre = await arte.ProximoDomingoLibre(Reloj.Hoy, C.Id == 0 ? null : C.Id);
    }

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i && !await db.Capitulos.AnyAsync(c => c.Id == i)) return NotFound();
        await Cargar(id);
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(C.Artista)) { Error = "Pon el artista."; await Cargar(C.Id == 0 ? null : C.Id); return Page(); }
        Capitulo c;
        if (C.Id == 0) { c = new Capitulo(); db.Capitulos.Add(c); }
        else c = await db.Capitulos.FindAsync(C.Id) ?? throw new InvalidOperationException();
        c.Artista = C.Artista.Trim(); c.Titulo = string.IsNullOrWhiteSpace(C.Titulo) ? c.Artista : C.Titulo.Trim();
        c.Fechas = L(C.Fechas); c.Entradilla = L(C.Entradilla); c.Texto = (C.Texto ?? "").Trim(); c.Activo = C.Activo;
        c.PortadaObraId = C.PortadaObraId;
        c.Publicacion = C.Publicacion is DateOnly p ? ArteService.DomingoDe(p) : null;
        await db.SaveChangesAsync();
        return Redirect($"/Admin/Arte/Capitulo/{c.Id}");
    }

    /// <summary>"Está listo": sale el próximo domingo libre (las obras sueltas de la cola se apartan una semana).</summary>
    public async Task<IActionResult> OnPostPublicar(int id)
    {
        var c = await db.Capitulos.FindAsync(id);
        if (c != null) { c.Publicacion = await arte.ProximoDomingoLibre(Reloj.Hoy, id); await db.SaveChangesAsync(); }
        return Redirect($"/Admin/Arte/Capitulo/{id}");
    }

    public async Task<IActionResult> OnPostQuitarFecha(int id)
    {
        var c = await db.Capitulos.FindAsync(id);
        if (c != null && !(c.Publicacion <= Reloj.Hoy)) { c.Publicacion = null; await db.SaveChangesAsync(); }
        return Redirect($"/Admin/Arte/Capitulo/{id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var c = await db.Capitulos.FindAsync(id);
        if (c != null) { db.Capitulos.Remove(c); await db.SaveChangesAsync(); } // sus obras quedan como sueltas (inactivas no: se respetan)
        return Redirect("/Admin/Arte");
    }

    static string? L(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
