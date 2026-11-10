using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class GastoService(AppDb db, ConfigService config)
{
    /// <summary>Primer mes de la app: antes, todo en blanco (sin fijos automáticos).</summary>
    public static readonly DateOnly InicioApp = new(2026, 11, 1);

    /// <summary>Crea los fijos del mes la primera vez que se visita (solo meses entre el inicio de la app y el actual).</summary>
    public async Task AsegurarFijos(int anio, int mes)
    {
        var hoy = Reloj.Hoy;
        if (anio * 100 + mes > hoy.Year * 100 + hoy.Month) return;
        if (anio * 100 + mes < InicioApp.Year * 100 + InicioApp.Month) return;
        if (await db.MesesInicializados.AnyAsync(m => m.Anio == anio && m.Mes == mes)) return;

        var veva = await config.GetDecimal(ConfigClaves.ImporteVeva, 300);
        var hipo = await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900);
        var f = new DateOnly(anio, mes, 1);
        db.Gastos.Add(new Gasto { Fecha = f, Tipo = TipoGasto.Veva, Importe = veva, EsFijo = true, Nota = "Fijo del mes" });
        db.Gastos.Add(new Gasto { Fecha = f, Tipo = TipoGasto.Hipoteca, Importe = hipo, EsFijo = true, Nota = "Fijo del mes" });
        db.MesesInicializados.Add(new MesInicializado { Anio = anio, Mes = mes });
        await db.SaveChangesAsync();
    }

    public record ResumenMes(int Anio, int Mes, decimal Compra, decimal Veva, decimal Hipoteca, int Movimientos)
    {
        public decimal Total => Compra + Veva + Hipoteca;
        public DateOnly Inicio => new(Anio, Mes, 1);
    }

    /// <summary>Un resumen por cada mes con movimientos, del más reciente al más antiguo.</summary>
    public async Task<List<ResumenMes>> Historico()
    {
        var todos = await db.Gastos.Select(g => new { g.Fecha.Year, g.Fecha.Month, g.Tipo, g.Importe }).ToListAsync();
        return todos.GroupBy(g => (g.Year, g.Month))
            .Select(gr => new ResumenMes(gr.Key.Year, gr.Key.Month,
                gr.Where(x => x.Tipo == TipoGasto.Compra).Sum(x => x.Importe),
                gr.Where(x => x.Tipo == TipoGasto.Veva).Sum(x => x.Importe),
                gr.Where(x => x.Tipo == TipoGasto.Hipoteca).Sum(x => x.Importe),
                gr.Count()))
            .OrderByDescending(r => r.Anio).ThenByDescending(r => r.Mes).ToList();
    }

    public Task<List<Gasto>> DelMes(int anio, int mes)
    {
        var ini = new DateOnly(anio, mes, 1);
        var fin = ini.AddMonths(1);
        return db.Gastos.Include(g => g.Lineas).Where(g => g.Fecha >= ini && g.Fecha < fin)
                        .OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Creado).ToListAsync();
    }
}
