using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Menu.Recetas;

public class IndexModel(AppDb db, AgenteService agente) : PageModel
{
    public List<Receta> Lista { get; set; } = new();
    public bool Agente => agente.Disponible;
    public string? Q { get; set; }

    public async Task OnGet(string? q)
    {
        Q = q?.Trim();
        var query = db.Recetas.Include(r => r.Ingredientes).ThenInclude(i => i.Ingrediente).AsQueryable();
        if (!string.IsNullOrEmpty(Q))
            query = query.Where(r => EF.Functions.Like(r.Nombre, $"%{Q}%") || r.Ingredientes.Any(i => EF.Functions.Like(i.Ingrediente.Nombre, $"%{Q}%")));
        Lista = await query.OrderBy(r => r.Nombre).ToListAsync();
    }
}
