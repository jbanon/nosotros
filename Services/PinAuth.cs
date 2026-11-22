using System.Collections.Concurrent;
using System.Security.Claims;
using Love.Data;
using Love.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class PinAuth(AppDb db)
{
    static readonly ConcurrentDictionary<string, (int fallos, DateTime hasta)> Bloqueos = new();

    public static bool Bloqueado(string ip) =>
        Bloqueos.TryGetValue(ip, out var b) && b.fallos >= 5 && b.hasta > DateTime.UtcNow;

    public async Task<Usuario?> Validar(string pin, string ip)
    {
        if (Bloqueado(ip)) return null;
        foreach (var u in await db.Usuarios.ToListAsync())
            if (BCrypt.Net.BCrypt.Verify(pin, u.PinHash))
            {
                Bloqueos.TryRemove(ip, out _);
                return u;
            }
        Bloqueos.AddOrUpdate(ip, (1, DateTime.UtcNow.AddMinutes(1)),
            (_, b) => b.hasta < DateTime.UtcNow ? (1, DateTime.UtcNow.AddMinutes(1)) : (b.fallos + 1, DateTime.UtcNow.AddMinutes(1)));
        return null;
    }

    public static async Task Entrar(HttpContext ctx, Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Role, u.Rol.ToString())
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(90) });
    }

    /// <summary>¿Ese PIN ya es de otra persona? (el PIN es lo único que distingue a quién entra)</summary>
    public async Task<bool> EnUso(Rol rol, string pin) =>
        (await db.Usuarios.Where(x => x.Rol != rol).ToListAsync()).Any(x => BCrypt.Net.BCrypt.Verify(pin, x.PinHash));

    public async Task CambiarPin(Rol rol, string nuevoPin)
    {
        var u = await db.Usuarios.FirstAsync(x => x.Rol == rol);
        u.PinHash = BCrypt.Net.BCrypt.HashPassword(nuevoPin);
        await db.SaveChangesAsync();
    }
}

public static class UserExt
{
    public static bool EsAdmin(this ClaimsPrincipal u) => u.IsInRole("Admin");
    /// <summary>Miranda o Aroa: su versión de la app (solo su página y los Juegos).</summary>
    public static bool EsNina(this ClaimsPrincipal u) => Ninas.De(u) != null;
    /// <summary>Página de inicio de cada uno.</summary>
    public static string Inicio(this ClaimsPrincipal u) => u.EsAdmin() ? "/Gastos" : u.EsNina() ? "/Hola" : "/Amor";
    /// <summary>Lo único que pueden abrir las niñas (además de los ficheros estáticos).</summary>
    public static bool PermitidoNina(string ruta) =>
        !ruta.StartsWith("/Juegos/Duelo", StringComparison.OrdinalIgnoreCase) &&
        (ruta == "/" || new[] { "/Hola", "/Juegos", "/Login" }.Any(p => ruta.Equals(p, StringComparison.OrdinalIgnoreCase) || ruta.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase)));
}
