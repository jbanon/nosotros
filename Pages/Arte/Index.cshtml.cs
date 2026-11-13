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
    /// <summary>Capítulos publicados (del más reciente al más antiguo) y el de esta semana, si toca capítulo.</summary>
    public List<(Capitulo cap, DateOnly fecha)> Capitulos { get; set; } = new();
    public (Capitulo cap, DateOnly fecha)? CapituloSemana { get; set; }
    /// <summary>Imagen de portada de un capítulo: la elegida o la primera obra con paseo.</summary>
    public static Obra? PortadaDe(Capitulo c) =>
        c.Obras.FirstOrDefault(o => o.Id == c.PortadaObraId) ?? c.Obras.Where(o => o.Paradas.Count > 0).OrderBy(o => o.Orden).FirstOrDefault() ?? c.Obras.OrderBy(o => o.Orden).FirstOrDefault();

    public async Task OnGet()
    {
        var hoy = Reloj.Hoy;
        Publicadas = await arte.Publicadas(hoy);
        Semana = Publicadas.Cast<(Obra, DateOnly)?>().FirstOrDefault(p => p!.Value.Item2 == ArteService.DomingoDe(hoy));
        Capitulos = await arte.CapitulosPublicados(hoy);
        CapituloSemana = Capitulos.Cast<(Capitulo, DateOnly)?>().FirstOrDefault(p => p!.Value.Item2 == ArteService.DomingoDe(hoy));
        var cal = (await arte.Calendario()).Values.Concat((await arte.CalendarioCapitulos()).Values);
        Proxima = cal.Where(f => f > hoy).OrderBy(f => f).Cast<DateOnly?>().FirstOrDefault();
    }
}
