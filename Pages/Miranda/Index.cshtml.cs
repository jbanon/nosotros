using System.Security.Claims;
using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Miranda;

/// <summary>La página de Miranda: su frase del día (de papá) y los juegos. Javi la ve con ?fecha= y puede cambiar la frase.</summary>
public class IndexModel(MirandaService miranda, JuegosDiarios juegos, AppDb db) : PageModel
{
    public DateOnly Fecha { get; set; }
    public string Frase { get; set; } = "";
    public string Saludo { get; set; } = "";
    public Tema Tema { get; set; } = null!;
    public int? Cumple { get; set; }
    public bool Preparando { get; set; }
    public List<(JuegosDiarios.Juego j, bool jugado)> Diarios { get; set; } = new();
    public bool EsAdmin => User.EsAdmin();

    public async Task OnGet(DateOnly? fecha)
    {
        Fecha = fecha is DateOnly f && EsAdmin ? f : Reloj.Hoy;
        Frase = await miranda.FraseDe(Fecha);
        Preparando = EsAdmin && await miranda.Preparando();
        var nac = MirandaService.Nacimiento;
        if (Fecha.Month == nac.Month && Fecha.Day == nac.Day) Cumple = Fecha.Year - nac.Year;
        Tema = Cumple != null ? TemaService.PorClave("cumple") : TemaService.PorFecha(Fecha);
        ViewData["Tema"] = Tema.Clave;
        var h = Reloj.Ahora.Hour;
        Saludo = h < 6 ? "Buenas noches" : h < 13 ? "Buenos días" : h < 21 ? "Buenas tardes" : "Buenas noches";

        var yo = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        foreach (var j in JuegosDiarios.Todos)
        {
            if (await juegos.BloqueadoHasta(j, Fecha) != null) continue;
            var clave = j.Clave; var dia = Fecha;
            Diarios.Add((j, await db.JuegoPartidas.AnyAsync(p => p.UsuarioId == yo && p.Juego == clave && p.Fecha == dia)));
        }
    }

    public async Task<IActionResult> OnPostGuardar(DateOnly fecha, string? frase)
    {
        if (!EsAdmin) return Forbid();
        await miranda.Guardar(fecha, frase);
        return Redirect($"/Miranda?fecha={fecha:yyyy-MM-dd}");
    }
}
