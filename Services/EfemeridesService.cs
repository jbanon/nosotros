using System.Text.Json.Nodes;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Efemérides del día ("tal día como hoy"): el agente escribe tres, graciosas o muy llamativas, para cada día del año.
/// Se guardan por mes y día y se reutilizan los años siguientes.
/// </summary>
public class EfemeridesService(AppDb db, AgenteService agente)
{
    public record Item(string Anio, string Emoji, string Titular, string Resumen, string Articulo, string Fuente);

    public static string Clave(DateOnly d) => $"{d.Month:00}-{d.Day:00}";

    public async Task<List<Item>> Del(DateOnly d)
    {
        var e = await db.Efemerides.FirstOrDefaultAsync(x => x.Mes == d.Month && x.Dia == d.Day);
        return e == null ? new() : Items(e);
    }

    public static List<Item> Items(Efemeride e) =>
        FrancesService.Lista(FrancesService.Json(e.Contenido), "items").Select(i => new Item(
            FrancesService.Txt(i, "anio"), FrancesService.Txt(i, "emoji"), FrancesService.Txt(i, "titular"),
            FrancesService.Txt(i, "resumen"), FrancesService.Txt(i, "articulo"), FrancesService.Txt(i, "fuente"))).ToList();

    public Task<bool> Preparando(DateOnly d)
    {
        var k = Clave(d);
        return db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.Efemerides && t.Entrada == k && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso));
    }

    /// <summary>Encarga las de hoy y las de mañana si faltan (lo llama el programador cada hora).</summary>
    public async Task Asegurar(DateOnly hoy)
    {
        if (!agente.Disponible) return;
        // Tras un fallo, no reintentar en bucle: se espera unas horas
        var desde = DateTime.UtcNow.AddHours(-6);
        foreach (var d in new[] { hoy, hoy.AddDays(1) })
        {
            var k = Clave(d);
            if (await db.Efemerides.AnyAsync(x => x.Mes == d.Month && x.Dia == d.Day) || await Preparando(d)) continue;
            if (await db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.Efemerides && t.Entrada == k && t.Estado == EstadoTarea.Error && t.Creada > desde)) continue;
            await Encargar(d, "programador");
            return;   // de una en una
        }
    }

    public Task<TareaAgente> Encargar(DateOnly d, string autor) =>
        agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.Efemerides, Entrada = Clave(d), Autor = autor });

    /// <summary>Javi: borra las del día y las vuelve a encargar.</summary>
    public async Task Rehacer(DateOnly d, string autor)
    {
        if (await Preparando(d)) return;
        await db.Efemerides.Where(x => x.Mes == d.Month && x.Dia == d.Day).ExecuteDeleteAsync();
        await Encargar(d, autor);
    }
}

/// <summary>Cada hora comprueba si faltan las efemérides de hoy o de mañana.</summary>
public class EfemeridesProgramador(IServiceScopeFactory scopes, ILogger<EfemeridesProgramador> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), ct); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<EfemeridesService>().Asegurar(Reloj.Hoy);
            }
            catch (Exception e) { log.LogError(e, "No se pudieron programar las efemérides"); }
        }
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
    }
}
