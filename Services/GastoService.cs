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
        if (!await db.MesesInicializados.AnyAsync(m => m.Anio == anio && m.Mes == mes)) await CrearFijos(anio, mes);
        // la compra de este mes descuenta de la hipoteca del siguiente (y la del anterior, de esta)
        await AjustarHipoteca(anio, mes);
        var sig = new DateOnly(anio, mes, 1).AddMonths(1);
        await AjustarHipoteca(sig.Year, sig.Month);
    }

    /// <summary>Lo gastado en compra por encima del objetivo ese mes (0 si no se pasó).</summary>
    public async Task<decimal> ExcesoCompra(int anio, int mes)
    {
        var ini = new DateOnly(anio, mes, 1);
        if (ini < InicioApp) return 0;
        var fin = ini.AddMonths(1);
        var compra = await db.Gastos.Where(g => g.Tipo == TipoGasto.Compra && g.Fecha >= ini && g.Fecha < fin).SumAsync(g => g.Importe);
        return Math.Max(0, compra - await config.GetDecimal(ConfigClaves.ObjetivoCompra, 350));
    }

    /// <summary>
    /// Hipoteca fija del mes = importe de la hipoteca − lo que la compra del mes anterior pasó del objetivo
    /// (ej.: 540 de compra con objetivo 350 → el mes siguiente 900 − 190 = 710). Nunca baja de 0. Se recalcula
    /// cada vez, así que un ticket tardío del mes anterior también cuenta.
    /// </summary>
    public async Task AjustarHipoteca(int anio, int mes)
    {
        var ini = new DateOnly(anio, mes, 1);
        var fijo = await db.Gastos.FirstOrDefaultAsync(g => g.EsFijo && g.Tipo == TipoGasto.Hipoteca && g.Fecha >= ini && g.Fecha < ini.AddMonths(1));
        if (fijo == null) return;
        var ant = ini.AddMonths(-1);
        var exceso = await ExcesoCompra(ant.Year, ant.Month);
        var base_ = await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900);
        var importe = Math.Max(0, base_ - exceso);
        var nota = exceso > 0 ? $"Fijo del mes: {base_:N0} − {exceso:N2} € de compra de más en {ant:MMMM}" : "Fijo del mes";
        if (fijo.Importe == importe && fijo.Nota == nota) return;
        fijo.Importe = importe; fijo.Nota = nota;
        await db.SaveChangesAsync();
    }

    async Task CrearFijos(int anio, int mes)
    {
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
        foreach (var m in await db.MesesInicializados.ToListAsync()) await AjustarHipoteca(m.Anio, m.Mes);
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
