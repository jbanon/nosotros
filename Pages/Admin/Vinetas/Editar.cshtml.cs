using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Vinetas;

public class EditarModel(AppDb db, FileStorage files, AgenteService agente, VinetaService vinetas) : PageModel
{
    [BindProperty] public Vineta V { get; set; } = new();
    [BindProperty] public IFormFile? Imagen { get; set; }
    [BindProperty] public bool Analizar { get; set; }
    public bool Nueva => V.Id == 0;
    public bool Agente => agente.Disponible;
    public TareaAgente? Tarea { get; set; }
    public List<JuegoVineta> Viables { get; set; } = new();
    public List<VinetaDia> Dias { get; set; } = new();
    public string? Error { get; set; }
    public static string[] Taxonomia => VinetaService.Taxonomia;

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i)
        {
            var v = await db.Vinetas.Include(x => x.Dias).FirstOrDefaultAsync(x => x.Id == i);
            if (v == null) return NotFound();
            V = v; Dias = v.Dias.OrderByDescending(d => d.Fecha).ToList();
            Tarea = await db.TareasAgente.Where(t => t.VinetaId == i).OrderByDescending(t => t.Creada).FirstOrDefaultAsync();
            Viables = await vinetas.JuegosViables(v);
        }
        else Analizar = true;
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        Vineta v;
        if (V.Id == 0)
        {
            if (Imagen == null || Imagen.Length == 0) { Error = "Sube la imagen de la viñeta."; return Page(); }
            v = new Vineta(); db.Vinetas.Add(v);
        }
        else v = await db.Vinetas.FindAsync(V.Id) ?? throw new InvalidOperationException();
        v.Autor = string.IsNullOrWhiteSpace(V.Autor) ? "72 Kilos" : V.Autor.Trim();
        v.Libro = V.Libro?.Trim(); v.FraseAutor = Limpia(V.FraseAutor); v.FraseMia = Limpia(V.FraseMia);
        v.Intenciones = Limpia(V.Intenciones); v.Tono = Limpia(V.Tono); v.Momentos = Limpia(V.Momentos); v.Descripcion = Limpia(V.Descripcion);
        v.JuegoSugerido = V.JuegoSugerido; v.EspacioFrase = V.EspacioFrase; v.Conocida = V.Conocida; v.Activa = V.Activa;
        v.UsoPortada = V.UsoPortada; v.UsoJuego = V.UsoJuego; v.FechaFija = V.FechaFija; v.Pregunta = Limpia(V.Pregunta); v.Respuesta = Limpia(V.Respuesta);
        if (Imagen != null && Imagen.Length > 0)
        {
            files.Borrar(v.Path);
            (v.Path, _) = await files.Guardar(Imagen, "vinetas");
        }
        await db.SaveChangesAsync();
        if (Analizar && agente.Disponible)
            await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.Vineta, Entrada = v.Libro ?? "viñeta", VinetaId = v.Id, Autor = User.Identity?.Name ?? "Javi" });
        return Redirect($"/Admin/Vinetas/Editar/{v.Id}");
    }

    static string? Limpia(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public async Task<IActionResult> OnPostAnalizar(int id)
    {
        var v = await db.Vinetas.FindAsync(id);
        if (v == null) return NotFound();
        if (agente.Disponible)
            await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.Vineta, Entrada = v.Libro ?? "viñeta", VinetaId = v.Id, Autor = User.Identity?.Name ?? "Javi" });
        return Redirect($"/Admin/Vinetas/Editar/{id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var v = await db.Vinetas.FindAsync(id);
        if (v != null) { files.Borrar(v.Path); db.Vinetas.Remove(v); await db.SaveChangesAsync(); }
        return Redirect("/Admin/Vinetas");
    }

    public async Task<IActionResult> OnGetEstado(int id)
    {
        var t = await db.TareasAgente.Where(x => x.VinetaId == id).OrderByDescending(x => x.Creada).FirstOrDefaultAsync();
        return new JsonResult(new { estado = t?.Estado.ToString() ?? "Ninguna" });
    }
}
