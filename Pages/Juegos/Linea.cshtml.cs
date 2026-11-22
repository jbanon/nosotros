using System.Security.Claims;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Juegos;

/// <summary>Línea del tiempo del arte: 5 obras a ordenar en el tiempo, con BriskaBird pintora de presentadora. Una por semana.</summary>
public class LineaModel(LineaArte linea, AppDb db) : PageModel
{
    public DateOnly LunesSemana { get; set; }
    public List<LineaArte.Carta> Cartas { get; set; } = new();
    public JuegoPartida? Mia { get; set; }
    /// <summary>Resultados de la semana de los demás (nombre, puntos), si ya han jugado.</summary>
    public List<(string Nombre, int Puntos)> Otros { get; set; } = new();
    public bool EsAdmin => User.EsAdmin();
    /// <summary>Javi y los niños juegan las veces que quieran.</summary>
    public bool SinLimite => EsAdmin || User.EsNina();
    public bool YaJugada => Mia?.Fin != null && !SinLimite;
    int Yo => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    async Task Cargar()
    {
        LunesSemana = LineaArte.Lunes(Reloj.Hoy);
        Cartas = linea.Semana(LunesSemana);
        Mia = await linea.DeSemana(Yo, LunesSemana);
        var l = LunesSemana;
        var otras = await db.JuegoPartidas.Where(p => p.Juego == LineaArte.Clave && p.Fecha == l && p.UsuarioId != Yo && p.Fin != null).ToListAsync();
        var nombres = await db.Usuarios.ToDictionaryAsync(u => u.Id, u => u.Nombre);
        Otros = otras.GroupBy(p => p.UsuarioId).Select(g => (nombres.GetValueOrDefault(g.Key, "?"), g.OrderByDescending(p => p.Id).First().Palabras)).ToList();
    }

    public async Task<IActionResult> OnGet()
    {
        await Cargar();
        return Page();
    }

    public async Task<IActionResult> OnPostEmpezar()
    {
        await Cargar();
        if (Mia != null && !SinLimite) return new JsonResult(new { ok = false, mensaje = "Ya has jugado esta semana. ¡El lunes, obras nuevas!" });
        db.JuegoPartidas.Add(new JuegoPartida { UsuarioId = Yo, Juego = LineaArte.Clave, Fecha = LunesSemana, Total = LineaArte.Maximo });
        await db.SaveChangesAsync();
        return new JsonResult(new { ok = true });
    }

    public record Resultado(int Puntos);

    public async Task<IActionResult> OnPostTerminar([FromBody] Resultado r)
    {
        await Cargar();
        if (Mia != null && Mia.Fin == null)
        {
            Mia.Fin = DateTime.UtcNow; Mia.Palabras = Math.Clamp(r.Puntos, 0, LineaArte.Maximo); Mia.Gano = Mia.Palabras == LineaArte.Maximo;
            await db.SaveChangesAsync();
        }
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnGetImagen(string id, CancellationToken ct)
    {
        var ruta = await linea.Imagen(id, ct);
        if (ruta == null) return NotFound();
        Response.Headers.CacheControl = "private, max-age=604800";
        return PhysicalFile(ruta, ruta.EndsWith(".png") ? "image/png" : "image/jpeg");
    }
}
