using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Amor;

public class IndexModel(MensajeService mensajes, ConfigService config, AppDb db) : PageModel
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

        var h = Reloj.Ahora.Hour;
        Saludo = h < 6 ? "Buenas noches" : h < 13 ? "Buenos días" : h < 21 ? "Buenas tardes" : "Buenas noches";
        ViewData["Tema"] = Msg.Tema.Clave;
    }
}
