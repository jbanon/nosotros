using System.Globalization;
using System.Text;
using Love.Data;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public record ProductoRecurrente(string Nombre, int Veces, int? CadaDias, DateOnly Ultima, decimal? PrecioMedio, int DiasDesdeUltima)
{
    /// <summary>Hay patrón y ya ha pasado (casi) el intervalo habitual.</summary>
    public bool TocaReponer => CadaDias is int c && DiasDesdeUltima >= Math.Max(1, (int)Math.Round(c * 0.85));
    public int? FaltanDias => CadaDias is int c ? c - DiasDesdeUltima : null;
}

/// <summary>Detecta compras recurrentes a partir de las líneas de ticket apuntadas en los gastos.</summary>
public class RecurrenteService(AppDb db)
{
    /// <summary>Clave de agrupación: minúsculas, sin acentos ni espacios dobles.</summary>
    public static string Clave(string s)
    {
        var norm = (s ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public async Task<List<ProductoRecurrente>> Analizar(DateOnly hoy)
    {
        var lineas = await db.TicketLineas.Select(l => new { l.Producto, l.Importe, l.Gasto.Fecha }).ToListAsync();
        var res = new List<ProductoRecurrente>();
        foreach (var g in lineas.GroupBy(l => Clave(l.Producto)))
        {
            if (g.Key == "") continue;
            var fechas = g.Select(x => x.Fecha).Distinct().OrderBy(f => f).ToList();
            int? cada = null;
            if (fechas.Count >= 2)
            {
                var intervalos = fechas.Zip(fechas.Skip(1), (a, b) => b.DayNumber - a.DayNumber).OrderBy(x => x).ToList();
                cada = Math.Max(1, intervalos[intervalos.Count / 2]); // mediana: aguanta un olvido o una compra extra
            }
            var nombre = g.OrderByDescending(x => x.Fecha).First().Producto.Trim();
            var precios = g.Where(x => x.Importe is decimal p && p > 0).Select(x => x.Importe!.Value).ToList();
            res.Add(new(nombre, fechas.Count, cada, fechas[^1], precios.Count > 0 ? Math.Round(precios.Average(), 2) : null, hoy.DayNumber - fechas[^1].DayNumber));
        }
        return res.OrderByDescending(r => r.TocaReponer).ThenBy(r => r.FaltanDias ?? int.MaxValue).ThenByDescending(r => r.Veces).ThenBy(r => r.Nombre).ToList();
    }

    public async Task<List<ProductoRecurrente>> Reponer(DateOnly hoy) => (await Analizar(hoy)).Where(r => r.TocaReponer).ToList();

    public Task<List<string>> Productos() =>
        db.TicketLineas.Select(l => l.Producto).Distinct().OrderBy(p => p).ToListAsync();
}
