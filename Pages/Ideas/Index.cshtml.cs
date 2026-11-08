using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Ideas;

public class IndexModel(AppDb db) : PageModel
{
    public List<Idea> Pendientes { get; set; } = new();
    public List<Idea> Hechas { get; set; } = new();

    public async Task OnGet()
    {
        var todas = await db.Ideas.OrderByDescending(i => i.Creada).ToListAsync();
        Pendientes = todas.Where(i => !i.Hecha).ToList();
        Hechas = todas.Where(i => i.Hecha).OrderByDescending(i => i.HechaEl).ToList();
    }

    public async Task<IActionResult> OnPostAnadir(string texto, string? nota)
    {
        texto = (texto ?? "").Trim();
        if (texto != "")
        {
            db.Ideas.Add(new Idea { Texto = texto, Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(), Autor = User.Identity?.Name ?? (User.EsAdmin() ? "Javi" : "Raquel") });
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHecha(int id)
    {
        var i = await db.Ideas.FindAsync(id);
        if (i != null) { i.Hecha = !i.Hecha; i.HechaEl = i.Hecha ? DateTime.UtcNow : null; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var i = await db.Ideas.FindAsync(id);
        if (i != null) { db.Ideas.Remove(i); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
