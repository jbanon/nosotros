using System.Security.Claims;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Juegos;

/// <summary>Partida diaria de un juego del adviento (globos, bloques, comecocos) en la zona Juegos.
/// Una al día por juego y persona (el admin, sin límite para probar), con la frase del día.</summary>
public class PartidaModel(JuegosDiarios juegos, AppDb db) : PageModel
{
    public JuegosDiarios.Juego Juego { get; set; } = null!;
    public TipoCasilla Tipo { get; set; }
    public string Frase { get; set; } = "";
    public DateOnly? Bloqueado { get; set; }
    public JuegoPartida? Hoy { get; set; }
    public bool EsAdmin => User.EsAdmin();
    /// <summary>Ya ha gastado la partida de hoy (al admin nunca se le acaba).</summary>
    public bool YaJugada => Hoy != null && !EsAdmin && !Libre;
    /// <summary>Ya jugó la frase que puso Javi para hoy: puede seguir con frases sorpresa, sin límite.</summary>
    public bool Libre { get; set; }
    /// <summary>La frase de hoy la ha puesto Javi.</summary>
    public bool Propia { get; set; }
    int Yo => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    async Task<bool> Cargar(string juego)
    {
        var j = JuegosDiarios.Buscar(juego);
        if (j == null || !Enum.TryParse<TipoCasilla>(j.Tipo, out var tipo)) return false;
        Juego = j; Tipo = tipo;
        Frase = await juegos.FraseDe(j, Reloj.Hoy);
        Bloqueado = EsAdmin ? null : await juegos.BloqueadoHasta(j, Reloj.Hoy);
        Hoy = await juegos.DeHoy(Yo, j, Reloj.Hoy);
        Propia = await juegos.EsPropia(j, Reloj.Hoy);
        if (Hoy != null && !EsAdmin && Propia)
        {
            Libre = true;
            var hoy = Reloj.Hoy; var clave = j.Clave; var yo = Yo;
            var n = await db.JuegoPartidas.CountAsync(p => p.UsuarioId == yo && p.Juego == clave && p.Fecha == hoy);
            Frase = JuegosDiarios.FraseSorpresa(j, hoy, n, Frase);
        }
        return true;
    }

    public async Task<IActionResult> OnGet(string juego)
    {
        if (!await Cargar(juego)) return NotFound();
        ViewData["Tema"] = "navidad";
        return Page();
    }

    public async Task<IActionResult> OnPostEmpezar(string juego)
    {
        if (!await Cargar(juego)) return NotFound();
        if (Bloqueado != null) return new JsonResult(new { ok = false, mensaje = "Este juego aún no se ha desbloqueado." });
        if (YaJugada) return new JsonResult(new { ok = false, mensaje = "Ya has jugado hoy. ¡Mañana más!" });
        var n = Frase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        db.JuegoPartidas.Add(new JuegoPartida { UsuarioId = Yo, Juego = Juego.Clave, Fecha = Reloj.Hoy, Total = n });
        await db.SaveChangesAsync();
        return new JsonResult(new { ok = true });
    }

    public record Resultado(bool Gano, int Palabras);

    public async Task<IActionResult> OnPostTerminar(string juego, [FromBody] Resultado r)
    {
        if (!await Cargar(juego)) return NotFound();
        if (Hoy != null && Hoy.Fin == null)
        {
            Hoy.Fin = DateTime.UtcNow; Hoy.Gano = r.Gano; Hoy.Palabras = Math.Clamp(r.Palabras, 0, Hoy.Total);
            await db.SaveChangesAsync();
        }
        return new JsonResult(new { ok = true });
    }
}
