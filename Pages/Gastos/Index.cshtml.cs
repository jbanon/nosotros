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
    public decimal Veva => Lista.Where(g => g.Tipo == TipoGasto.Veva).Sum(g => g.Importe);
    public decimal Hipoteca => Lista.Where(g => g.Tipo == TipoGasto.Hipoteca).Sum(g => g.Importe);
    public decimal Total => Lista.Sum(g => g.Importe);
    public DateOnly Inicio => new(Anio, Mes, 1);

    public async Task OnGet(int? anio, int? mes)
    {
        var hoy = Reloj.Hoy;
        Anio = anio ?? hoy.Year; Mes = mes ?? hoy.Month;
        await gastos.AsegurarFijos(Anio, Mes);
        Lista = await gastos.DelMes(Anio, Mes);
        Objetivo = await config.GetDecimal(ConfigClaves.ObjetivoCompra, 350);
    }
}
