using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Love.Pages.Gastos;

public class EditarModel(AppDb db, FileStorage files, ConfigService config) : PageModel
{
    [BindProperty] public Gasto Gasto { get; set; } = new();
    [BindProperty] public string ImporteTexto { get; set; } = "";
    [BindProperty] public IFormFile? Adjunto { get; set; }
    [BindProperty] public bool QuitarAdjunto { get; set; }
    public decimal ImporteVeva { get; set; }
    public decimal ImporteHipoteca { get; set; }
    public bool Nuevo => Gasto.Id == 0;
    public string? Error { get; set; }

    async Task Cargar()
    {
        ImporteVeva = await config.GetDecimal(ConfigClaves.ImporteVeva, 300);
        ImporteHipoteca = await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900);
    }

    public async Task<IActionResult> OnGet(int? id, DateOnly? fecha)
    {
        await Cargar();
        if (id is int i)
        {
            var g = await db.Gastos.FindAsync(i);
            if (g == null) return NotFound();
            Gasto = g;
            ImporteTexto = g.Importe.ToString("0.##", CultureInfo.InvariantCulture);
        }
        else Gasto = new Gasto { Fecha = fecha ?? Reloj.Hoy, Tipo = TipoGasto.Compra };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        await Cargar();
        if (!decimal.TryParse(ImporteTexto.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var importe) || importe <= 0)
        { Error = "Pon un importe válido"; return Page(); }

        Gasto g;
        if (Gasto.Id == 0) { g = new Gasto(); db.Gastos.Add(g); }
        else g = await db.Gastos.FindAsync(Gasto.Id) ?? throw new InvalidOperationException();
        g.Fecha = Gasto.Fecha; g.Tipo = Gasto.Tipo; g.Importe = importe; g.Nota = Gasto.Nota?.Trim();

        if (QuitarAdjunto) { files.Borrar(g.AdjuntoPath); g.AdjuntoPath = null; g.AdjuntoNombre = null; g.AdjuntoMime = null; }
        if (Adjunto != null && Adjunto.Length > 0)
        {
            files.Borrar(g.AdjuntoPath);
            (g.AdjuntoPath, g.AdjuntoMime) = await files.Guardar(Adjunto, $"gastos/{g.Fecha.Year}/{g.Fecha.Month:00}");
            g.AdjuntoNombre = Path.GetFileName(Adjunto.FileName);
        }
        await db.SaveChangesAsync();
        return Redirect($"/Gastos?anio={g.Fecha.Year}&mes={g.Fecha.Month}");
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        var g = await db.Gastos.FindAsync(id);
        if (g == null) return Redirect("/Gastos");
        files.Borrar(g.AdjuntoPath);
        db.Gastos.Remove(g);
        await db.SaveChangesAsync();
        return Redirect($"/Gastos?anio={g.Fecha.Year}&mes={g.Fecha.Month}");
    }
}
