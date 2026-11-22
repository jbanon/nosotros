using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Frase del día de cada niña (<see cref="Ninas"/>): el agente las escribe de 14 en 14 y se guardan en Config
/// (<c>{slug}-frase:yyyy-MM-dd</c>). Si falta la de un día, sale una de su lista de reserva.
/// </summary>
public class NinasService(AppDb db, AgenteService agente)
{
    public const int Tanda = 14;

    static string Prefijo(Nina n) => $"{n.Slug}-frase:";
    public static string Clave(Nina n, DateOnly d) => $"{Prefijo(n)}{d:yyyy-MM-dd}";

    public static string FraseReserva(Nina n, DateOnly d) => n.Reserva[d.DayNumber % n.Reserva.Length];

    public async Task<string> FraseDe(Nina n, DateOnly d) =>
        (await db.Configs.FindAsync(Clave(n, d)))?.Valor is { Length: > 0 } f ? f : FraseReserva(n, d);

    /// <summary>Javi la cambia a mano (vacía: vuelve a la de reserva).</summary>
    public async Task Guardar(Nina n, DateOnly d, string? texto)
    {
        texto = (texto ?? "").Trim();
        var c = await db.Configs.FindAsync(Clave(n, d));
        if (texto == "") { if (c != null) db.Configs.Remove(c); }
        else if (c == null) db.Configs.Add(new Config { Clave = Clave(n, d), Valor = texto });
        else c.Valor = texto;
        await db.SaveChangesAsync();
    }

    /// <summary>Las últimas frases escritas, para que el agente no se repita.</summary>
    public static Task<List<string>> Anteriores(AppDb db, Nina n, int cuantas, CancellationToken ct = default)
    {
        var p = Prefijo(n);
        return db.Configs.Where(c => c.Clave.StartsWith(p)).OrderByDescending(c => c.Clave).Take(cuantas).Select(c => c.Valor).ToListAsync(ct);
    }

    /// <summary>Notas de la tarea del agente: "slug|cuántas" (las primeras de Miranda eran solo "cuántas").</summary>
    public static (Nina nina, int n) LeerNotas(string? notas)
    {
        var p = (notas ?? "").Split('|');
        if (p.Length == 2) return (Ninas.PorSlug(p[0]) ?? Ninas.Miranda, int.TryParse(p[1], out var a) ? a : Tanda);
        return (Ninas.Miranda, int.TryParse(p[0], out var b) ? b : Tanda);
    }

    public Task<bool> Preparando(Nina n)
    {
        var notas = $"{n.Slug}|";
        return db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.FrasesNina && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso)
            && (t.Notas!.StartsWith(notas) || (n.Slug == "miranda" && !t.Notas!.Contains('|'))));
    }

    /// <summary>Si a alguna niña le falta alguna frase de los próximos 7 días, encarga una tanda (de una en una; lo llama el programador cada hora).</summary>
    public async Task Asegurar(DateOnly hoy)
    {
        if (!agente.Disponible) return;
        if (await db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.FrasesNina && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso))) return;
        var desde = DateTime.UtcNow.AddHours(-6);
        if (await db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.FrasesNina && t.Estado == EstadoTarea.Error && t.Creada > desde)) return;
        foreach (var n in Ninas.Todas)
        {
            var rol = n.Rol;
            if (!await db.Usuarios.AnyAsync(u => u.Rol == rol)) continue;
            var claves = Enumerable.Range(0, 7).Select(i => Clave(n, hoy.AddDays(i))).ToList();
            var hay = await db.Configs.Where(c => claves.Contains(c.Clave)).Select(c => c.Clave).ToListAsync();
            var primera = Enumerable.Range(0, 7).Select(i => hoy.AddDays(i)).FirstOrDefault(d => !hay.Contains(Clave(n, d)));
            if (primera == default) continue;
            await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.FrasesNina, Entrada = primera.ToString("yyyy-MM-dd"), Notas = $"{n.Slug}|{Tanda}", Autor = "programador" });
            return;
        }
    }
}

/// <summary>Cada hora comprueba si faltan frases de las niñas para la semana que viene.</summary>
public class NinasProgramador(IServiceScopeFactory scopes, ILogger<NinasProgramador> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(3), ct); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(20));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NinasService>().Asegurar(Reloj.Hoy);
            }
            catch (Exception e) { log.LogError(e, "No se pudieron programar las frases de las niñas"); }
        }
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
    }
}
