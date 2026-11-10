using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Love.Pages.Menu;

public class ListaModel(AppDb db, ListaCompraService lista, RecurrenteService recurrentes, FileStorage files) : PageModel
{
    public Semana Semana { get; set; } = null!;
    public DateOnly Lunes { get; set; }
    public string TextoCompartir { get; set; } = "";
    public int Raciones { get; set; }
    public List<ProductoRecurrente> Reponer { get; set; } = new();
    public string? ErrorGasto { get; set; }
    /// <summary>Compras ya apuntadas hoy, para avisar y no duplicar.</summary>
    public List<Gasto> ComprasHoy { get; set; } = new();
    public bool Comprando => Semana.CompraInicio != null;
    public int MinutosComprando => Semana.CompraInicio is DateTime i ? (int)Math.Round((DateTime.UtcNow - i).TotalMinutes) : 0;

    /// <summary>Minutos desde "Empezar a comprar" (null si no se pulsó o hace más de 5 h, que será un olvido).</summary>
    public static int? Duracion(Semana s)
    {
        if (s.CompraInicio is not DateTime i) return null;
        var min = (int)Math.Round((DateTime.UtcNow - i).TotalMinutes);
        return min >= 0 && min <= 300 ? min : null;
    }

    public async Task OnGet(DateOnly? lunes)
    {
        Lunes = Reloj.Lunes(lunes ?? Reloj.Hoy);
        Semana = await lista.SemanaDe(Lunes);
        await lista.Regenerar(Semana.Id);
        Semana = await lista.SemanaDe(Lunes);
        Raciones = await lista.Raciones();
        TextoCompartir = ListaCompraService.TextoCompartir(Semana, Raciones);
        var enLista = Semana.Lista.Select(i => RecurrenteService.Clave(i.Nombre)).ToHashSet();
        Reponer = (await recurrentes.Reponer(Reloj.Hoy)).Where(p => !enLista.Contains(RecurrenteService.Clave(p.Nombre))).Take(8).ToList();
        var hoy = Reloj.Hoy;
        ComprasHoy = await db.Gastos.Where(g => g.Fecha == hoy && g.Tipo == TipoGasto.Compra).OrderByDescending(g => g.Creado).ToListAsync();
    }

    /// <summary>Terminar la compra: crea el gasto del mes con el importe del ticket, su foto y el detalle de lo comprado.</summary>
    public async Task<IActionResult> OnPostApuntarGasto(DateOnly lunes, string? importe, IFormFile? ticket, bool desmarcar)
    {
        var txt = (importe ?? "").Replace("€", "").Replace(',', '.').Trim();
        if (!decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var imp) || imp <= 0)
        {
            ErrorGasto = "Pon el importe del ticket.";
            await OnGet(lunes);
            return Page();
        }
        var s = await lista.SemanaDe(lunes);
        var g = new Gasto { Fecha = Reloj.Hoy, Tipo = TipoGasto.Compra, Importe = imp, Nota = $"Compra · semana del {s.LunesInicio:d 'de' MMMM}", DuracionMin = Duracion(s) };
        s.CompraInicio = null;
        int orden = 0;
        foreach (var it in s.Lista.Where(i => i.Incluido && i.Comprado).OrderBy(i => i.Zona).ThenBy(i => i.Nombre))
            g.Lineas.Add(new TicketLinea { Producto = it.Nombre, Orden = orden++ });
        if (ticket != null && ticket.Length > 0)
        {
            (g.AdjuntoPath, g.AdjuntoMime) = await files.Guardar(ticket, $"gastos/{g.Fecha.Year}/{g.Fecha.Month:00}");
            g.AdjuntoNombre = Path.GetFileName(ticket.FileName);
        }
        db.Gastos.Add(g);
        if (desmarcar)
            foreach (var it in s.Lista.Where(i => i.Comprado)) { if (it.Manual) db.ListaItems.Remove(it); else it.Comprado = false; }
        await db.SaveChangesAsync();
        return Redirect($"/Gastos?anio={g.Fecha.Year}&mes={g.Fecha.Month}");
    }

    public async Task<IActionResult> OnPostComprado(DateOnly lunes, int id)
    {
        var it = await db.ListaItems.FindAsync(id);
        if (it != null)
        {
            it.Comprado = !it.Comprado;
            // El primer producto marcado arranca el cronómetro si no se pulsó "Empezar a comprar"
            if (it.Comprado)
            {
                var s = await db.Semanas.FindAsync(it.SemanaId);
                if (s != null && s.CompraInicio == null) s.CompraInicio = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { lunes });
    }

    /// <summary>Parar (o rearmar) el cronómetro a mano; normalmente arranca solo al marcar el primer producto.</summary>
    public async Task<IActionResult> OnPostEmpezar(DateOnly lunes, bool parar)
    {
        var s = await lista.SemanaDe(lunes);
        s.CompraInicio = parar ? null : DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostIncluir(DateOnly lunes, int id, bool incluir)
    {
        var it = await db.ListaItems.FindAsync(id);
        if (it != null)
        {
            if (!incluir && it.Manual) db.ListaItems.Remove(it);
            else { it.Incluido = incluir; if (!incluir) it.Comprado = false; }
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostAnadir(DateOnly lunes, string nombre, string? cantidad, Zona zona)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre != "")
        {
            var s = await lista.SemanaDe(lunes);
            var ing = await db.Ingredientes.FirstOrDefaultAsync(i => i.Nombre.ToLower() == nombre.ToLower());
            decimal? cant = null; string? unidad = null;
            if (!string.IsNullOrWhiteSpace(cantidad))
            {
                var partes = cantidad.Trim().Split(' ', 2);
                if (decimal.TryParse(partes[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var c)) { cant = c; unidad = partes.Length > 1 ? partes[1] : null; }
                else unidad = cantidad.Trim();
            }
            s.Lista.Add(new ListaItem { Nombre = ing?.Nombre ?? nombre, IngredienteId = ing?.Id, Zona = ing?.Zona ?? zona, Cantidad = cant, Unidad = unidad, Manual = true, Incluido = true });
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { lunes });
    }

    public async Task<IActionResult> OnPostLimpiar(DateOnly lunes)
    {
        var s = await lista.SemanaDe(lunes);
        foreach (var it in s.Lista.Where(i => i.Comprado)) { if (it.Manual) db.ListaItems.Remove(it); else it.Comprado = false; }
        await db.SaveChangesAsync();
        return RedirectToPage(new { lunes });
    }
}
