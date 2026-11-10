using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Arte;

/// <summary>"Disfruta la vista": la obra de la semana y el álbum de las anteriores.</summary>
public class IndexModel(ArteService arte) : PageModel
{
    public List<(Obra obra, DateOnly fecha)> Publicadas { get; set; } = new();
    public (Obra obra, DateOnly fecha)? Semana { get; set; }
    public DateOnly? Proxima { get; set; }
    public int Favoritas => Publicadas.Count(p => p.obra.Favorita);

    public async Task OnGet()
    {
        var hoy = Reloj.Hoy;
        Publicadas = await arte.Publicadas(hoy);
        Semana = Publicadas.Cast<(Obra, DateOnly)?>().FirstOrDefault(p => p!.Value.Item2 == ArteService.DomingoDe(hoy));
        var cal = await arte.Calendario();
        Proxima = cal.Values.Where(f => f > hoy).OrderBy(f => f).Cast<DateOnly?>().FirstOrDefault();
    }
}
