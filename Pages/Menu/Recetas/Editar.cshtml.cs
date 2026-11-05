using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Love.Pages.Menu.Recetas;

public class EditarModel(AppDb db, FileStorage files) : PageModel
{
    [BindProperty] public Receta Receta { get; set; } = new();
    [BindProperty] public List<string> IngNombre { get; set; } = new();
    [BindProperty] public List<string> IngCantidad { get; set; } = new();
    [BindProperty] public List<string> IngUnidad { get; set; } = new();
    [BindProperty] public List<Zona> IngZona { get; set; } = new();
    [BindProperty] public IFormFile? Foto { get; set; }
    public List<Ingrediente> Maestro { get; set; } = new();
    public bool Nueva => Receta.Id == 0;

    public async Task<IActionResult> OnGet(int? id)
    {
        Maestro = await db.Ingredientes.OrderBy(i => i.Nombre).ToListAsync();
        if (id is int i)
        {
            var r = await db.Recetas.Include(x => x.Ingredientes.OrderBy(x => x.Orden)).ThenInclude(x => x.Ingrediente).FirstOrDefaultAsync(x => x.Id == i);
            if (r == null) return NotFound();
            Receta = r;
            foreach (var ri in r.Ingredientes)
            {
                IngNombre.Add(ri.Ingrediente.Nombre);
                IngCantidad.Add(ri.Cantidad?.ToString("0.##", CultureInfo.InvariantCulture) ?? "");
                IngUnidad.Add(ri.Unidad ?? "");
                IngZona.Add(ri.Ingrediente.Zona);
            }
        }
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        Maestro = await db.Ingredientes.OrderBy(i => i.Nombre).ToListAsync();
        if (string.IsNullOrWhiteSpace(Receta.Nombre)) { ModelState.AddModelError("Receta.Nombre", "Ponle un nombre"); return Page(); }

        Receta r;
        if (Receta.Id == 0) { r = new Receta(); db.Recetas.Add(r); }
        else
        {
            r = await db.Recetas.Include(x => x.Ingredientes).FirstAsync(x => x.Id == Receta.Id);
            db.RecetaIngredientes.RemoveRange(r.Ingredientes);
            r.Ingredientes.Clear();
        }
        r.Nombre = Receta.Nombre.Trim();
        r.Descripcion = Receta.Descripcion?.Trim();
        r.Raciones = Math.Max(1, Receta.Raciones);
        r.Etiquetas = Receta.Etiquetas?.Trim();
        r.Preparacion = Receta.Preparacion?.Trim();

        if (Foto != null && Foto.Length > 0)
        {
            files.Borrar(r.FotoPath);
            (r.FotoPath, _) = await files.Guardar(Foto, "recetas");
        }

        for (int i = 0; i < IngNombre.Count; i++)
        {
            var nombre = (IngNombre[i] ?? "").Trim();
            if (nombre == "") continue;
            var zona = i < IngZona.Count ? IngZona[i] : Zona.Otros;
            var ing = Maestro.FirstOrDefault(x => string.Equals(x.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            if (ing == null) { ing = new Ingrediente { Nombre = nombre, Zona = zona }; db.Ingredientes.Add(ing); Maestro.Add(ing); }
            else if (ing.Zona != zona) ing.Zona = zona;
            decimal? cant = decimal.TryParse((i < IngCantidad.Count ? IngCantidad[i] : "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var c) ? c : null;
            r.Ingredientes.Add(new RecetaIngrediente { Ingrediente = ing, Cantidad = cant, Unidad = i < IngUnidad.Count ? IngUnidad[i]?.Trim() : null, Orden = i });
        }
        await db.SaveChangesAsync();
        return Redirect($"/Menu/Recetas/Ver/{r.Id}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var r = await db.Recetas.FindAsync(id);
        if (r != null) { files.Borrar(r.FotoPath); db.Recetas.Remove(r); await db.SaveChangesAsync(); }
        return Redirect("/Menu/Recetas");
    }
}
