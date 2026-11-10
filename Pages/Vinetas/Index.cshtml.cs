using Love.Models;
using Love.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Vinetas;

/// <summary>Álbum: las viñetas que Raquel ya ha descubierto.</summary>
public class IndexModel(VinetaService vinetas, Love.Data.AppDb db, ConfigService config) : PageModel
{
    public List<VinetaDia> Dias { get; set; } = new();
    public int Total { get; set; }
    public int Corazones { get; set; }
    public VinetaDia? Hoy { get; set; }
    public List<VinetaService.Logro> Logros { get; set; } = new();
    public bool ValeCanjeado { get; set; }
    public string Firma { get; set; } = "";

    public async Task OnGet()
    {
        var hoy = Reloj.Hoy;
        Dias = await vinetas.Desveladas(hoy);
        Total = await db.Vinetas.CountAsync(v => v.Activa && v.UsoPortada);
        Corazones = await vinetas.Corazones();
        Hoy = await vinetas.DelDia(hoy, persistir: false);
        Logros = await vinetas.Logros();
        ValeCanjeado = await config.Get(ConfigClaves.LogroValeCanjeado, "") != "";
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
    }

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostCanjear()
    {
        var logros = await vinetas.Logros();
        if (logros.First(l => l.Clave == "vale").Conseguido && !User.EsAdmin()) await config.Set(ConfigClaves.LogroValeCanjeado, DateTime.UtcNow.ToString("o"));
        return RedirectToPage();
    }
}
