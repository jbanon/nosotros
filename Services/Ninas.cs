using System.Security.Claims;
using Love.Models;

namespace Love.Services;

/// <summary>
/// Las niñas que tienen su propia versión de la app (Miranda desde 2026-11-21, Aroa desde 2026-11-22): solo su página
/// con la frase del día y los Juegos. Aquí va su perfil (para el agente), sus frases de los juegos y las de reserva.
/// </summary>
public record Nina(Rol Rol, string Nombre, string Slug, DateOnly? Nacimiento, int Edad, string Firma, string Perfil,
                   string[] FrasesJuegos, string[] Reserva, int OffsetCruci)
{
    public int EdadEn(DateOnly d) => Nacimiento is DateOnly n ? d.Year - n.Year - (d < n.AddYears(d.Year - n.Year) ? 1 : 0) : Edad;
    public bool EsCumple(DateOnly d) => Nacimiento is DateOnly n && n.Month == d.Month && n.Day == d.Day;
}

public static class Ninas
{
    public static readonly Nina Miranda = new(Rol.Miranda, "Miranda", "miranda", new DateOnly(2015, 3, 10), 11, "Papá",
        "Miranda es la hija de Javi, que es quien firma («Papá»). Nació el 10 de marzo de 2015. Es alta y delgada, rubia y con el pelo rizado. " +
        "Le encanta bailar, le gusta el maquillaje y todavía juega con muñecos. Ve muchos vídeos de YouTube (más tonterías de las que a su padre le gustaría). " +
        "Es muy graciosa y divertida: a veces le da por hacer el tonto en el coche y todos se parten, y se tira pedos de cachondeo. En casa hay una perra, Briska. " +
        "Guiños posibles: baile, maquillaje, muñecos, YouTube, el coche, Briska, su pelo, lo alta que es, los pedos.",
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
        ],
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
        ],
        1_000_000_000);

    public static readonly Nina Aroa = new(Rol.Aroa, "Aroa", "aroa", null, 10, "Javi",
        "Aroa tiene 10 años. Le encantan las guerreras del k-pop: las canciones pegadizas, las coreografías, los escenarios con luces, " +
        "los trajes brillantes y la idea de salvar el mundo cantando. En casa hay una perra, Briska. Firma las frases Javi. " +
        "Guiños posibles: coreografías, ensayos, conciertos, fans, micrófonos, luces, poderes de guerrera, Briska; sin citar personajes concretos.",
        [
            "Una guerrera del pop nunca sale sin su micrófono",
            "Briska ensaya la coreografía pero siempre va tarde",
            "Mi superpoder secreto es cantar en la ducha",
            "Con un buen estribillo se derrota a cualquier monstruo",
            "Las luces del escenario se encienden cuando llego yo",
            "Hoy ensayo la coreografía hasta que el suelo brille",
            "Los deberes huyen cuando escuchan mi canción",
            "Briska quiere ser la mascota oficial de la gira",
            "Un buen grito de guerra arregla cualquier lunes",
            "Mi peine es un micrófono y mi cama un escenario",
            "Las fans gritan mi nombre aunque sea en el pasillo",
            "Para salvar el mundo primero hay que desayunar",
            "Si la canción se pega es que ha funcionado",
            "Mis zapatillas brillan más que una estrella fugaz",
            "Bailo tan rápido que mi sombra no me alcanza",
            "Cada concierto empieza con un buen calentamiento",
            "Briska aplaude con la cola cuando termino de cantar",
            "La purpurina es el uniforme oficial de las guerreras",
            "Hoy toca ensayo general en el salón de casa",
            "Con mis amigas formamos el grupo más ruidoso",
            "El estribillo más pegadizo lo invento en el baño",
            "Las tablas de multiplicar también tienen ritmo",
            "Un monstruo no aguanta ni dos minutos de baile",
            "Mi mejor paso de baile lo inventé tropezando",
        ],
        [
            "Aviso para monstruos: hoy Aroa ha desayunado bien y tiene el estribillo preparado. Mejor quedaos en casa.",
            "Dato importante: cantar en la ducha cuenta como ensayo general. Mañana, gira mundial.",
            "Briska ha pedido entrar en tu grupo. Su especialidad: ladrar en el estribillo y comerse el confeti.",
            "Si hoy alguien está de mal humor, prueba con una coreografía de diez segundos. No falla nunca.",
            "Las guerreras de verdad también se atan los cordones. Sobre todo antes de saltar al escenario.",
            "Reto del día: inventa un grito de guerra para cuando terminas los deberes. Que suene épico.",
            "Un micrófono, una pose y tres segundos de silencio: así empiezan los grandes conciertos (y los grandes chistes).",
            "Hoy tus zapatillas tienen superpoderes. Bueno, eso dicen ellas.",
            "Cada vez que aciertas una multiplicación, en algún sitio se enciende un foco. Hoy, ilumina el estadio.",
            "Pregunta seria: ¿los monstruos bailan mal porque son monstruos o son monstruos porque bailan mal?",
            "Si el pasillo de casa fuera un escenario, hoy habría lleno total. Y Briska en primera fila.",
            "Ensayo secreto: haz el paso más difícil que sepas mientras nadie mira. Luego, haz como si nada.",
            "Las mejores canciones se inventan sin querer. Tararea algo raro y apúntalo, por si acaso.",
            "Hoy toca brillar un poco más que ayer. Con purpurina o sin ella, tú decides.",
            "Briska sostiene que ella también es guerrera. Su arma secreta: mirarte fijamente hasta que le das algo.",
        ],
        1_500_000_000);

    public static readonly Nina[] Todas = [Miranda, Aroa];

    public static Nina? De(Rol rol) => Todas.FirstOrDefault(n => n.Rol == rol);
    public static Nina? PorSlug(string? slug) => Todas.FirstOrDefault(n => n.Slug == slug);
    public static Nina? De(ClaimsPrincipal u) => Todas.FirstOrDefault(n => u.IsInRole(n.Rol.ToString()));
}
