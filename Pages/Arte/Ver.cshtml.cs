using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Arte;

public class RespuestaArte { public string? Texto { get; set; } }

/// <summary>El visor: obra entera, recorrido por las paradas y vuelta a la vista completa con la frase de Javi.</summary>
public class VerModel(AppDb db, ArteService arte, ConfigService config) : PageModel
{
    public Obra O { get; set; } = null!;
    public string DatosJson { get; set; } = "{}";
    bool EsAdmin => User.EsAdmin();

    async Task<bool> Cargar(int id)
    {
        if (!await arte.PuedeVer(id, Reloj.Hoy, EsAdmin)) return false;
        var o = await db.Obras.Include(x => x.Paradas.OrderBy(p => p.Orden)).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return false;
        O = o; return true;
    }

    public async Task<IActionResult> OnGet(int id)
    {
        if (!await Cargar(id)) return Redirect("/Arte");
        DatosJson = AdvientoService.Serializar(new
        {
            id = O.Id, titulo = O.Titulo, autor = O.Autor, anio = O.Anio, museo = O.Museo, intro = O.Intro, frase = O.FraseMia,
            firma = await config.Get(ConfigClaves.Firma, "Javier"), ancho = O.Ancho, alto = O.Alto,
            reducida = $"/Arte/Imagen/{O.Id}?t=r", grande = $"/Arte/Imagen/{O.Id}?t=g",
            paradas = O.Paradas.OrderBy(p => p.Orden).Select(p => new { x = p.X, y = p.Y, zoom = p.Zoom, titulo = p.Titulo, texto = p.Texto }),
            favorita = O.Favorita, respuesta = O.RespuestaRaquel, admin = EsAdmin,
        });
        ViewData["SinNav"] = true;
        return Page();
    }

    public async Task<IActionResult> OnPostVista(int id)
    {
        if (!await Cargar(id)) return NotFound();
        if (!EsAdmin && O.VistaEl == null) { O.VistaEl = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostFavorita(int id)
    {
        if (!await Cargar(id)) return NotFound();
        if (!EsAdmin) { O.Favorita = !O.Favorita; await db.SaveChangesAsync(); }
        return new JsonResult(new { favorita = EsAdmin ? !O.Favorita : O.Favorita });
    }

    public async Task<IActionResult> OnPostResponder(int id, [FromBody] RespuestaArte r)
    {
        if (!await Cargar(id)) return NotFound();
        var texto = (r.Texto ?? "").Trim();
        if (!EsAdmin)
        {
            O.RespuestaRaquel = texto == "" ? null : texto; O.RespondidaEl = texto == "" ? null : DateTime.UtcNow; O.RespuestaVista = false;
            await db.SaveChangesAsync();
        }
        return new JsonResult(new { ok = true });
    }
}
