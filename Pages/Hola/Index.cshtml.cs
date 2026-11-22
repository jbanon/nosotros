using System.Security.Claims;
using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Hola;

/// <summary>La página de cada niña (Miranda, Aroa): su frase del día y los juegos.
/// Javi la ve con ?nina=aroa&amp;fecha=… y puede cambiar la frase.</summary>
public class IndexModel(NinasService ninas, JuegosDiarios juegos, AppDb db) : PageModel
{
    public Nina Nina { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public string Frase { get; set; } = "";
    public string Saludo { get; set; } = "";
    public Tema Tema { get; set; } = null!;
    public int? Cumple { get; set; }
    public bool Preparando { get; set; }
    public List<(JuegosDiarios.Juego j, bool jugado)> Diarios { get; set; } = new();
    public bool EsAdmin => User.EsAdmin();

    public async Task<IActionResult> OnGet(DateOnly? fecha, string? nina)
    {
        var n = Ninas.De(User) ?? (EsAdmin ? Ninas.PorSlug(nina) ?? Ninas.Miranda : null);
        if (n == null) return Redirect(User.Inicio());
        Nina = n;
        Fecha = fecha is DateOnly f && EsAdmin ? f : Reloj.Hoy;
        Frase = await ninas.FraseDe(n, Fecha);
        Preparando = EsAdmin && await ninas.Preparando(n);
        if (n.EsCumple(Fecha)) Cumple = n.EdadEn(Fecha);
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
        return Page();
    }

    public async Task<IActionResult> OnPostGuardar(string nina, DateOnly fecha, string? frase)
    {
        if (!EsAdmin || Ninas.PorSlug(nina) is not Nina n) return Forbid();
        await ninas.Guardar(n, fecha, frase);
        return Redirect($"/Hola?nina={n.Slug}&fecha={fecha:yyyy-MM-dd}");
    }
}
