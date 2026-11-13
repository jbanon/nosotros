using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Mensajes;

public class IndexModel(AppDb db) : PageModel
{
    public int Mes { get; set; }
    public string? Q { get; set; }
    public List<Mensaje> Lista { get; set; } = new();
    public static readonly string[] Meses = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    public async Task OnGet(int? mes, string? q)
    {
        Q = q?.Trim();
        Mes = mes ?? Reloj.Hoy.Month;
        var query = db.Mensajes.AsQueryable();
        query = string.IsNullOrEmpty(Q) ? query.Where(m => m.Mes == Mes) : query.Where(m => EF.Functions.ILike(m.Texto, $"%{Q}%"));
        Lista = await query.OrderBy(m => m.Mes).ThenBy(m => m.Dia).ToListAsync();
    }
}
