using Love.Data;
using Love.Models;
using Love.Pages.Adviento;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Vinetas;

/// <summary>La viñeta de hoy cuando llega como juego: rompecabezas, parejas, revelar, criptofrase u ordena la frase.</summary>
public class HoyModel(VinetaService vinetas, AppDb db, ConfigService config) : PageModel
{
    public VinetaDia Dia { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public bool Preview { get; set; }
    public string DatosJson { get; set; } = "{}";
    public string Firma { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Intro { get; set; } = "";
    public int Corazones { get; set; }
    public bool EsHoy => Fecha == Reloj.Hoy;
    bool EsAdmin => User.EsAdmin();
    bool Persistir => !EsAdmin;
    int Semilla => Dia.Fecha.DayNumber * 31 + Dia.VinetaId * 7 + 5;

    async Task<bool> Cargar(DateOnly? fecha)
    {
        var hoy = Reloj.Hoy;
        Preview = fecha != null && EsAdmin;
        // Raquel puede volver a cualquier día pasado (los juegos no caducan); el admin previsualiza sin guardar.
        Fecha = Preview ? fecha!.Value : (fecha is DateOnly f && f <= hoy ? f : hoy);
        var d = await vinetas.DelDia(Fecha, persistir: Persistir);
        if (d == null) return false;
        Dia = d;
        // Parejas necesita al menos 3 viñetas ya conocidas; si no las hay, el juego pasa a rompecabezas
        // (aquí, para que la página y la comprobación de la solución vean el mismo juego).
        if (Dia.Juego == JuegoVineta.Parejas)
        {
            Conocidas = (await vinetas.Desveladas(Fecha)).Select(x => x.VinetaId).Where(id => id != Dia.VinetaId).Distinct().ToList();
            if (Conocidas.Count < 3)
            {
                Dia.Juego = JuegoVineta.Rompecabezas; Dia.Estado = null;
                if (Persistir && Dia.Id != 0) await db.SaveChangesAsync();
            }
        }
        return true;
    }
    List<int> Conocidas { get; set; } = new();

    string FraseCripto => VinetaService.TieneFraseLarga(Dia.Vineta.FraseAutor, 12) ? Dia.Vineta.FraseAutor! : Dia.Vineta.FraseMia ?? Dia.Vineta.FraseAutor ?? "";
    string FraseOrdena => VinetaService.Palabras(Dia.Vineta.FraseMia) >= 4 ? Dia.Vineta.FraseMia! : Dia.Vineta.FraseAutor ?? Dia.Vineta.FraseMia ?? "";
    string[] PalabrasOrdena => FraseOrdena.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    static int[] Barajar(int n, int semilla)
    {
        var a = Enumerable.Range(0, n).ToArray();
        var rnd = new Random(semilla);
        do { for (int i = n - 1; i > 0; i--) { int j = rnd.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); } }
        while (a.Select((x, i) => x == i).All(x => x) && n > 1);
        return a;
    }

    public async Task<IActionResult> OnGet(DateOnly? fecha)
    {
        if (!await Cargar(fecha)) return Redirect("/Vinetas");
        var v = Dia.Vineta;
        if (Dia.Juego == JuegoVineta.Ninguno || (Dia.Desvelada && !Preview)) return Redirect($"/Vinetas/Ver/{v.Id}");
        if (Persistir && Dia.AbiertaEl == null) { Dia.AbiertaEl = DateTime.UtcNow; await db.SaveChangesAsync(); }
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
        Corazones = await vinetas.Corazones();
        var otras = Conocidas;
        object vista;
        switch (Dia.Juego)
        {
            case JuegoVineta.Rompecabezas:
                Titulo = "Rompecabezas"; Intro = "Toca dos piezas para intercambiarlas hasta recomponer la viñeta.";
                vista = new { n = 3, permutacion = Barajar(9, Semilla) };
                break;
            case JuegoVineta.Parejas:
                Titulo = "Encuentra las parejas"; Intro = "Son viñetas que ya conoces. Cuando las emparejes todas, aparece la de hoy.";
                var rnd = new Random(Semilla);
                otras = otras.OrderBy(_ => rnd.Next()).Take(6).ToList();
                vista = new { ids = otras, orden = Barajar(otras.Count * 2, Semilla + 1) };
                break;
            case JuegoVineta.Revelar:
                Titulo = v.Pregunta != null ? "Revelar" : "Rasca y mira"; Intro = v.Pregunta != null ? "Responde y la viñeta se aclara." : "Rasca con el dedo para descubrir la viñeta.";
                vista = new { pregunta = v.Pregunta, rascar = v.Pregunta == null };
                break;
            case JuegoVineta.Criptofrase:
                Titulo = "Criptofrase"; Intro = "Cada número es una letra. Toca una casilla y escribe. Al descifrarla, aparece la viñeta.";
                vista = Criptofrase.Vista(FraseCripto, Semilla, VinetaService.TieneFraseLarga(v.FraseAutor, 12) ? "Lo que dice la viñeta" : "Lo que quiero decirte");
                break;
            default:
                Titulo = "Ordena la frase"; Intro = "Toca las palabras en orden. Si te equivocas, vuelve a su sitio.";
                var palabras = PalabrasOrdena;
                var orden = Barajar(palabras.Length, Semilla);
                vista = new { palabras = orden.Select(i => new { i, t = palabras[i] }).ToArray(), total = palabras.Length };
                break;
        }
        if (Fecha != Reloj.Hoy && !Preview) Intro = $"Viñeta del {Fecha:d 'de' MMMM}. " + Intro;
        DatosJson = AdvientoService.Serializar(new
        {
            tipo = Dia.Juego.ToString(), vinetaId = v.Id, imagen = $"/Vinetas/Imagen/{v.Id}", resuelta = Dia.Desvelada && !Preview, estado = Dia.Estado,
            pistas = Dia.Pistas, vista, admin = EsAdmin, fecha = Fecha.ToString("yyyy-MM-dd"), letra = (string?)null
        });
        return Page();
    }

    public async Task<IActionResult> OnPostResolver([FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(fecha)) return NotFound();
        var v = Dia.Vineta;
        bool ok = Dia.Juego switch
        {
            JuegoVineta.Rompecabezas => p.Orden != null && p.Orden.Length == 9 && p.Orden.Select((x, i) => x == i).All(x => x),
            JuegoVineta.Parejas => p.Parejas,
            JuegoVineta.Revelar => v.Pregunta != null ? Criptofrase.Coincide(v.Respuesta, p.Respuesta) : p.Porcentaje >= 60,
            JuegoVineta.Criptofrase => Criptofrase.Comprobar(FraseCripto, Semilla, p.Asignacion ?? new()),
            JuegoVineta.OrdenaFrase => p.Orden != null && Criptofrase.SoloLetras(string.Join(" ", p.Orden.Where(i => i >= 0 && i < PalabrasOrdena.Length).Select(i => PalabrasOrdena[i]))) == Criptofrase.SoloLetras(FraseOrdena),
            _ => false
        };
        if (!ok) return new JsonResult(new { ok = false, mensaje = Dia.Juego switch { JuegoVineta.Revelar => "No es eso. Prueba otra vez o pide una pista.", JuegoVineta.Criptofrase => "Aún no. Alguna letra no está en su sitio.", _ => "Todavía no." } });
        if (Persistir && Dia.ResueltaEl == null)
        {
            Dia.ResueltaEl = DateTime.UtcNow; Dia.Corazones = 1;
            if (p.Estado != null) Dia.Estado = p.Estado;
            await db.SaveChangesAsync();
        }
        return new JsonResult(new { ok = true, vinetaId = v.Id, fraseMia = v.FraseMia, corazones = await vinetas.Corazones(), solucion = Dia.Juego == JuegoVineta.Criptofrase ? FraseCripto : null });
    }

    public async Task<IActionResult> OnPostPista([FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(fecha)) return NotFound();
        var v = Dia.Vineta;
        object? pista = Dia.Juego switch
        {
            JuegoVineta.Criptofrase => Criptofrase.Pista(FraseCripto, Semilla, p.Asignacion ?? new()),
            JuegoVineta.OrdenaFrase => PistaOrdena(p.Orden ?? []),
            JuegoVineta.Rompecabezas => PistaRompe(p.Orden ?? []),
            JuegoVineta.Revelar when v.Pregunta != null && Criptofrase.SoloLetras(v.Respuesta).Length > 0 => new { texto = $"Empieza por {Criptofrase.SoloLetras(v.Respuesta)[0]} y tiene {Criptofrase.SoloLetras(v.Respuesta).Length} letras" },
            _ => null
        };
        if (pista != null && Persistir) { Dia.Pistas++; await db.SaveChangesAsync(); }
        return new JsonResult(new { pista, pistas = Dia.Pistas });
    }

    object? PistaOrdena(int[] orden)
    {
        var palabras = PalabrasOrdena;
        int pos = orden.Length;
        return pos >= palabras.Length ? null : new { indice = pos, texto = palabras[pos] };
    }

    static object? PistaRompe(int[] orden)
    {
        if (orden.Length != 9 || orden.Distinct().Count() != 9) return null;
        for (int i = 0; i < 9; i++) if (orden[i] != i) return new { pos = i, con = Array.IndexOf(orden, i) };
        return null;
    }

    public async Task<IActionResult> OnPostEstado([FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(fecha)) return NotFound();
        if (Persistir && !Dia.Desvelada) { Dia.Estado = p.Estado; await db.SaveChangesAsync(); }
        return new JsonResult(new { ok = true });
    }
}
