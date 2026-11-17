using System.Security.Claims;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Frances;

public class IndexModel(AppDb db, FrancesService frances, AgenteService agente) : PageModel
{
    public List<PildoraFrances> Pildoras { get; set; } = new();
    public List<PildoraFrances> Futuras { get; set; } = new();
    public Dictionary<int, PildoraEntrega> Mias { get; set; } = new();
    public Dictionary<int, PildoraEntrega> DeRaquel { get; set; } = new();
    public List<TareaAgente> Tareas { get; set; } = new();
    public bool Preparando { get; set; }
    public DateOnly Proxima { get; set; }
    public bool AgenteDisponible => agente.Disponible;
    public bool EsAdmin => User.EsAdmin();
    int Yo => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGet()
    {
        var hoy = Reloj.Hoy;
        Pildoras = await frances.Publicadas(hoy);
        var ids = Pildoras.Select(p => p.Id).ToList();
        var entregas = await db.PildoraEntregas.Where(e => ids.Contains(e.PildoraId)).ToListAsync();
        Mias = entregas.Where(e => e.UsuarioId == Yo).ToDictionary(e => e.PildoraId);
        Preparando = await frances.Preparando();
        if (EsAdmin)
        {
            var raquel = await db.Usuarios.Where(u => u.Rol == Rol.Raquel).Select(u => u.Id).ToListAsync();
            DeRaquel = entregas.Where(e => raquel.Contains(e.UsuarioId)).GroupBy(e => e.PildoraId).ToDictionary(g => g.Key, g => g.First());
            Futuras = await db.Pildoras.Where(p => p.Fecha > hoy).OrderBy(p => p.Fecha).ToListAsync();
            Tareas = await db.TareasAgente.Where(t => t.Tipo == TipoTareaAgente.Pildora || t.Tipo == TipoTareaAgente.PildoraCorreccion)
                .OrderByDescending(t => t.Id).Take(6).ToListAsync();
            Proxima = await frances.ProximaFecha(hoy);
        }
    }

    /// <summary>Javi encarga una píldora a mano (sale hoy), con tema y, si quiere, una idea.</summary>
    public async Task<IActionResult> OnPostEncargar(string tema, string? idea)
    {
        if (!EsAdmin) return Forbid();
        await frances.Encargar(Reloj.Hoy, tema, idea, User.Identity?.Name ?? "Javi");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReintentar(int id)
    {
        if (!EsAdmin) return Forbid();
        var t = await db.TareasAgente.FindAsync(id);
        if (t == null) return RedirectToPage();
        if (t.EntregaId is int e && await db.PildoraEntregas.FindAsync(e) is { } en) await frances.ReintentarCorreccion(en);
        else await agente.Reintentar(db, t);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrar(int id)
    {
        if (!EsAdmin) return Forbid();
        if (await db.Pildoras.FindAsync(id) is { } p) { db.Pildoras.Remove(p); await db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
