using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Love.Pages.Juegos;

public class DueloModel(DueloCifras duelo) : PageModel
{
    DueloCifras.Jugador Yo => new(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!);

    public void OnGet() { }
    public async Task<IActionResult> OnGetEstado() => new JsonResult(await duelo.Estado(Yo));
    public async Task<IActionResult> OnPostRetar() { duelo.Retar(Yo); return await OnGetEstado(); }
    public async Task<IActionResult> OnPostAceptar() { duelo.Aceptar(Yo); return await OnGetEstado(); }
    public async Task<IActionResult> OnPostNueva() { duelo.Nueva(Yo); return await OnGetEstado(); }
    public async Task<IActionResult> OnPostSalir() { duelo.Salir(Yo); return await OnGetEstado(); }
    public async Task<IActionResult> OnPostJugada(int valor) { duelo.Jugada(Yo, valor); return await OnGetEstado(); }
}
