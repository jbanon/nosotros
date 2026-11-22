using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Love.Pages.Juegos;

public class CrucigramaModel(CrucigramaGenerador gen, CruciCompartido compartido) : PageModel
{
    public int N { get; set; }
    public int Semilla { get; set; }
    public CrucigramaGenerador.Rejilla Rejilla { get; set; } = null!;
    int Yo => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public void OnGet(int n = 0)
    {
        N = Math.Max(0, n);
        Semilla = CrucigramaGenerador.Semilla(Reloj.Hoy, N, Ninas.De(User));
        Rejilla = gen.Generar(Semilla);
    }

    public record Op(int Semilla, string? K, string? Dir, Dictionary<string, string>? Letras, bool Ayuda, bool Rendirse);

    /// <summary>Consulta y cambios en una sola llamada: manda el cursor y las letras pendientes, devuelve el estado.</summary>
    public async Task<IActionResult> OnPostSync([FromBody] Op op) =>
        CrucigramaGenerador.OffsetDe(op.Semilla) != (Ninas.De(User)?.OffsetCruci ?? 0) && !User.EsAdmin() ? Forbid() : new JsonResult(await compartido.Operar(op.Semilla, Yo, op.K, op.Dir,
            (op.Letras ?? new()).Select(x => (x.Key, x.Value)), op.Ayuda, op.Rendirse));
}
