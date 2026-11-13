using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Arte;

/// <summary>Un capítulo: el artista explicado (texto por secciones) y sus obras, con paseo o solo con reseña.</summary>
public class CapituloModel(AppDb db, ArteService arte) : PageModel
{
    public Capitulo C { get; set; } = null!;
    public DateOnly? Fecha { get; set; }
    /// <summary>Texto ya convertido a HTML seguro y el índice de secciones (id del ancla, título).</summary>
    public string TextoHtml { get; set; } = "";
    public List<(string id, string titulo)> Secciones { get; set; } = new();
    /// <summary>Obras con paseo por los detalles (tienen paradas), en su orden.</summary>
    public List<Obra> ConPaseo { get; set; } = new();
    /// <summary>Obras solo con reseña (sin paradas): la reseña es Obra.Intro.</summary>
    public List<Obra> ParaMirar { get; set; } = new();
    public Obra? Portada { get; set; }
    public int MinutosLectura { get; set; }
    /// <summary>Número de orden del capítulo según su domingo (el primero publicado es el 1).</summary>
    public int Numero { get; set; } = 1;

    public async Task<IActionResult> OnGet(int id)
    {
        var admin = User.EsAdmin();
        if (!await arte.PuedeVerCapitulo(id, Reloj.Hoy, admin)) return Redirect("/Arte");
        var c = await db.Capitulos.Include(x => x.Obras.Where(o => o.Activa)).ThenInclude(o => o.Paradas).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return Redirect("/Arte");
        C = c;
        var cal = await arte.CalendarioCapitulos();
        Fecha = cal.TryGetValue(id, out var f) ? f : null;
        Numero = Fecha is DateOnly fd ? cal.Values.Count(x => x < fd) + 1 : cal.Count + 1;
        TextoHtml = ArteService.TextoHtml(c.Texto, out var secs);
        Secciones = secs;
        var obras = c.Obras.OrderBy(o => o.Orden).ThenBy(o => o.Id).ToList();
        ConPaseo = obras.Where(o => o.Paradas.Count > 0).ToList();
        ParaMirar = obras.Where(o => o.Paradas.Count == 0).ToList();
        Portada = obras.FirstOrDefault(o => o.Id == c.PortadaObraId) ?? ConPaseo.FirstOrDefault() ?? obras.FirstOrDefault();
        var palabras = (c.Entradilla + " " + c.Texto).Split((char[])[' ', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        MinutosLectura = Math.Max(1, (int)Math.Round(palabras / 200.0));
        if (!admin && c.VistoEl == null) { c.VistoEl = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return Page();
    }
}
