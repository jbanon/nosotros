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

    /// <summary>Las de Miranda (11 años): de risa, nada de amor de pareja.</summary>
    static readonly string[] FrasesMiranda =
    [
        "Briska baila mejor que tú pero no se lo digas",
        "Los rizos rubios son muelles llenos de alegría",
        "Quien hace el tonto en el coche nunca llega aburrida",
        "Mi peluche favorito sabe todos mis secretos",
        "Un pedo a tiempo vale más que mil chistes",
        "Hoy me maquillo de tigre y salgo a rugir",
        "Las caras raras en el semáforo son puro arte",
        "Briska quiere un canal de YouTube con mil seguidores",
        "Bailar en la cocina hace que la cena sepa mejor",
        "Soy tan alta que saludo a las nubes por la mañana",
        "El pintalabios va en los labios y no en la nariz",
        "Un vídeo más y me voy a dormir dijo nadie nunca",
        "Mi perra me roba los calcetines y no se arrepiente",
        "Las tonterías en el coche se pagan con carcajadas",
        "Si me peino los rizos se ponen a protestar",
        "Hoy he bailado tanto que el suelo pide vacaciones",
        "Mis muñecos hacen una fiesta cuando apago la luz",
        "Briska se comió los deberes y no tengo pruebas",
        "Papá canta fatal pero baila todavía peor",
        "El mejor escenario del mundo es la ducha a todo volumen",
        "Los lunes deberían empezar siempre a las doce",
        "Un chiste malo bien contado es un chiste buenísimo",
        "Las cosquillas están prohibidas antes del desayuno",
        "Con purpurina hasta los martes parecen viernes",
    ];

    static string[] Lista(bool miranda) => miranda ? FrasesMiranda : Frases;

    /// <summary>Frase por defecto (de la lista fija).</summary>
    public static string Frase(Juego j, DateOnly dia, bool miranda = false)
    {
        var k = Array.IndexOf(Todos, j); var l = Lista(miranda);
        return l[(dia.DayNumber + k * 8) % l.Length];
    }

    /// <summary>Frase sorpresa para las partidas extra (tras jugar la de Javi): fija para cada día, juego y número de partida.</summary>
    public static string FraseSorpresa(Juego j, DateOnly dia, int partida, string excluir)
    {
        var lista = Frases.Where(f => f != excluir).ToArray();
        var k = Array.IndexOf(Todos, j);
        return lista[new Random(dia.DayNumber * 31 + k * 7 + partida).Next(lista.Length)];
    }

    static string ClaveFrase(Juego j, DateOnly dia) => $"frase-juego:{j.Clave}:{dia:yyyy-MM-dd}";

    /// <summary>La frase del día: la que haya escrito Javi para ese juego y día o, si no, la de la lista.
    /// Miranda tiene siempre la de su lista (las de Javi son para Raquel).</summary>
    public async Task<string> FraseDe(Juego j, DateOnly dia, bool miranda = false) =>
        miranda ? Frase(j, dia, true)
        : (await db.Configs.FindAsync(ClaveFrase(j, dia)))?.Valor is { Length: > 0 } f ? f : Frase(j, dia);

    public async Task<bool> EsPropia(Juego j, DateOnly dia, bool miranda = false) => !miranda && await db.Configs.FindAsync(ClaveFrase(j, dia)) != null;

    /// <summary>Guarda la frase de Javi para ese día (vacía o igual a la de la lista: vuelve a la de la lista).</summary>
    public async Task GuardarFrase(Juego j, DateOnly dia, string? texto)
    {
        texto = System.Text.RegularExpressions.Regex.Replace(texto ?? "", @"\s+", " ").Trim();
        var c = await db.Configs.FindAsync(ClaveFrase(j, dia));
        if (texto == "" || texto == Frase(j, dia)) { if (c != null) db.Configs.Remove(c); }
        else if (c == null) db.Configs.Add(new Config { Clave = ClaveFrase(j, dia), Valor = texto });
        else c.Valor = texto;
        await db.SaveChangesAsync();
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
