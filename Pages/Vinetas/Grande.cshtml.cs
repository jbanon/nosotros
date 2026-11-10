using Love.Data;
using Love.Models;
using Love.Pages.Adviento;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Vinetas;

/// <summary>Desbloqueo: rompecabezas 4×4 con cualquier viñeta del álbum. No guarda nada.</summary>
public class GrandeModel(AppDb db, VinetaService vinetas) : PageModel
{
    public Vineta V { get; set; } = null!;
    public string DatosJson { get; set; } = "{}";
    const int N = 4;

    async Task<bool> Cargar(int id)
    {
        if (!await vinetas.PuedeVer(id, Reloj.Hoy, User.EsAdmin())) return false;
        if (!(await vinetas.Logros()).First(l => l.Clave == "grande").Conseguido && !User.EsAdmin()) return false;
        var v = await db.Vinetas.FindAsync(id);
        if (v == null) return false;
        V = v; return true;
    }

    static int[] Barajar(int n, int semilla)
    {
        var a = Enumerable.Range(0, n).ToArray();
        var rnd = new Random(semilla);
        for (int i = n - 1; i > 0; i--) { int j = rnd.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
        return a;
    }

    public async Task<IActionResult> OnGet(int id)
    {
        if (!await Cargar(id)) return Redirect("/Vinetas");
        DatosJson = AdvientoService.Serializar(new { tipo = "Rompecabezas", vinetaId = V.Id, imagen = $"/Vinetas/Imagen/{V.Id}", resuelta = false, estado = (string?)null, pistas = 0, vista = new { n = N, permutacion = Barajar(N * N, Environment.TickCount) }, admin = true, fecha = (string?)null });
        return Page();
    }

    public async Task<IActionResult> OnPostResolver(int id, [FromBody] PeticionJuego p)
    {
        if (!await Cargar(id)) return NotFound();
        var ok = p.Orden != null && p.Orden.Length == N * N && p.Orden.Select((x, i) => x == i).All(x => x);
        return new JsonResult(new { ok, mensaje = ok ? null : "Todavía no." });
    }

    public async Task<IActionResult> OnPostPista(int id, [FromBody] PeticionJuego p)
    {
        if (!await Cargar(id)) return NotFound();
        var orden = p.Orden ?? [];
        if (orden.Length != N * N) return new JsonResult(new { pista = (object?)null });
        for (int i = 0; i < orden.Length; i++) if (orden[i] != i) return new JsonResult(new { pista = new { pos = i, con = Array.IndexOf(orden, i) } });
        return new JsonResult(new { pista = (object?)null });
    }

    public IActionResult OnPostEstado(int id) => new JsonResult(new { ok = true });
}
