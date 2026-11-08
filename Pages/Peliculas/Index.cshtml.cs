using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Peliculas;

public class IndexModel(AppDb db) : PageModel
{
    public List<Pelicula> Pendientes { get; set; } = new();
    public List<Pelicula> Vistas { get; set; } = new();

    public async Task OnGet()
    {
        var todas = await db.Peliculas.OrderByDescending(p => p.Anadida).ToListAsync();
        Pendientes = todas.Where(p => !p.Vista).ToList();
        Vistas = todas.Where(p => p.Vista).OrderByDescending(p => p.VistaEl).ToList();
    }

    public async Task<IActionResult> OnPostAnadir(string titulo, string? nota)
    {
        titulo = (titulo ?? "").Trim();
        if (titulo != "")
        {
            db.Peliculas.Add(new Pelicula { Titulo = titulo, Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(), Autor = User.EsAdmin() ? "Javier" : "Raquel" });
            await db.SaveChangesAsync();
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
        if (p != null) { db.Peliculas.Remove(p); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
