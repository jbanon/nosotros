using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Frase del día de Miranda (11 años): el agente las escribe de 14 en 14 y se guardan en Config
/// (<c>miranda-frase:yyyy-MM-dd</c>). Si falta la de un día, sale una de la lista de reserva.
/// </summary>
public class MirandaService(AppDb db, AgenteService agente)
{
    public static readonly DateOnly Nacimiento = new(2015, 3, 10);
    const string Prefijo = "miranda-frase:";
    public const int Tanda = 14;

    public static string Clave(DateOnly d) => $"{Prefijo}{d:yyyy-MM-dd}";

    static readonly string[] Reserva =
    [
        "Hoy es un gran día para bailar en el pasillo como si nadie te viera. Briska te ve, pero no se chiva.",
        "Dato científico: los rizos rubios son muelles de alegría. Por eso nunca se están quietos.",
        "Briska quiere saber cuándo subes un tutorial de maquillaje para perras. Está muy interesada.",
        "Aviso a los coches de alrededor: en el asiento de atrás viaja la mejor cómica del país.",
        "Ser alta tiene ventajas: eres la primera en enterarte de si llueve.",
        "Los muñecos de tu cuarto han votado en secreto. Has ganado: persona favorita del año.",
        "Un pedo a tiempo es una carcajada asegurada. En el ascensor, no, por favor.",
        "Si YouTube supiera lo graciosa que eres, te pagaría a ti por verlo.",
        "Consejo de maquillaje de papá: el pintalabios va en los labios. Lo sé, soy un experto.",
        "Pregunta seria: ¿los rizos se peinan solos por la noche o se van de fiesta?",
        "Récord mundial que te pertenece: más caras raras por minuto en un semáforo.",
        "Briska dice que, si le das tu bocadillo, te enseña un paso de baile. Miente: solo sabe sentarse.",
        "Un día serás muy mayor y muy seria. Hoy no es ese día.",
        "Si las tonterías en el coche dieran puntos, ya tendrías el carné de conducir.",
        "Cuenta hoy un chiste malo. Si se ríen, era bueno; si no, era malísimo, que es todavía mejor.",
        "Lo bueno de tener rizos: si te levantas despeinada, parece que lo has hecho a propósito.",
        "Misión del día: hacer reír a alguien que esté de mal humor. Tienes el superpoder, úsalo.",
        "Bailar en la cocina tiene una ventaja: nadie te pide el carné de bailarina.",
        "Hoy es buen día para inventar un baile nuevo y ponerle un nombre ridículo.",
        "Si algo sale mal, pon cara de pato. No arregla nada, pero es más divertido.",
        "Un estornudo, un hipo y un pedo entran en un bar… El final te lo inventas tú.",
        "La gente alta ve el mundo desde arriba. Por eso tus chistes llegan más lejos.",
    ];

    public static string FraseReserva(DateOnly d) => Reserva[d.DayNumber % Reserva.Length];

    public async Task<string> FraseDe(DateOnly d) =>
        (await db.Configs.FindAsync(Clave(d)))?.Valor is { Length: > 0 } f ? f : FraseReserva(d);

    /// <summary>Javi la cambia a mano (vacía: vuelve a la de reserva).</summary>
    public async Task Guardar(DateOnly d, string? texto)
    {
        texto = (texto ?? "").Trim();
        var c = await db.Configs.FindAsync(Clave(d));
        if (texto == "") { if (c != null) db.Configs.Remove(c); }
        else if (c == null) db.Configs.Add(new Config { Clave = Clave(d), Valor = texto });
        else c.Valor = texto;
        await db.SaveChangesAsync();
    }

    /// <summary>Las últimas frases escritas, para que el agente no se repita.</summary>
    public static Task<List<string>> Anteriores(AppDb db, int n, CancellationToken ct = default) =>
        db.Configs.Where(c => c.Clave.StartsWith(Prefijo)).OrderByDescending(c => c.Clave).Take(n).Select(c => c.Valor).ToListAsync(ct);

    public Task<bool> Preparando() =>
        db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.FrasesMiranda && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso));

    /// <summary>Si en los próximos 7 días falta alguna, encarga una tanda desde la primera que falte (lo llama el programador cada hora).</summary>
    public async Task Asegurar(DateOnly hoy)
    {
        if (!agente.Disponible || await Preparando()) return;
        if (!await db.Usuarios.AnyAsync(u => u.Rol == Rol.Miranda)) return;
        var desde = DateTime.UtcNow.AddHours(-6);
        if (await db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.FrasesMiranda && t.Estado == EstadoTarea.Error && t.Creada > desde)) return;
        var claves = Enumerable.Range(0, 7).Select(i => Clave(hoy.AddDays(i))).ToList();
        var hay = await db.Configs.Where(c => claves.Contains(c.Clave)).Select(c => c.Clave).ToListAsync();
        var primera = Enumerable.Range(0, 7).Select(i => hoy.AddDays(i)).FirstOrDefault(d => !hay.Contains(Clave(d)));
        if (primera == default) return;
        await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.FrasesMiranda, Entrada = primera.ToString("yyyy-MM-dd"), Notas = Tanda.ToString(), Autor = "programador" });
    }
}

/// <summary>Cada hora comprueba si faltan frases de Miranda para la semana que viene.</summary>
public class MirandaProgramador(IServiceScopeFactory scopes, ILogger<MirandaProgramador> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(3), ct); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<MirandaService>().Asegurar(Reloj.Hoy);
            }
            catch (Exception e) { log.LogError(e, "No se pudieron programar las frases de Miranda"); }
        }
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
    }
}
