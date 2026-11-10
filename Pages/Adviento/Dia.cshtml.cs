using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Adviento;

/// <summary>Lo que manda el navegador al resolver, pedir pista o guardar progreso.</summary>
public class PeticionJuego
{
    public Dictionary<string, string>? Asignacion { get; set; }
    public Dictionary<string, string>? Celdas { get; set; }
    public int[]? Eleccion { get; set; }
    public string? Respuesta { get; set; }
    public string? Estado { get; set; }
    /// <summary>Juegos de viñetas: permutación del rompecabezas o índices de "ordena la frase".</summary>
    public int[]? Orden { get; set; }
    public bool Parejas { get; set; }
    public int Porcentaje { get; set; }
}

public class DiaModel(AdvientoService adv, AppDb db, ConfigService config) : PageModel
{
    public AdvientoCasilla Casilla { get; set; } = null!;
    public List<AdvientoCasilla> Todas { get; set; } = new();
    public DateOnly Hoy { get; set; }
    public bool Preview { get; set; }
    public bool Cerrada { get; set; }
    public string MensajeCerrada { get; set; } = "";
    public int Letras { get; set; }
    public int Total { get; set; }
    public int Posicion { get; set; }
    public string DatosJson { get; set; } = "{}";
    public List<List<(string? letra, int dia)>> FraseFinal { get; set; } = new();
    public List<AdvientoCasilla> Fotos { get; set; } = new();
    public string Firma { get; set; } = "";
    public Vineta? VinetaRegalo { get; set; }
    bool EsAdmin => User.EsAdmin();

    async Task<bool> Cargar(int dia, DateOnly? fecha)
    {
        Preview = fecha != null && EsAdmin;
        Hoy = Preview ? fecha!.Value : Reloj.Hoy;
        var anio = AdvientoService.AnioDe(Hoy);
        Todas = await adv.Casillas(anio);
        var c = Todas.FirstOrDefault(x => x.Dia == dia);
        if (c == null) return false;
        Casilla = c;
        Cerrada = !AdvientoService.Abierta(c, Hoy) && !(EsAdmin && !Preview);
        Total = Todas.Count(x => x.DaLetra);
        Letras = Todas.Count(x => x.DaLetra && x.Resuelta);
        Posicion = Todas.Where(x => x.DaLetra).OrderBy(x => x.Dia).ToList().FindIndex(x => x.Dia == dia) + 1;
        return true;
    }

    public async Task<IActionResult> OnGet(int dia, DateOnly? fecha)
    {
        if (!await Cargar(dia, fecha)) return NotFound();
        ViewData["Tema"] = "navidad";
        var c = Casilla;
        if (Cerrada)
        {
            MensajeCerrada = AdvientoService.MensajesCerrada[(c.Dia * 5 + Hoy.Day) % AdvientoService.MensajesCerrada.Length];
            return Page();
        }
        if (!EsAdmin)
        {
            bool cambios = false;
            if (c.AbiertaEl == null) { c.AbiertaEl = DateTime.UtcNow; cambios = true; }
            // Fotos sin pregunta y vales: la letra (si la hay) se gana al abrir.
            bool sinReto = c.Tipo == TipoCasilla.Foto && string.IsNullOrWhiteSpace(c.Pregunta) || c.Tipo == TipoCasilla.Vale || c.Tipo == TipoCasilla.Final;
            if (sinReto && c.ResueltaEl == null) { c.ResueltaEl = DateTime.UtcNow; cambios = true; Letras = Todas.Count(x => x.DaLetra && x.Resuelta); }
            if (cambios) await db.SaveChangesAsync();
        }
        if (c.VinetaId != null)
        {
            VinetaRegalo = await db.Vinetas.FindAsync(c.VinetaId.Value);
            if (VinetaRegalo != null && c.Resuelta && !EsAdmin && VinetaRegalo.MostradaEl == null) { VinetaRegalo.MostradaEl = Hoy; await db.SaveChangesAsync(); }
        }
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
        if (c.Tipo == TipoCasilla.Final)
        {
            var frase = await config.Get(ConfigClaves.AdvientoFrase, "");
            FraseFinal = AdvientoService.FraseFinal(frase, Todas);
            Fotos = Todas.Where(x => x.FotoPath != null && AdvientoService.Abierta(x, Hoy)).OrderBy(x => x.Dia).ToList();
        }
        object vista = c.Tipo switch
        {
            TipoCasilla.Criptofrase => adv.VistaCripto(c),
            TipoCasilla.Crucigrama => adv.VistaCruci(c),
            TipoCasilla.Murdoku => adv.VistaMurdoku(c),
            TipoCasilla.Foto => new { pregunta = c.Pregunta },
            _ => new { }
        };
        object? solucion = !c.Resuelta ? null : c.Tipo switch
        {
            TipoCasilla.Criptofrase => adv.SolucionCripto(c),
            TipoCasilla.Crucigrama => adv.SolucionCruci(c),
            TipoCasilla.Murdoku => new { eleccion = AdvientoService.Json(c)["solucion"], explicacion = adv.ExplicacionMurdoku(c) },
            TipoCasilla.Foto => c.Respuesta,
            _ => null
        };
        DatosJson = AdvientoService.Serializar(new
        {
            dia = c.Dia, tipo = c.Tipo.ToString(), letra = c.Letra, resuelta = c.Resuelta, posicion = Posicion, letras = Letras, total = Total,
            estado = c.Estado, pistas = c.Pistas, vista, solucion, admin = EsAdmin, fecha = Preview ? Hoy.ToString("yyyy-MM-dd") : null
        });
        return Page();
    }

