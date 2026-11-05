using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class ListaCompraService(AppDb db)
{
    public async Task<Semana> SemanaDe(DateOnly lunes)
    {
        var s = await db.Semanas.Include(x => x.Recetas).ThenInclude(r => r.Receta)
                                .Include(x => x.Lista)
                                .FirstOrDefaultAsync(x => x.LunesInicio == lunes);
        if (s == null)
        {
            s = new Semana { LunesInicio = lunes };
            db.Semanas.Add(s);
            await db.SaveChangesAsync();
        }
        return s;
    }

    /// <summary>
    /// Sincroniza la lista con las recetas marcadas: añade lo nuevo, actualiza cantidades,
    /// quita lo que ya no procede de ninguna receta y conserva manuales, marcados y desmarcados.
    /// </summary>
    public async Task Regenerar(int semanaId)
    {
        var s = await db.Semanas.Include(x => x.Recetas).ThenInclude(r => r.Receta).ThenInclude(r => r.Ingredientes).ThenInclude(i => i.Ingrediente)
                                .Include(x => x.Lista)
                                .FirstAsync(x => x.Id == semanaId);

        // clave: ingrediente + unidad normalizada
        var agregados = new Dictionary<(int ingId, string unidad), (Ingrediente ing, decimal? cant, List<string> origen, string? unidadOriginal)>();
        foreach (var sr in s.Recetas)
            foreach (var ri in sr.Receta.Ingredientes)
            {
                var key = (ri.IngredienteId, NormUnidad(ri.Unidad));
                agregados.TryGetValue(key, out var cur);
                var cant = ri.Cantidad is decimal c ? c * sr.Veces : (decimal?)null;
                var total = cur.cant is decimal t && cant is decimal n ? t + n : cur.cant ?? cant;
                var origen = cur.origen ?? new List<string>();
                if (!origen.Contains(sr.Receta.Nombre)) origen.Add(sr.Receta.Nombre);
                agregados[key] = (ri.Ingrediente, total, origen, cur.unidadOriginal ?? (string.IsNullOrWhiteSpace(ri.Unidad) ? null : ri.Unidad.Trim()));
            }

        var autos = s.Lista.Where(l => !l.Manual).ToList();
        foreach (var l in autos)
        {
            var key = (l.IngredienteId ?? -1, NormUnidad(l.Unidad));
            if (agregados.TryGetValue(key, out var a))
            {
                l.Cantidad = a.cant;
                l.Unidad = a.unidadOriginal ?? l.Unidad;
                l.Nombre = a.ing.Nombre;
                l.Zona = a.ing.Zona;
                l.Origen = string.Join(", ", a.origen);
                agregados.Remove(key);
            }
            else db.ListaItems.Remove(l);
        }
        foreach (var (key, a) in agregados)
            s.Lista.Add(new ListaItem
            {
                IngredienteId = a.ing.Id, Nombre = a.ing.Nombre, Zona = a.ing.Zona,
                Cantidad = a.cant, Unidad = a.unidadOriginal,
                Incluido = !a.ing.Despensa, Origen = string.Join(", ", a.origen)
            });
        await db.SaveChangesAsync();
    }

    /// <summary>Unidad en minúsculas y singular ("hojas" y "hoja" se suman).</summary>
    public static string NormUnidad(string? u)
    {
        u = (u ?? "").Trim().ToLowerInvariant();
        return u.Length > 3 && u.EndsWith('s') ? u[..^1] : u;
    }

    public static string TextoCompartir(Semana s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🛒 Lista de la compra ({s.LunesInicio:d MMM} – {s.LunesInicio.AddDays(6):d MMM})");
        foreach (var g in s.Lista.Where(l => l.Incluido && !l.Comprado).GroupBy(l => l.Zona).OrderBy(g => g.Key))
        {
            sb.AppendLine();
            sb.AppendLine($"{ZonaInfo.Emoji(g.Key)} {ZonaInfo.Nombre(g.Key).ToUpperInvariant()}");
            foreach (var l in g.OrderBy(l => l.Nombre))
                sb.AppendLine($"• {l.Nombre}{Cant(l)}");
        }
        return sb.ToString();
    }

    public static string Cant(ListaItem l) =>
        l.Cantidad is decimal c ? $" {c:0.##} {l.Unidad}".TrimEnd() : (l.Unidad != null ? " " + l.Unidad : "");
}

public static class ZonaInfo
{
    public static string Nombre(Zona z) => z switch
    {
        Zona.Fruteria => "Frutería", Zona.Charcuteria => "Charcutería", Zona.Carniceria => "Carnicería",
        Zona.Pescaderia => "Pescadería", _ => "Otros"
    };
    public static string Emoji(Zona z) => z switch
    {
        Zona.Fruteria => "🥬", Zona.Charcuteria => "🧀", Zona.Carniceria => "🥩", Zona.Pescaderia => "🐟", _ => "🧺"
    };
    public static string Css(Zona z) => z.ToString().ToLowerInvariant();
    public static IEnumerable<Zona> Todas => Enum.GetValues<Zona>();
}
