using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public record Sugerencia(Receta Receta, string Motivo);

/// <summary>Sugiere recetas para una semana a partir del historial de semanas anteriores.</summary>
public class SugerenciaService(AppDb db)
{
    public async Task<List<Sugerencia>> Para(DateOnly lunes, int n = 4)
    {
        var recetas = await db.Recetas.ToListAsync();
        if (recetas.Count == 0) return new();
        var enSemana = (await db.SemanaRecetas.Where(s => s.Semana.LunesInicio == lunes).Select(s => s.RecetaId).ToListAsync()).ToHashSet();
        var hist = await db.SemanaRecetas.Where(s => s.Semana.LunesInicio < lunes)
                                         .Select(s => new { s.RecetaId, s.Semana.LunesInicio }).ToListAsync();
        var porReceta = hist.GroupBy(h => h.RecetaId)
                            .ToDictionary(g => g.Key, g => (ultima: g.Max(x => x.LunesInicio), veces: g.Count()));
        var candidatas = recetas.Where(r => !enSemana.Contains(r.Id)).ToList();
        var rnd = new Random(lunes.DayNumber); // las mismas sugerencias durante toda la semana
        var res = new List<Sugerencia>();

        // 1) Las que hace más tiempo que no cocinamos (nunca las de la semana pasada)
        foreach (var r in candidatas.Where(r => porReceta.ContainsKey(r.Id)).OrderBy(r => porReceta[r.Id].ultima).Take(2))
        {
            var sem = (lunes.DayNumber - porReceta[r.Id].ultima.DayNumber) / 7;
            if (sem < 2) break;
            res.Add(new(r, sem < 5 ? $"hace {sem} semanas que no la hacemos" : "hace mucho que no la hacemos"));
        }
        // 2) Favoritas (repetidas) que no hayan salido en las dos últimas semanas
        foreach (var r in candidatas.Where(r => porReceta.TryGetValue(r.Id, out var p) && p.veces >= 2 && p.ultima < lunes.AddDays(-14))
                                    .OrderByDescending(r => porReceta[r.Id].veces).ThenBy(_ => rnd.Next()))
        {
            if (res.Count >= n) break;
            if (res.Any(s => s.Receta.Id == r.Id)) continue;
            res.Add(new(r, $"de las que más repetimos · {porReceta[r.Id].veces} veces"));
        }
        // 3) Las que aún no hemos probado
        foreach (var r in candidatas.Where(r => !porReceta.ContainsKey(r.Id)).OrderBy(_ => rnd.Next()))
        {
            if (res.Count >= n) break;
            res.Add(new(r, "aún no la hemos hecho"));
        }
        return res;
    }
}
