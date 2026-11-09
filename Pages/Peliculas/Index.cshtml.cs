using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Peliculas;

public class IndexModel(AppDb db, AgenteService agente, FileStorage files) : PageModel
{
    public List<Pelicula> Pendientes { get; set; } = new();
    public List<Pelicula> Vistas { get; set; } = new();
    /// <summary>Películas cuya ficha está preparando el agente ahora mismo.</summary>
    public HashSet<int> EnMarcha { get; set; } = new();
    public bool Agente => agente.Disponible;

    public async Task OnGet()
    {
        var todas = await db.Peliculas.OrderByDescending(p => p.Anadida).ToListAsync();
        Pendientes = todas.Where(p => !p.Vista).ToList();
        Vistas = todas.Where(p => p.Vista).OrderByDescending(p => p.VistaEl).ToList();
        EnMarcha = (await db.TareasAgente.Where(t => t.PeliculaId != null && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso)).Select(t => t.PeliculaId!.Value).ToListAsync()).ToHashSet();
    }

    public async Task<IActionResult> OnPostAnadir(string titulo, string? nota)
    {
        titulo = (titulo ?? "").Trim();
        if (titulo != "")
        {
            var autor = User.Identity?.Name ?? (User.EsAdmin() ? "Javi" : "Raquel");
            var p = new Pelicula { Titulo = titulo, Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(), Autor = autor };
            db.Peliculas.Add(p);
            await db.SaveChangesAsync();
            // El agente prepara la mini ficha en segundo plano
            if (agente.Disponible)
                await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.FichaPelicula, Entrada = titulo, Notas = p.Nota, PeliculaId = p.Id, Autor = autor });
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVista(int id)
    {
        var p = await db.Peliculas.FindAsync(id);
        if (p != null) { p.Vista = !p.Vista; p.VistaEl = p.Vista ? DateTime.UtcNow : null; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var p = await db.Peliculas.FindAsync(id);
        if (p != null) { files.Borrar(p.PosterPath); db.Peliculas.Remove(p); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetPendientes() =>
        new JsonResult(new { enMarcha = await db.TareasAgente.AnyAsync(t => t.PeliculaId != null && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso)) });
}
