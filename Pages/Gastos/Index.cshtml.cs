using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Gastos;

public class IndexModel(GastoService gastos, ConfigService config) : PageModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<Gasto> Lista { get; set; } = new();
    public decimal Objetivo { get; set; }
    public decimal Compra => Lista.Where(g => g.Tipo == TipoGasto.Compra).Sum(g => g.Importe);
    public decimal Veva => Lista.Where(g => g.Tipo == TipoGasto.Veva).Sum(g => g.Importe) + (Prevision ? VevaPrev : 0);
    public decimal Hipoteca => Lista.Where(g => g.Tipo == TipoGasto.Hipoteca).Sum(g => g.Importe) + (Prevision ? HipotecaPrev : 0);
    public decimal Total => Lista.Sum(g => g.Importe) + (Prevision ? VevaPrev + HipotecaPrev : 0);
    /// <summary>Mes que aún no ha empezado: los fijos se muestran como previsión (se apuntan el día 1).</summary>
    public bool Prevision { get; set; }
    public decimal VevaPrev { get; set; }
    public decimal HipotecaPrev { get; set; }
    public DateOnly Inicio => new(Anio, Mes, 1);
    /// <summary>Lo que se descuenta de la hipoteca de este mes por la compra de más del anterior.</summary>
    public decimal DescuentoHipoteca { get; set; }

    public async Task OnGet(int? anio, int? mes)
    {
        var hoy = Reloj.Hoy;
        Anio = anio ?? hoy.Year; Mes = mes ?? hoy.Month;
        await gastos.AsegurarFijos(Anio, Mes);
        Lista = await gastos.DelMes(Anio, Mes);
        Objetivo = await config.GetDecimal(ConfigClaves.ObjetivoCompra, 350);
        var ant = Inicio.AddMonths(-1);
        if (Lista.Any(g => g.EsFijo && g.Tipo == TipoGasto.Hipoteca)) DescuentoHipoteca = await gastos.ExcesoCompra(ant.Year, ant.Month);
        else if (Inicio == new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1) && Inicio >= GastoService.InicioApp)
        {
            Prevision = true;
            (VevaPrev, HipotecaPrev, DescuentoHipoteca) = await gastos.PrevisionFijos(Anio, Mes);
        }
    }
}
