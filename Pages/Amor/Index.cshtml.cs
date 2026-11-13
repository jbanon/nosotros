using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Amor;

public class IndexModel(MensajeService mensajes, ConfigService config, AppDb db, AdvientoService adv, VinetaService vinetas, ArteService arte) : PageModel
{
    public DateOnly Fecha { get; set; }
    public MensajeDelDia Msg { get; set; } = null!;
    public Proximo? Proximo { get; set; }
    public int DiasJuntos { get; set; }
    public string Firma { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Declaracion { get; set; } = "";
    public Lectura? LecturaNueva { get; set; }
    public int NumLecturas { get; set; }
    public string Saludo { get; set; } = "";
    public bool Preview { get; set; }
    /// <summary>Texto del aviso del calendario de adviento (null si no toca mostrarlo).</summary>
    public string? Adviento { get; set; }
    public string? AdvientoSub { get; set; }
    public bool AdvientoEnlace { get; set; }
    public VinetaDia? Vineta { get; set; }
    public int VinetasDescubiertas { get; set; }
    public int JuegosPendientes { get; set; }
    public Obra? ObraSemana { get; set; }
    /// <summary>Capítulo de arte de esta semana, mientras Raquel no lo haya abierto.</summary>
    public Capitulo? CapituloSemana { get; set; }
    public int ObrasPublicadas { get; set; }

    public async Task OnGet(DateOnly? fecha)
    {
        Fecha = fecha is DateOnly f && User.EsAdmin() ? f : Reloj.Hoy;
        Preview = fecha != null && User.EsAdmin();
        Msg = await mensajes.Para(Fecha);
        Proximo = await mensajes.Proxima(Fecha);
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
        Nombre = await config.Get(ConfigClaves.NombreElla, "Raquel");
        Declaracion = await config.Get(ConfigClaves.Declaracion, ConfigClaves.DeclaracionPorDefecto);
        NumLecturas = await db.Lecturas.CountAsync(l => l.Publicada);
        LecturaNueva = await db.Lecturas.Where(l => l.Publicada && !l.Leida).OrderByDescending(l => l.Creada).FirstOrDefaultAsync();

        var aniversario = await db.FechasEspeciales.Where(x => x.AnioOrigen != null && x.Tema == "aniversario").FirstOrDefaultAsync();
        if (aniversario != null)
            DiasJuntos = Fecha.DayNumber - new DateOnly(aniversario.AnioOrigen!.Value, aniversario.Mes, aniversario.Dia).DayNumber;

        await CargarAdviento();
        if (!Preview) await vinetas.Completar(Fecha);
        Vineta = await vinetas.DelDia(Fecha, persistir: !Preview);
        VinetasDescubiertas = (await vinetas.Desveladas(Fecha)).Count;
        JuegosPendientes = (await vinetas.Pendientes(Fecha)).Count(d => d.Fecha != Fecha);
        var publicadas = await arte.Publicadas(Fecha);
        ObrasPublicadas = publicadas.Count;
        ObraSemana = publicadas.Where(p => p.fecha == ArteService.DomingoDe(Fecha) && p.obra.VistaEl == null).Select(p => p.obra).FirstOrDefault();
        var capitulos = await arte.CapitulosPublicados(Fecha);
        ObrasPublicadas += capitulos.Count;
        CapituloSemana = capitulos.Where(p => p.fecha == ArteService.DomingoDe(Fecha) && p.cap.VistoEl == null).Select(p => p.cap).FirstOrDefault();

        var h = Reloj.Ahora.Hour;
        Saludo = h < 6 ? "Buenas noches" : h < 13 ? "Buenos días" : h < 21 ? "Buenas tardes" : "Buenas noches";
        ViewData["Tema"] = Msg.Tema.Clave;
    }

    async Task CargarAdviento()
    {
        var anio = AdvientoService.AnioDe(Fecha);
        var casillas = await adv.Casillas(anio);
        if (casillas.Count == 0) return;
        var inicio = new DateOnly(anio, 12, 1);
        int faltan = inicio.DayNumber - Fecha.DayNumber;
        if (faltan > 0 && faltan <= 7) { Adviento = "Algo se está preparando…"; AdvientoSub = faltan == 1 ? "Mañana se abre la primera casilla" : $"Faltan {faltan} días para el 1 de diciembre"; return; }
        if (!AdvientoService.EnTemporada(Fecha) && faltan > 0) return;
        AdvientoEnlace = true;
        int total = casillas.Count(c => c.DaLetra), letras = casillas.Count(c => c.DaLetra && c.Resuelta);
        var hoy = casillas.FirstOrDefault(c => AdvientoService.FechaDe(c) == Fecha);
        var pendientes = casillas.Where(c => AdvientoService.Abierta(c, Fecha) && c.AbiertaEl == null).Select(c => c.Dia).ToList();
        if (hoy != null && hoy.AbiertaEl == null) { Adviento = $"Hoy toca abrir la casilla {hoy.Dia}"; AdvientoSub = $"Calendario de adviento · {letras} de {total} letras"; }
        else if (pendientes.Count > 0) { Adviento = $"Tienes {pendientes.Count} casilla{(pendientes.Count == 1 ? "" : "s")} sin abrir"; AdvientoSub = $"Calendario de adviento · {letras} de {total} letras"; }
        else { Adviento = "Calendario de adviento"; AdvientoSub = letras == total ? "¡Todas las letras conseguidas!" : $"{letras} de {total} letras · las abiertas siguen ahí"; }
    }

    public static string NombreJuego(JuegoVineta j) => j switch
    {
        JuegoVineta.Rompecabezas => "Un rompecabezas", JuegoVineta.Parejas => "Encuentra las parejas", JuegoVineta.Revelar => "Algo que revelar",
        JuegoVineta.Criptofrase => "Una criptofrase", JuegoVineta.OrdenaFrase => "Ordena la frase", _ => "Un juego"
    };
}
