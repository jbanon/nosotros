using Love.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages;

public class LoginModel(PinAuth auth) : PageModel
{
    public string? Error { get; set; }

    public IActionResult OnGet(bool salir = false)
    {
        if (salir) { HttpContext.SignOutAsync().Wait(); return Page(); }
        return Page();
    }

    public async Task<IActionResult> OnPost(string pin)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        if (PinAuth.Bloqueado(ip)) { Error = "Demasiados intentos. Espera un minuto."; return Page(); }
        var u = await auth.Validar(pin ?? "", ip);
        if (u == null) { Error = "Ese PIN no es"; return Page(); }
        await PinAuth.Entrar(HttpContext, u);
        return Redirect(u.Rol == Models.Rol.Admin ? "/Gastos" : "/Amor");
    }
}
