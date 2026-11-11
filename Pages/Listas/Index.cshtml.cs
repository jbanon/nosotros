using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Listas;

public class IndexModel(AppDb db) : PageModel
{
    public List<Lista> Listas { get; set; } = new();

    public async Task OnGet() =>
        Listas = await db.Listas.Include(l => l.Elementos).OrderBy(l => l.Nombre).ToListAsync();

    public async Task<IActionResult> OnPostCrear(string? nombre, string? emoji)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre == "") return RedirectToPage();
        var l = new Lista { Nombre = nombre, Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim() };
        db.Listas.Add(l);
        await db.SaveChangesAsync();
        return Redirect($"/Listas/Ver/{l.Id}?foco=1");
    }
}
