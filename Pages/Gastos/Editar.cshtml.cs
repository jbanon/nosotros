using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Love.Pages.Gastos;

public class EditarModel(AppDb db, FileStorage files, ConfigService config, RecurrenteService recurrentes, ListaCompraService listaCompra) : PageModel
{
    [BindProperty] public Gasto Gasto { get; set; } = new();
    [BindProperty] public string ImporteTexto { get; set; } = "";
    [BindProperty] public IFormFile? Adjunto { get; set; }
    [BindProperty] public bool QuitarAdjunto { get; set; }
    [BindProperty] public List<string> LinProducto { get; set; } = new();
    [BindProperty] public List<string> LinImporte { get; set; } = new();
    public List<string> ProductosConocidos { get; set; } = new();
    public decimal ImporteVeva { get; set; }
    public decimal ImporteHipoteca { get; set; }
    public bool Nuevo => Gasto.Id == 0;
    public string? Error { get; set; }

    async Task Cargar()
    {
        ImporteVeva = await config.GetDecimal(ConfigClaves.ImporteVeva, 300);
        ImporteHipoteca = await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900);
        ProductosConocidos = await recurrentes.Productos();
    }

    /// <summary>Semana de la lista de la compra desde la que se viene (para rellenar el ticket y el tiempo).</summary>
    [BindProperty] public DateOnly? Lunes { get; set; }

    public async Task<IActionResult> OnGet(int? id, DateOnly? fecha, DateOnly? lunes)
    {
        await Cargar();
        if (id is int i)
        {
            var g = await db.Gastos.Include(x => x.Lineas.OrderBy(l => l.Orden)).FirstOrDefaultAsync(x => x.Id == i);
            if (g == null) return NotFound();
            Gasto = g;
            ImporteTexto = g.Importe.ToString("0.##", CultureInfo.InvariantCulture);
            foreach (var l in g.Lineas)
            {
                LinProducto.Add(l.Producto);
                LinImporte.Add(l.Importe?.ToString("0.##", CultureInfo.InvariantCulture) ?? "");
            }
        }
        else
        {
            Gasto = new Gasto { Fecha = fecha ?? Reloj.Hoy, Tipo = TipoGasto.Compra };
            if (lunes is DateOnly l)
            {
                // Viene de "terminar la compra": nota y líneas del ticket con lo marcado como comprado
                Lunes = l;
                var semana = await listaCompra.SemanaDe(Reloj.Lunes(l));
                Gasto.Nota = $"Compra · semana del {semana.LunesInicio:d 'de' MMMM}";
                foreach (var it in semana.Lista.Where(i => i.Incluido && i.Comprado).OrderBy(i => i.Zona).ThenBy(i => i.Nombre))
                { LinProducto.Add(it.Nombre); LinImporte.Add(""); }
            }
        }
        return Page();
    }

    static decimal? Num(string? s) =>
        decimal.TryParse((s ?? "").Replace("€", "").Replace(',', '.').Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>"Guardar como compra nueva": crea otro gasto con lo escrito, sin tocar el que se estaba editando.</summary>
    public Task<IActionResult> OnPostNuevo() { Gasto.Id = 0; return OnPost(); }

    public async Task<IActionResult> OnPost()
    {
        await Cargar();
        if (!decimal.TryParse(ImporteTexto.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var importe) || importe <= 0)
        { Error = "Pon un importe válido"; return Page(); }

        Gasto g;
        if (Gasto.Id == 0) { g = new Gasto(); db.Gastos.Add(g); }
        else g = await db.Gastos.Include(x => x.Lineas).FirstOrDefaultAsync(x => x.Id == Gasto.Id) ?? throw new InvalidOperationException();
        g.Fecha = Gasto.Fecha; g.Tipo = Gasto.Tipo; g.Importe = importe; g.Nota = Gasto.Nota?.Trim();
        if (Lunes is DateOnly lu && g.Tipo == TipoGasto.Compra)
        {
            var semana = await listaCompra.SemanaDe(Reloj.Lunes(lu));
            g.DuracionMin = Love.Pages.Menu.ListaModel.Duracion(semana);
            semana.CompraInicio = null;
        }

        // Líneas del ticket: se sustituyen por las del formulario (solo en compras)
        db.TicketLineas.RemoveRange(g.Lineas);
        g.Lineas.Clear();
        if (g.Tipo == TipoGasto.Compra)
        {
            int orden = 0;
            for (int i = 0; i < LinProducto.Count; i++)
            {
                var prod = (LinProducto[i] ?? "").Trim();
                if (prod == "") continue;
                g.Lineas.Add(new TicketLinea { Producto = prod, Importe = i < LinImporte.Count ? Num(LinImporte[i]) : null, Orden = orden++ });
            }
        }

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
        if (g.EsFijo && !User.EsAdmin()) return Forbid();
        files.Borrar(g.AdjuntoPath);
        db.Gastos.Remove(g);
        await db.SaveChangesAsync();
        return Redirect($"/Gastos?anio={g.Fecha.Year}&mes={g.Fecha.Month}");
    }
}
