using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Vinetas;

public class VerModel(AppDb db, VinetaService vinetas, ConfigService config) : PageModel
{
    public Vineta V { get; set; } = null!;
    public VinetaDia? Dia { get; set; }
    public string Firma { get; set; } = "";
    public bool Guardada { get; set; }
    public bool GrandeDesbloqueado { get; set; }

    async Task<bool> Cargar(int id)
    {
        if (!await vinetas.PuedeVer(id, Reloj.Hoy, User.EsAdmin())) return false;
        var v = await db.Vinetas.Include(x => x.Dias).FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return false;
        V = v; Dia = v.Dias.OrderByDescending(d => d.Fecha).FirstOrDefault();
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
        GrandeDesbloqueado = (await vinetas.Logros()).First(l => l.Clave == "grande").Conseguido;
        return true;
    }

    public async Task<IActionResult> OnGet(int id, bool guardada) { Guardada = guardada; return await Cargar(id) ? Page() : NotFound(); }

    public async Task<IActionResult> OnPostFavorita(int id)
    {
        if (!await Cargar(id)) return NotFound();
        V.Favorita = !V.Favorita; await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResponder(int id, string? texto)
    {
        if (!await Cargar(id)) return NotFound();
        texto = (texto ?? "").Trim();
        V.RespuestaRaquel = texto == "" ? null : texto; V.RespondidaEl = texto == "" ? null : DateTime.UtcNow; V.RespuestaVista = false;
        await db.SaveChangesAsync();
        return RedirectToPage(new { id, guardada = true });
    }
}
