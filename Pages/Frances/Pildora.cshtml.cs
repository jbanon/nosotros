using System.Security.Claims;
using System.Text.Json.Nodes;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Frances;

public class PildoraModel(AppDb db, FrancesService frances) : PageModel
{
    public PildoraFrances P { get; set; } = null!;
    public JsonObject C { get; set; } = new();
    public PildoraEntrega? Mia { get; set; }
    /// <summary>Para Javi: las entregas de los demás (las de Raquel), con su nombre.</summary>
    public List<(string Nombre, PildoraEntrega E)> Otras { get; set; } = new();
    public bool EsAdmin => User.EsAdmin();
    public bool Corazon { get; set; }
    int Yo => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    async Task<bool> Cargar(int id)
    {
        var p = await db.Pildoras.FindAsync(id);
        if (p == null || (p.Fecha > Reloj.Hoy && !EsAdmin)) return false;
        P = p; C = FrancesService.Json(p.Contenido);
        Mia = await db.PildoraEntregas.FirstOrDefaultAsync(e => e.PildoraId == id && e.UsuarioId == Yo);
        if (EsAdmin)
        {
            var otras = await db.PildoraEntregas.Where(e => e.PildoraId == id && e.UsuarioId != Yo).ToListAsync();
            var nombres = await db.Usuarios.ToDictionaryAsync(u => u.Id, u => u.Nombre);
            Otras = otras.Select(e => (nombres.GetValueOrDefault(e.UsuarioId, "?"), e)).ToList();
        }
        return true;
    }

    public async Task<IActionResult> OnGet(int id, bool corazon)
    {
        if (!await Cargar(id)) return NotFound();
        Corazon = corazon;
        return Page();
    }

    /// <summary>¿Hay alguna corrección en marcha en la página? (para recargar sola).</summary>
    public bool Esperando => Mia?.Estado == EstadoEntrega.Corrigiendo || Otras.Any(o => o.E.Estado == EstadoEntrega.Corrigiendo);

    public async Task<IActionResult> OnPostEntregar(int id, List<string>? preguntas, List<string>? huecos, string? comentario)
    {
        if (!await Cargar(id)) return NotFound();
        if (Mia != null) return RedirectToPage(new { id });
        var nVoc = FrancesService.Lista(C, "vocabulario").Count;
        var voc = new JsonArray();
        for (int i = 0; i < nVoc; i++) voc.Add(int.TryParse(Request.Form[$"voc{i}"], out var v) ? v : -1);
        var r = new JsonObject
        {
            ["preguntas"] = new JsonArray((preguntas ?? new()).Select(x => (JsonNode?)JsonValue.Create((x ?? "").Trim())).ToArray()),
            ["huecos"] = new JsonArray((huecos ?? new()).Select(x => (JsonNode?)JsonValue.Create((x ?? "").Trim())).ToArray()),
            ["vocabulario"] = voc,
            ["comentario"] = (comentario ?? "").Trim(),
        };
        await frances.Entregar(P, Yo, r);
        return RedirectToPage(new { id, corazon = true });
    }

    public async Task<IActionResult> OnPostReintentar(int id, int entrega)
    {
        if (!await Cargar(id)) return NotFound();
        var e = await db.PildoraEntregas.FindAsync(entrega);
        if (e != null && e.PildoraId == id && (e.UsuarioId == Yo || EsAdmin) && e.Estado == EstadoEntrega.Error) await frances.ReintentarCorreccion(e);
        return RedirectToPage(new { id });
    }
}
