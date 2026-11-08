using Love.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Gastos;

public class RecurrentesModel(RecurrenteService recurrentes) : PageModel
{
    public List<ProductoRecurrente> Productos { get; set; } = new();
    public async Task OnGet() => Productos = await recurrentes.Analizar(Reloj.Hoy);
}
