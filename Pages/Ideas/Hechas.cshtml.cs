using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Ideas;

public class HechasModel(AppDb db) : PageModel
{
    public List<Idea> Hechas { get; set; } = new();
    public async Task OnGet() => Hechas = await db.Ideas.Where(i => i.Hecha).OrderByDescending(i => i.HechaEl).ToListAsync();
}
