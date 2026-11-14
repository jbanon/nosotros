using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Juegos;

public class CrucigramaModel(CrucigramaGenerador gen) : PageModel
{
    public int N { get; set; }
    public int Semilla { get; set; }
    public CrucigramaGenerador.Rejilla Rejilla { get; set; } = null!;

    public void OnGet(int n = 0)
    {
        N = Math.Max(0, n);
        Semilla = CrucigramaGenerador.Semilla(Reloj.Hoy, N);
        Rejilla = gen.Generar(Semilla);
    }
}
