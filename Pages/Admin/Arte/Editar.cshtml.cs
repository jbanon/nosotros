using System.Text.Json;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Arte;

public class EditarModel(AppDb db, ArteService arte, FileStorage files) : PageModel
{
    [BindProperty] public Obra O { get; set; } = new();
    [BindProperty] public IFormFile? Imagen { get; set; }
    /// <summary>Paradas editadas en el navegador, como JSON [{x,y,zoom,titulo,texto}].</summary>
    [BindProperty] public string ParadasJson { get; set; } = "[]";
    public bool Nueva => O.Id == 0;
    public string? Error { get; set; }
    public DateOnly? Fecha { get; set; }

    public record ParadaDto(double X, double Y, double Zoom, string? Titulo, string? Texto);
    static readonly JsonSerializerOptions Opc = new() { PropertyNameCaseInsensitive = true };

    public async Task<IActionResult> OnGet(int? id)
    {
        if (id is int i)
        {
            var o = await db.Obras.Include(x => x.Paradas.OrderBy(p => p.Orden)).FirstOrDefaultAsync(x => x.Id == i);
            if (o == null) return NotFound();
            O = o;
            ParadasJson = AdvientoService.Serializar(o.Paradas.OrderBy(p => p.Orden).Select(p => new { x = p.X, y = p.Y, zoom = p.Zoom, titulo = p.Titulo, texto = p.Texto }));
            Fecha = (await arte.Calendario()).TryGetValue(o.Id, out var f) ? f : null;
        }
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        Obra o;
        if (O.Id == 0)
        {
            if (Imagen == null || Imagen.Length == 0) { Error = "Sube la imagen de la obra."; return Page(); }
            o = new Obra { Orden = (await db.Obras.MaxAsync(x => (int?)x.Orden) ?? 0) + 1 };
            db.Obras.Add(o);
        }
        else o = await db.Obras.Include(x => x.Paradas).FirstOrDefaultAsync(x => x.Id == O.Id) ?? throw new InvalidOperationException();
        if (string.IsNullOrWhiteSpace(O.Titulo)) { Error = "Ponle título."; return Page(); }
        o.Titulo = O.Titulo.Trim(); o.Autor = L(O.Autor); o.Anio = L(O.Anio); o.Museo = L(O.Museo); o.FuenteUrl = L(O.FuenteUrl); o.Licencia = L(O.Licencia);
        o.Intro = L(O.Intro); o.FraseMia = L(O.FraseMia); o.Activa = O.Activa;
        o.Publicacion = O.Publicacion is DateOnly p ? ArteService.DomingoDe(p) : null;
        if (Imagen != null && Imagen.Length > 0)
        {
            await using var s = Imagen.OpenReadStream();
            try
            {
                var (g, r, w, h) = await arte.GuardarImagen(s);
                files.Borrar(o.ImagenPath); files.Borrar(o.ReducidaPath);
                o.ImagenPath = g; o.ReducidaPath = r; o.Ancho = w; o.Alto = h;
            }
            catch (Exception e) { Error = "No se pudo leer la imagen: " + e.Message; return Page(); }
        }
        List<ParadaDto> paradas;
        try { paradas = JsonSerializer.Deserialize<List<ParadaDto>>(string.IsNullOrWhiteSpace(ParadasJson) ? "[]" : ParadasJson, Opc) ?? new(); }
        catch (JsonException) { Error = "Las paradas no se han podido leer."; return Page(); }
        db.ParadasObra.RemoveRange(o.Paradas); o.Paradas.Clear();
        int i = 0;
        foreach (var pd in paradas.Where(x => !string.IsNullOrWhiteSpace(x.Titulo) || !string.IsNullOrWhiteSpace(x.Texto)))
            o.Paradas.Add(new ParadaObra { Orden = i++, X = Math.Clamp(pd.X, 0, 1), Y = Math.Clamp(pd.Y, 0, 1), Zoom = Math.Clamp(pd.Zoom, 1, 12), Titulo = (pd.Titulo ?? "").Trim(), Texto = (pd.Texto ?? "").Trim() });
        await db.SaveChangesAsync();
        return Redirect($"/Admin/Arte/Editar/{o.Id}");
    }

    static string? L(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var o = await db.Obras.FindAsync(id);
        if (o != null) { files.Borrar(o.ImagenPath); files.Borrar(o.ReducidaPath); db.Obras.Remove(o); await db.SaveChangesAsync(); }
        return Redirect("/Admin/Arte");
    }
}
