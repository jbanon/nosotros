using System.Text.Json;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Adviento;

public class EditarModel(AdvientoService adv, AppDb db, FileStorage files) : PageModel
{
    [BindProperty] public AdvientoCasilla Casilla { get; set; } = new();
    [BindProperty] public IFormFile? Foto { get; set; }
    [BindProperty] public bool QuitarFoto { get; set; }
    public DateOnly Hoy => Reloj.Hoy;
    public List<Vineta> Vinetas { get; set; } = new();

    async Task<AdvientoCasilla?> Buscar(int dia)
    {
        var anio = AdvientoService.AnioDe(Reloj.Hoy);
        return await adv.Casilla(anio, dia) ?? await adv.Casilla(Reloj.Hoy.Year, dia);
    }

    public async Task<IActionResult> OnGet(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        Casilla = c;
        Vinetas = await db.Vinetas.Where(v => v.Activa).OrderBy(v => v.Libro).ThenBy(v => v.FraseAutor).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPost(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(Casilla.Datos))
        {
            try { JsonDocument.Parse(Casilla.Datos); }
            catch (JsonException e) { ModelState.AddModelError("Casilla.Datos", "El JSON no es válido: " + e.Message); Casilla.Id = c.Id; Casilla.Anio = c.Anio; Casilla.Dia = c.Dia; Casilla.FotoPath = c.FotoPath; return Page(); }
        }
        c.Titulo = Casilla.Titulo.Trim();
        c.Intro = string.IsNullOrWhiteSpace(Casilla.Intro) ? null : Casilla.Intro.Trim();
        c.Tipo = Casilla.Tipo;
        c.Letra = string.IsNullOrWhiteSpace(Casilla.Letra) ? null : Casilla.Letra.Trim().ToUpperInvariant()[..1];
        c.Pregunta = string.IsNullOrWhiteSpace(Casilla.Pregunta) ? null : Casilla.Pregunta.Trim();
        c.Respuesta = string.IsNullOrWhiteSpace(Casilla.Respuesta) ? null : Casilla.Respuesta.Trim();
        c.Datos = string.IsNullOrWhiteSpace(Casilla.Datos) ? "{}" : Casilla.Datos.Trim();
        c.VinetaId = Casilla.VinetaId;
        if (QuitarFoto) { files.Borrar(c.FotoPath); c.FotoPath = null; }
        if (Foto != null && Foto.Length > 0)
        {
            files.Borrar(c.FotoPath);
            (c.FotoPath, _) = await files.Guardar(Foto, "adviento");
        }
        await db.SaveChangesAsync();
        return Redirect("/Admin/Adviento");
    }

    public async Task<IActionResult> OnPostReiniciar(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        c.AbiertaEl = null; c.ResueltaEl = null; c.CanjeadoEl = null; c.Estado = null; c.Pistas = 0;
        await db.SaveChangesAsync();
        return Redirect($"/Admin/Adviento/Editar/{dia}");
    }
}
