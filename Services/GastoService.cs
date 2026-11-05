using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class GastoService(AppDb db, ConfigService config)
{
    /// <summary>Crea los fijos del mes la primera vez que se visita (solo meses hasta el actual).</summary>
    public async Task AsegurarFijos(int anio, int mes)
    {
        var hoy = Reloj.Hoy;
        if (anio * 100 + mes > hoy.Year * 100 + hoy.Month) return;
        if (await db.MesesInicializados.AnyAsync(m => m.Anio == anio && m.Mes == mes)) return;

        var veva = await config.GetDecimal(ConfigClaves.ImporteVeva, 300);
        var hipo = await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900);
        var f = new DateOnly(anio, mes, 1);
        db.Gastos.Add(new Gasto { Fecha = f, Tipo = TipoGasto.Veva, Importe = veva, EsFijo = true, Nota = "Fijo del mes" });
        db.Gastos.Add(new Gasto { Fecha = f, Tipo = TipoGasto.Hipoteca, Importe = hipo, EsFijo = true, Nota = "Fijo del mes" });
        db.MesesInicializados.Add(new MesInicializado { Anio = anio, Mes = mes });
        await db.SaveChangesAsync();
    }

    public Task<List<Gasto>> DelMes(int anio, int mes)
    {
        var ini = new DateOnly(anio, mes, 1);
        var fin = ini.AddMonths(1);
        return db.Gastos.Where(g => g.Fecha >= ini && g.Fecha < fin)
                        .OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Creado).ToListAsync();
    }
}
