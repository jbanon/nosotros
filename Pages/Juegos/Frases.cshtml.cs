using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Juegos;

/// <summary>Javi: frases del día de los juegos (globos, bloques, comecocos), de hoy a dos semanas vista.</summary>
public class FrasesModel(JuegosDiarios juegos) : PageModel
{
    public const int Dias = 14;
    public record Fila(DateOnly Dia, JuegosDiarios.Juego Juego, string Frase, bool Propia, string PorDefecto);
    public List<Fila> Filas { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        if (!User.EsAdmin()) return Forbid();
        var hoy = Reloj.Hoy;
        for (int i = 0; i < Dias; i++)
            foreach (var j in JuegosDiarios.Todos)
            {
                var d = hoy.AddDays(i);
                Filas.Add(new(d, j, await juegos.FraseDe(j, d), await juegos.EsPropia(j, d), JuegosDiarios.Frase(j, d)));
            }
        return Page();
    }

    public async Task<IActionResult> OnPost(string juego, DateOnly dia, string? frase)
    {
        if (!User.EsAdmin()) return Forbid();
        var j = JuegosDiarios.Buscar(juego);
        if (j == null || dia < Reloj.Hoy) return BadRequest();
        await juegos.GuardarFrase(j, dia, frase);
        TempData["Ok"] = $"Guardada la frase de {j.Nombre} del {dia:d 'de' MMMM}";
        return Redirect($"/Juegos/Frases#f-{j.Clave}-{dia:yyyyMMdd}");
    }
}
