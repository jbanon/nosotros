using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Adviento;

public class IndexModel(AdvientoService adv, AppDb db, ConfigService config) : PageModel
{
    public List<AdvientoCasilla> Casillas { get; set; } = new();
    public int Anio { get; set; }
    public DateOnly Hoy { get; set; }
    [BindProperty] public string Frase { get; set; } = "";
    public int Letras => Casillas.Count(c => c.DaLetra);
    public string LetrasEnOrden => string.Concat(Casillas.Where(c => c.DaLetra).OrderBy(c => c.Dia).Select(c => c.Letra!.ToUpperInvariant()));
    public string FraseSinEspacios => new(AdvientoService.Normalizar(Frase).Where(char.IsLetter).ToArray());

    public async Task OnGet()
    {
        Hoy = Reloj.Hoy;
        Anio = AdvientoService.AnioDe(Hoy);
        Casillas = await adv.Casillas(Anio);
        if (Casillas.Count == 0) { Anio = Hoy.Year; Casillas = await adv.Casillas(Anio); }
        Frase = await config.Get(ConfigClaves.AdvientoFrase, "");
    }

    public async Task<IActionResult> OnPostFrase()
    {
        await config.Set(ConfigClaves.AdvientoFrase, Frase.Trim());
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReiniciarTodo()
    {
        var anio = AdvientoService.AnioDe(Reloj.Hoy);
        foreach (var c in await db.Adviento.Where(x => x.Anio == anio).ToListAsync())
        { c.AbiertaEl = null; c.ResueltaEl = null; c.CanjeadoEl = null; c.Estado = null; c.Pistas = 0; }
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public static string Estado(AdvientoCasilla c, DateOnly hoy)
    {
        if (c.Tipo == TipoCasilla.Vale) return c.CanjeadoEl != null ? "🎟️ canjeado" : c.AbiertaEl != null ? "👀 abierto" : AdvientoService.Abierta(c, hoy) ? "— sin abrir" : "🔒";
        if (c.Resuelta) return "✅ resuelta" + (c.Pistas > 0 ? $" ({c.Pistas} pista{(c.Pistas == 1 ? "" : "s")})" : "");
        if (c.AbiertaEl != null) return "👀 abierta" + (c.Pistas > 0 ? $" ({c.Pistas} pista{(c.Pistas == 1 ? "" : "s")})" : "");
        return AdvientoService.Abierta(c, hoy) ? "— sin abrir" : "🔒";
    }
}
