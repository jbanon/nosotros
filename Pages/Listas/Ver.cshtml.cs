using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Listas;

public class VerModel(AppDb db) : PageModel
{
    public Lista Lista { get; set; } = null!;
    public bool Foco { get; set; }
    bool EsFetch => Request.Headers.XRequestedWith == "fetch";

    async Task<Lista?> Cargar(int id) =>
        await db.Listas.Include(l => l.Elementos.OrderBy(e => e.Orden)).FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IActionResult> OnGet(int id, bool foco)
    {
        var l = await Cargar(id);
        if (l == null) return NotFound();
        Lista = l; Foco = foco;
        return Page();
    }

    /// <summary>Marca o desmarca. Desde la página se llama con fetch (sin recargar ni saltar arriba).</summary>
    public async Task<IActionResult> OnPostMarcar(int id, int elemento)
    {
        var e = await db.ListaElementos.FirstOrDefaultAsync(x => x.Id == elemento && x.ListaId == id);
        if (e != null) { e.Hecho = !e.Hecho; await db.SaveChangesAsync(); }
        if (EsFetch) return new JsonResult(new { hecho = e?.Hecho ?? false });
        return Redirect($"/Listas/Ver/{id}");
    }

    /// <summary>Añade una cosa por línea (se puede pegar una lista entera).</summary>
    public async Task<IActionResult> OnPostAnadir(int id, string? texto)
    {
        var l = await Cargar(id);
        if (l == null) return NotFound();
        var orden = l.Elementos.Count == 0 ? 0 : l.Elementos.Max(e => e.Orden) + 1;
        var existentes = l.Elementos.Select(e => e.Texto.ToLowerInvariant()).ToHashSet();
        foreach (var linea in (texto ?? "").Replace("\r", "").Split('\n'))
        {
            var t = linea.Trim().TrimStart('-', '•', '*').Trim();
            if (t == "" || !existentes.Add(t.ToLowerInvariant())) continue;
            if (t.Length > 200) t = t[..200];
            l.Elementos.Add(new ListaElemento { Texto = char.ToUpper(t[0]) + t[1..], Orden = orden++ });
        }
        await db.SaveChangesAsync();
        return Redirect($"/Listas/Ver/{id}?foco=1#anadir");
    }

    public async Task<IActionResult> OnPostQuitar(int id, int elemento)
    {
        var e = await db.ListaElementos.FirstOrDefaultAsync(x => x.Id == elemento && x.ListaId == id);
        if (e != null) { db.ListaElementos.Remove(e); await db.SaveChangesAsync(); }
        return Redirect($"/Listas/Ver/{id}");
    }

    public async Task<IActionResult> OnPostDesmarcar(int id)
    {
        await db.ListaElementos.Where(e => e.ListaId == id && e.Hecho).ExecuteUpdateAsync(s => s.SetProperty(e => e.Hecho, false));
        return Redirect($"/Listas/Ver/{id}");
    }

    public async Task<IActionResult> OnPostRenombrar(int id, string? nombre, string? emoji)
    {
        var l = await db.Listas.FindAsync(id);
        if (l == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(nombre)) l.Nombre = nombre.Trim();
        l.Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
        await db.SaveChangesAsync();
        return Redirect($"/Listas/Ver/{id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var l = await db.Listas.FindAsync(id);
        if (l != null) { db.Listas.Remove(l); await db.SaveChangesAsync(); }
        return Redirect("/Listas");
    }
}