    public async Task<IActionResult> OnPostResolver(int dia, [FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(dia, fecha) || Cerrada) return NotFound();
        var c = Casilla;
        bool ok = c.Tipo switch
        {
            TipoCasilla.Criptofrase => adv.ComprobarCripto(c, p.Asignacion ?? new()),
            TipoCasilla.Crucigrama => adv.ComprobarCruci(c, p.Celdas ?? new()),
            TipoCasilla.Murdoku => adv.ComprobarMurdoku(c, p.Eleccion ?? []),
            TipoCasilla.Foto => adv.ComprobarFoto(c, p.Respuesta ?? ""),
            _ => false
        };
        if (!ok) return new JsonResult(new { ok = false, mensaje = MensajeFallo(c) });
        if (!EsAdmin && c.ResueltaEl == null)
        {
            c.ResueltaEl = DateTime.UtcNow;
            if (p.Estado != null) c.Estado = p.Estado;
            if (c.VinetaId != null && await db.Vinetas.FindAsync(c.VinetaId.Value) is { } vr && vr.MostradaEl == null) vr.MostradaEl = Hoy;
            await db.SaveChangesAsync();
            Letras = Todas.Count(x => x.DaLetra && x.Resuelta);
        }
        object? solucion = c.Tipo switch
        {
            TipoCasilla.Criptofrase => adv.SolucionCripto(c),
            TipoCasilla.Crucigrama => adv.SolucionCruci(c),
            TipoCasilla.Murdoku => new { eleccion = AdvientoService.Json(c)["solucion"], explicacion = adv.ExplicacionMurdoku(c) },
            TipoCasilla.Foto => c.Respuesta,
            _ => null
        };
        return new JsonResult(new { ok = true, letra = c.Letra, posicion = Posicion, letras = Letras, total = Total, solucion });
    }

    static string MensajeFallo(AdvientoCasilla c) => c.Tipo switch
    {
        TipoCasilla.Murdoku => "Mmm, no. Alguien tiene coartada. Revisa las pistas.",
        TipoCasilla.Crucigrama => "Casi. Hay alguna letra que no encaja.",
        TipoCasilla.Foto => "No es eso. Mira bien la foto… o pide una pista.",
        _ => "Aún no. Alguna letra no está en su sitio.",
    };

    public async Task<IActionResult> OnPostPista(int dia, [FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(dia, fecha) || Cerrada) return NotFound();
        var c = Casilla;
        object? pista = c.Tipo switch
        {
            TipoCasilla.Criptofrase => adv.PistaCripto(c, p.Asignacion ?? new()),
            TipoCasilla.Crucigrama => adv.PistaCruci(c, p.Celdas ?? new()),
            TipoCasilla.Murdoku => adv.PistaMurdoku(c, p.Eleccion ?? []),
            TipoCasilla.Foto => adv.PistaFoto(c),
            _ => null
        };
        if (pista != null && !EsAdmin) { c.Pistas++; await db.SaveChangesAsync(); }
        return new JsonResult(new { pista, pistas = c.Pistas });
    }

    public async Task<IActionResult> OnPostEstado(int dia, [FromBody] PeticionJuego p, DateOnly? fecha)
    {
        if (!await Cargar(dia, fecha) || Cerrada) return NotFound();
        if (!EsAdmin && !Casilla.Resuelta) { Casilla.Estado = p.Estado; await db.SaveChangesAsync(); }
        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostCanjear(int dia, DateOnly? fecha)
    {
        if (!await Cargar(dia, fecha) || Cerrada) return NotFound();
        if (!EsAdmin && Casilla.Tipo == TipoCasilla.Vale && Casilla.CanjeadoEl == null) { Casilla.CanjeadoEl = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return Redirect($"/Adviento/Dia/{dia}" + (Preview ? $"?fecha={Hoy:yyyy-MM-dd}" : ""));
    }
}
