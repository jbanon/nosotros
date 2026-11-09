using System.Text.Json.Nodes;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Menu.Recetas;

/// <summary>Pedir una receta al agente (por nombre o desde una foto), revisarla en conversación y guardarla.</summary>
public class AgenteModel(AppDb db, AgenteService agente, FileStorage files) : PageModel
{
    public TareaAgente? Tarea { get; set; }
    public JsonObject? Propuesta { get; set; }
    public JsonArray Historial { get; set; } = new();
    public List<TareaAgente> Recientes { get; set; } = new();
    public bool Disponible => agente.Disponible;
    public string? Error { get; set; }
    string Autor => User.Identity?.Name ?? (User.EsAdmin() ? "Javi" : "Raquel");

    static bool EsReceta(TareaAgente t) => t.Tipo is TipoTareaAgente.Receta or TipoTareaAgente.RecetaFoto;

    async Task<TareaAgente?> Buscar(int id)
    {
        var t = await db.TareasAgente.FindAsync(id);
        return t != null && EsReceta(t) ? t : null;
    }

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i)
        {
            Tarea = await Buscar(i);
            if (Tarea == null) return NotFound();
            Propuesta = AgenteService.Propuesta(Tarea);
            Historial = AgenteService.Historial(Tarea);
        }
        else
        {
            var desde = DateTime.UtcNow.AddDays(-14);
            Recientes = await db.TareasAgente.Where(t => (t.Tipo == TipoTareaAgente.Receta || t.Tipo == TipoTareaAgente.RecetaFoto) && t.Aplicada == null && t.Creada > desde)
                .OrderByDescending(t => t.Creada).Take(10).ToListAsync();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostCrear(string? nombre, string? notas, IFormFile? foto)
    {
        nombre = (nombre ?? "").Trim(); notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (nombre == "" && (foto == null || foto.Length == 0)) { Error = "Escribe el nombre de la receta o sube una foto."; return await OnGet(null); }
        var t = new TareaAgente { Autor = Autor, Notas = notas };
        if (foto != null && foto.Length > 0)
        {
            (t.FotoPath, _) = await files.Guardar(foto, "agente");
            t.Tipo = TipoTareaAgente.RecetaFoto;
            t.Entrada = nombre != "" ? nombre : "Receta de la foto";
        }
        else { t.Tipo = TipoTareaAgente.Receta; t.Entrada = nombre; }
        await agente.Encolar(db, t);
        return Redirect($"/Menu/Recetas/Agente/{t.Id}");
    }

    public async Task<IActionResult> OnPostCambiar(int id, string texto)
    {
        var t = await Buscar(id);
        if (t == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(texto) && t.Estado is EstadoTarea.Lista or EstadoTarea.Error && t.Aplicada == null)
            await agente.Cambiar(db, t, texto);
        return Redirect($"/Menu/Recetas/Agente/{id}");
    }

    public async Task<IActionResult> OnPostReintentar(int id)
    {
        var t = await Buscar(id);
        if (t == null) return NotFound();
        if (t.Estado == EstadoTarea.Error) await agente.Reintentar(db, t);
        return Redirect($"/Menu/Recetas/Agente/{id}");
    }

    public async Task<IActionResult> OnPostGuardar(int id)
    {
        var t = await Buscar(id);
        if (t == null) return NotFound();
        if (t.RecetaId != null) return Redirect($"/Menu/Recetas/Ver/{t.RecetaId}");
        var p = AgenteService.Propuesta(t);
        if (t.Estado != EstadoTarea.Lista || p == null) return Redirect($"/Menu/Recetas/Agente/{id}");
        var r = await AgenteService.CrearReceta(db, p);
        t.RecetaId = r.Id; t.Aplicada = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Redirect($"/Menu/Recetas/Ver/{r.Id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var t = await Buscar(id);
        if (t != null && t.Estado != EstadoTarea.EnCurso) { files.Borrar(t.FotoPath); db.TareasAgente.Remove(t); await db.SaveChangesAsync(); }
        return Redirect("/Menu/Recetas/Agente");
    }

    public async Task<IActionResult> OnGetEstado(int id)
    {
        var t = await Buscar(id);
        return t == null ? NotFound() : new JsonResult(new { estado = t.Estado.ToString(), error = t.Error });
    }
}
