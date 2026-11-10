using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Vinetas;

public class IndexModel(AppDb db, VinetaService vinetas, ConfigService config) : PageModel
{
    public List<Vineta> Lista { get; set; } = new();
    public List<string> Libros { get; set; } = new();
    public string? Libro { get; set; }
    public string? Filtro { get; set; }
    public List<VinetaService.Carpeta> Carpetas { get; set; } = new();
    public Dictionary<int, VinetaDia> UltimoDia { get; set; } = new();
    public HashSet<int> Analizando { get; set; } = new();
    public string PorSemana { get; set; } = "3";
    public string JuegoDe10 { get; set; } = "5";
    public string ConocidasDesde { get; set; } = "2027-02-01";
    public string LogroGrande { get; set; } = "3"; public string LogroVale { get; set; } = "6"; public string LogroCarta { get; set; } = "10";
    public string LogroValeTexto { get; set; } = ""; public string LogroCartaTexto { get; set; } = ""; public string ValeCanjeado { get; set; } = "";
    public int Corazones { get; set; }
    public int? Importadas { get; set; }
    public VinetaDia? Hoy { get; set; }

    public async Task OnGet(string? libro, string? filtro, int? importadas)
    {
        Libro = libro; Filtro = filtro; Importadas = importadas;
        var q = db.Vinetas.AsQueryable();
        if (!string.IsNullOrEmpty(libro)) q = q.Where(v => v.Libro == libro);
        Lista = await q.OrderByDescending(v => v.RespondidaEl != null && !v.RespuestaVista).ThenBy(v => v.Libro).ThenBy(v => v.Id).ToListAsync();
        if (filtro == "sinfrase") Lista = Lista.Where(v => string.IsNullOrWhiteSpace(v.FraseMia)).ToList();
        else if (filtro == "respuestas") Lista = Lista.Where(v => v.RespuestaRaquel != null).ToList();
        else if (filtro == "mostradas") Lista = Lista.Where(v => v.MostradaEl != null).ToList();
        else if (filtro == "pendientes") Lista = Lista.Where(v => v.MostradaEl == null && v.Activa).ToList();
        Libros = await db.Vinetas.Where(v => v.Libro != null).Select(v => v.Libro!).Distinct().OrderBy(x => x).ToListAsync();
        Carpetas = await vinetas.CarpetasImportables();
        UltimoDia = (await db.VinetaDias.ToListAsync()).GroupBy(d => d.VinetaId).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Fecha).First());
        Analizando = (await db.TareasAgente.Where(t => t.VinetaId != null && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso)).Select(t => t.VinetaId!.Value).ToListAsync()).ToHashSet();
        PorSemana = await config.Get(ConfigClaves.VinetasPorSemana, "3");
        JuegoDe10 = await config.Get(ConfigClaves.VinetasJuegoDe10, "5");
        ConocidasDesde = await config.Get(ConfigClaves.VinetasConocidasDesde, "2027-02-01");
        Hoy = await vinetas.DelDia(Reloj.Hoy, persistir: false);
        LogroGrande = await config.Get(ConfigClaves.LogroGrande, "3"); LogroVale = await config.Get(ConfigClaves.LogroVale, "6"); LogroCarta = await config.Get(ConfigClaves.LogroCarta, "10");
        LogroValeTexto = await config.Get(ConfigClaves.LogroValeTexto, "Vale por un desayuno en la cama, con el periódico y sin prisa."); LogroCartaTexto = await config.Get(ConfigClaves.LogroCartaTexto, "");
        ValeCanjeado = await config.Get(ConfigClaves.LogroValeCanjeado, ""); Corazones = await vinetas.Corazones();
    }

    public async Task<IActionResult> OnPostLogros(string grande, string vale, string carta, string valeTexto, string cartaTexto, bool reiniciarVale)
    {
        await config.Set(ConfigClaves.LogroGrande, grande.Trim()); await config.Set(ConfigClaves.LogroVale, vale.Trim()); await config.Set(ConfigClaves.LogroCarta, carta.Trim());
        await config.Set(ConfigClaves.LogroValeTexto, valeTexto.Trim()); await config.Set(ConfigClaves.LogroCartaTexto, cartaTexto.Trim());
        if (reiniciarVale) await config.Set(ConfigClaves.LogroValeCanjeado, "");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportar(string carpeta)
    {
        var n = await vinetas.Importar(carpeta);
        return RedirectToPage(new { importadas = n });
    }

    public async Task<IActionResult> OnPostConfig(string porSemana, string juegoDe10, string conocidasDesde)
    {
        await config.Set(ConfigClaves.VinetasPorSemana, porSemana.Trim());
        await config.Set(ConfigClaves.VinetasJuegoDe10, juegoDe10.Trim());
        await config.Set(ConfigClaves.VinetasConocidasDesde, conocidasDesde.Trim());
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVistas()
    {
        foreach (var v in await db.Vinetas.Where(v => v.RespuestaRaquel != null && !v.RespuestaVista).ToListAsync()) v.RespuestaVista = true;
        await db.SaveChangesAsync();
        return RedirectToPage(new { filtro = "respuestas" });
    }
}
