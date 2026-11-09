using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Adviento;

public class IndexModel(AdvientoService adv, ConfigService config) : PageModel
{
    public List<AdvientoCasilla> Casillas { get; set; } = new();
    public DateOnly Hoy { get; set; }
    public int Anio { get; set; }
    public bool Preview { get; set; }
    public int Letras { get; set; }
    public int Total { get; set; }
    public int DiasParaEmpezar { get; set; }
    public string Nombre { get; set; } = "";

    public async Task<IActionResult> OnGet(DateOnly? fecha)
    {
        Preview = fecha != null && User.EsAdmin();
        Hoy = Preview ? fecha!.Value : Reloj.Hoy;
        Anio = AdvientoService.AnioDe(Hoy);
        Casillas = await adv.Casillas(Anio);
        if (Casillas.Count == 0) return User.EsAdmin() ? Redirect("/Admin/Adviento") : Redirect("/Amor");
        Nombre = await config.Get(ConfigClaves.NombreElla, "Raquel");
        Total = Casillas.Count(c => c.DaLetra);
        Letras = Casillas.Count(c => c.DaLetra && c.Resuelta);
        var inicio = new DateOnly(Anio, 12, 1);
        DiasParaEmpezar = Math.Max(0, inicio.DayNumber - Hoy.DayNumber);
        ViewData["Tema"] = "navidad";
        return Page();
    }

    public bool Abierta(AdvientoCasilla c) => AdvientoService.Abierta(c, Hoy);
    public bool EsHoy(AdvientoCasilla c) => AdvientoService.FechaDe(c) == Hoy;
    public static string Icono(AdvientoCasilla c) => c.Tipo switch
    {
        TipoCasilla.Foto => "📷", TipoCasilla.Criptofrase => "🔡", TipoCasilla.Murdoku => "🔍",
        TipoCasilla.Crucigrama => "✏️", TipoCasilla.Vale => "🎟️", _ => "💌"
    };
}
