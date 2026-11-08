using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Lecturas;

public class IndexModel(AppDb db) : PageModel
{
    public List<Lectura> Lecturas { get; set; } = new();
    public async Task OnGet() => Lecturas = await db.Lecturas.OrderByDescending(l => l.Creada).ToListAsync();
}
