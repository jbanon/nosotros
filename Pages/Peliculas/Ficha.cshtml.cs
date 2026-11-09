using System.Text.Json.Nodes;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Peliculas;

/// <summary>Mini ficha de una película hecha por el agente, con conversación para corregirla.</summary>
public class FichaModel(AppDb db, AgenteService agente) : PageModel
{
    public Pelicula Peli { get; set; } = null!;
    public TareaAgente? Tarea { get; set; }
    public JsonArray Historial { get; set; } = new();
    public bool Disponible => agente.Disponible;
    public bool EnMarcha => Tarea != null && Tarea.Estado is EstadoTarea.Pendiente or EstadoTarea.EnCurso;
    string Autor => User.Identity?.Name ?? (User.EsAdmin() ? "Javi" : "Raquel");

    async Task<bool> Cargar(int id)
    {
        var p = await db.Peliculas.FindAsync(id);
        if (p == null) return false;
        Peli = p;
        Tarea = await db.TareasAgente.Where(t => t.PeliculaId == id).OrderByDescending(t => t.Creada).FirstOrDefaultAsync();
        if (Tarea != null) Historial = AgenteService.Historial(Tarea);
        return true;
    }

    public async Task<IActionResult> OnGet(int id) => await Cargar(id) ? Page() : NotFound();

    /// <summary>Pedir un cambio (o la primera ficha si no la hay).</summary>
    public async Task<IActionResult> OnPostCambiar(int id, string? texto)
    {
        if (!await Cargar(id)) return NotFound();
        texto = (texto ?? "").Trim();
        if (EnMarcha || !Disponible) return RedirectToPage(new { id });
        if (Tarea != null && Tarea.Estado == EstadoTarea.Lista && texto != "")
            await agente.Cambiar(db, Tarea, texto);
        else
            await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.FichaPelicula, Entrada = Peli.Titulo, Notas = texto != "" ? texto : Peli.Nota, PeliculaId = Peli.Id, Autor = Autor });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetEstado(int id)
    {
        var t = await db.TareasAgente.Where(x => x.PeliculaId == id).OrderByDescending(x => x.Creada).FirstOrDefaultAsync();
        return new JsonResult(new { estado = t?.Estado.ToString() ?? "Ninguna", error = t?.Error });
    }
}
