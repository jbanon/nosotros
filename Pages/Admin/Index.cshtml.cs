using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin;

public class IndexModel(AppDb db, ConfigService config) : PageModel
{
    /// <summary>Vales que Raquel ha canjeado en los últimos 30 días (adviento y desbloqueo de viñetas).</summary>
    public List<(string Titulo, DateTime Cuando)> Canjes { get; set; } = new();
    public int NumMensajes { get; set; }
    public int NumFechas { get; set; }
    public int RespuestasNuevas { get; set; }
    public int VinetasSinFrase { get; set; }
    public async Task OnGet()
    {
        RespuestasNuevas = await db.Vinetas.CountAsync(v => v.RespuestaRaquel != null && !v.RespuestaVista);
        VinetasSinFrase = await db.Vinetas.CountAsync(v => v.Activa && string.IsNullOrEmpty(v.FraseMia));
        NumMensajes = await db.Mensajes.CountAsync(m => m.Activo);
        NumFechas = await db.FechasEspeciales.CountAsync(f => f.Activo);
        var desde = DateTime.UtcNow.AddDays(-30);
        foreach (var c in await db.Adviento.Where(c => c.CanjeadoEl != null && c.CanjeadoEl >= desde).ToListAsync())
            Canjes.Add((c.Titulo, c.CanjeadoEl!.Value));
        if (DateTime.TryParse(await config.Get(ConfigClaves.LogroValeCanjeado, ""), null, System.Globalization.DateTimeStyles.RoundtripKind, out var v) && v >= desde)
        {
            var texto = await config.Get(ConfigClaves.LogroValeTexto, "");
            Canjes.Add((string.IsNullOrWhiteSpace(texto) ? "El vale de los corazones" : texto, v));
        }
        Canjes = Canjes.OrderByDescending(x => x.Cuando).ToList();
    }
}
