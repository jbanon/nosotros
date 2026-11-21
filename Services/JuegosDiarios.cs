using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>Juegos del adviento que se quedan en la zona Juegos: una partida al día por juego y persona,
/// con una frase del día distinta para cada juego. Se desbloquean cuando llega su casilla del adviento.</summary>
public class JuegosDiarios(AppDb db)
{
    public record Juego(string Clave, string Nombre, string Emoji, string Tipo, string Descripcion);

    public static readonly Juego[] Todos =
    [
        new("globos", "Globos", "🎈", "Globos", "Explota globos, caza las palabras de los corazones y escribe la frase."),
        new("arkanoid", "Bloques", "🧱", "Arkanoid", "Tira los bloques y dale a BriskaBird para que suelte palabras."),
        new("comecocos", "Comecocos", "🦘", "Comecocos", "Recorre el laberinto y alcanza a Briskanguro para que suelte palabras."),
    ];

    /// <summary>Juegos ya abiertos en Juegos sin esperar a su casilla del adviento (Javier, 2026-11-21: el Arkanoid).</summary>
    static readonly string[] Activos = ["arkanoid"];

    public static Juego? Buscar(string? clave) => Todos.FirstOrDefault(j => j.Clave == clave);

    /// <summary>Frases del día: graciosas y con algo de clase. Cada juego toma una distinta cada día.</summary>
    static readonly string[] Frases =
    [
        "Contigo hasta los lunes tienen su punto de gracia",
        "Eres mi plan favorito incluso los días sin plan",
        "Te quiero más que Briska a una croqueta caída",
        "Me robaste el corazón y no pienso denunciarte",
        "Si me pierdo, búscame donde estés tú",
        "Prometo quererte incluso cuando me robes la manta",
        "Eres la razón por la que sonrío mirando el móvil",
        "Daría saltos de canguro solo por verte sonreír",
        "Contigo el sofá es el mejor destino del mundo",
        "Tu risa es mi canción favorita y nunca se acaba",
        "Nadie me pone tan tonto como tú, y me encanta",
        "Eres mi persona favorita para no hacer nada",
        "Te elegiría otra vez aunque me tocara fregar siempre",
        "Si fueras croqueta no llegarías ni al plato",
        "Contigo hasta la lluvia del norte parece bonita",
        "Mi sitio favorito sigue siendo tu abrazo de sofá",
        "Eres el mejor mensaje que me ha llegado nunca",
        "Te quiero aunque me ganes siempre a las cifras",
        "Ojalá todos los días empezaran con tus buenos días",
        "Tienes la llave de casa y también la del corazón",
        "Envejecer contigo me parece un planazo",
        "Eres la casualidad más bonita de aquel chat",
        "Contigo la vida es más fácil y mucho más divertida",
        "Me haces reír hasta cuando me robas las patatas",
    ];

    public static string Frase(Juego j, DateOnly dia)
    {
        var k = Array.IndexOf(Todos, j);
        return Frases[(dia.DayNumber + k * 8) % Frases.Length];
    }

    /// <summary>null si se puede jugar; si no, desde cuándo (o "pronto" si el juego aún no existe).</summary>
    public async Task<DateOnly?> BloqueadoHasta(Juego j, DateOnly hoy)
    {
        if (!Enum.TryParse<TipoCasilla>(j.Tipo, out var tipo)) return DateOnly.MaxValue;
        if (Activos.Contains(j.Clave)) return null;
        var anio = AdvientoService.AnioDe(hoy);
        var c = await db.Adviento.Where(x => x.Anio == anio && x.Tipo == tipo).OrderBy(x => x.Dia).FirstOrDefaultAsync();
        return c == null || AdvientoService.Abierta(c, hoy) ? null : AdvientoService.FechaDe(c);
    }

    public Task<JuegoPartida?> DeHoy(int usuario, Juego j, DateOnly hoy) =>
        db.JuegoPartidas.Where(p => p.UsuarioId == usuario && p.Juego == j.Clave && p.Fecha == hoy).OrderByDescending(p => p.Id).FirstOrDefaultAsync();
}
