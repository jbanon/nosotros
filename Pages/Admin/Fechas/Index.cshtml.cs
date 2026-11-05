using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Fechas;

public class IndexModel(AppDb db) : PageModel
{
    public List<FechaEspecial> Lista { get; set; } = new();
    public async Task OnGet() => Lista = await db.FechasEspeciales.OrderBy(f => f.Mes).ThenBy(f => f.Dia).ToListAsync();
}
