using System.Security.Claims;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Juegos;

/// <summary>Álbum de la Línea del tiempo: las obras de las semanas jugadas, con año, autor y un texto breve.</summary>
public class LineaAlbumModel(LineaArte linea) : PageModel
{
    public List<(LineaArte.Obra Obra, DateOnly Semana)> Obras { get; set; } = new();

    public async Task OnGet()
    {
        Obras = await linea.Album(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.EsAdmin());
    }
}
