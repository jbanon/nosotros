using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Gastos;

public class HistoricoModel(GastoService gastos, ConfigService config) : PageModel
{
    public List<GastoService.ResumenMes> Meses { get; set; } = new();
    public decimal Objetivo { get; set; }

    public async Task OnGet()
    {
        var hoy = Reloj.Hoy;
        await gastos.AsegurarFijos(hoy.Year, hoy.Month);
        Meses = await gastos.Historico();
        Objetivo = await config.GetDecimal(ConfigClaves.ObjetivoCompra, 350);
    }
}
