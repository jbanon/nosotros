using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public record MensajeDelDia(string Texto, string? Titulo, TipoMensaje Tipo, FechaEspecial? Especial, Tema Tema, string? Contador);
public record Proximo(FechaEspecial Fecha, int DiasFaltan);

public class MensajeService(AppDb db)
{
    public async Task<MensajeDelDia> Para(DateOnly fecha)
    {
        var especial = await db.FechasEspeciales
            .FirstOrDefaultAsync(f => f.Activo && f.Mes == fecha.Month && f.Dia == fecha.Day);

        var tema = especial?.Tema != null && TemaService.Temas.ContainsKey(especial.Tema)
            ? TemaService.Temas[especial.Tema]
            : TemaService.PorFecha(fecha);

        if (especial != null)
        {
            string? contador = null;
            if (especial.AnioOrigen is int a && !string.IsNullOrWhiteSpace(especial.TextoContador))
                contador = especial.TextoContador.Replace("{n}", (fecha.Year - a).ToString());
            return new(especial.Texto, especial.Nombre, TipoMensaje.Frase, especial, tema, contador);
        }

        var m = await db.Mensajes.FirstOrDefaultAsync(x => x.Activo && x.Mes == fecha.Month && x.Dia == fecha.Day)
             ?? await db.Mensajes.Where(x => x.Activo && x.Mes == fecha.Month).OrderBy(x => x.Dia).FirstOrDefaultAsync()
             ?? new Mensaje { Texto = "Te quiero. Hoy también.", Tipo = TipoMensaje.Frase };
        return new(m.Texto, m.Titulo, m.Tipo, null, tema, null);
    }

    /// <summary>Próxima fecha especial con cuenta atrás, si falta ≤ maxDias (y no es hoy).</summary>
    public async Task<Proximo?> Proxima(DateOnly hoy, int maxDias = 30)
    {
        var fechas = await db.FechasEspeciales.Where(f => f.Activo && f.CuentaAtras).ToListAsync();
        Proximo? mejor = null;
        foreach (var f in fechas)
        {
            if (!DateOnly.TryParse($"{hoy.Year}-{f.Mes:00}-{f.Dia:00}", out var d)) continue;
            if (d < hoy) d = d.AddYears(1);
            int faltan = d.DayNumber - hoy.DayNumber;
            if (faltan > 0 && faltan <= maxDias && (mejor == null || faltan < mejor.DiasFaltan))
                mejor = new(f, faltan);
        }
        return mejor;
    }
}
