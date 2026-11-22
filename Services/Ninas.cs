using System.Security.Claims;
using Love.Models;

namespace Love.Services;

/// <summary>
/// Las niñas que tienen su propia versión de la app (Miranda desde 2026-11-21; Aroa, Elsa y Marcos desde 2026-11-22). «Niñas» por costumbre: Marcos también: solo su página
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

    public static readonly Nina Elsa = new(Rol.Elsa, "Elsa", "elsa", new DateOnly(2014, 8, 2), 12, "Javi",
        "Elsa nació el 2 de agosto de 2014. Es muy deportista (le encanta moverse, entrenar, competir y ganar, y sabe perder con estilo) y muy inteligente: " +
        "pilla las cosas a la primera, le gustan los retos, los acertijos y saber el porqué de todo, y a veces sabe más que los mayores. " +
        "Briska es la perra de Javi, que es quien firma (Javi). " +
        "Guiños posibles: entrenamientos, partidos, carreras, medallas, el cronómetro, las agujetas, estirar, el balón, acertijos, " +
        "preguntas difíciles, ganar a los mayores, Briska; sin inventarse un deporte concreto.",
        [
            "Quien estira antes de correr llega antes a la meta",
            "Briska corre detrás de la pelota pero nunca la devuelve",
            "Mi cerebro también hace flexiones por las mañanas",
            "Las agujetas son medallas que no se ven",
            "Ganar mola pero aprender a perder mola todavía más",
            "Un buen acertijo es un partido para la cabeza",
            "El cronómetro nunca miente pero a veces exagera",
            "Hoy he corrido tanto que mi sombra pide un descanso",
            "Si me preguntas por qué te pregunto por qué no",
            "Los mayores pierden al parchís y dicen que dejaron ganar",
            "Una zapatilla sin atar es una caída anunciada",
            "Briska entrena todos los días para ser la más dormilona",
            "La mejor jugada es la que nadie ve venir",
            "Pensar rápido es como correr pero sin sudar",
            "El balón siempre vuelve si lo tratas con cariño",
            "Mi botella de agua viaja más que muchos turistas",
            "Las preguntas difíciles son mis favoritas del menú",
            "Calentar los músculos también calienta las ideas",
            "Hoy toca batir mi récord aunque sea de bostezos",
            "Un equipo gana cuando nadie quiere ser el único héroe",
            "Las matemáticas son un deporte con mucho sentido",
            "Briska ha pedido ser la mascota oficial del equipo",
            "Cansada pero feliz es mi forma favorita de acabar",
            "El que entrena en silencio celebra con mucho ruido",
        ],
        [
            "Dato científico: las agujetas son el cuerpo diciendo «ayer lo diste todo». Hoy, a estirar y a presumir.",
            "Acertijo del día: ¿qué corre mucho y nunca se cansa? El reloj. Por eso nunca le dejamos ganar.",
            "Briska te desafía a una carrera. Ventaja suya: cuatro patas. Ventaja tuya: sabes dónde está la meta.",
            "Pregunta para los mayores de casa: ¿por qué el cielo es azul? Espera a que se líen y explícaselo tú.",
            "Hoy cuenta como entrenamiento subir las escaleras de dos en dos. Y bajar sin tropezar, también.",
            "Las medallas brillan, pero lo que de verdad brilla es levantarse cuando te caes.",
            "Reto del día: aprender una palabra difícil y usarla en la cena como si nada.",
            "Un buen deportista calienta antes, se esfuerza durante y merienda después. El orden importa.",
            "Si hoy pierdes una partida, piensa que el otro necesitaba la alegría más que tú. Y la próxima, a por ella.",
            "Hay dos tipos de personas: las que leen esto, y tú, que ya lo habías adivinado.",
            "Misión: ganar a alguien mayor a cualquier juego de pensar. No vale enfadarse si te piden la revancha.",
            "Briska ha estado entrenando toda la noche. Bueno, roncando. Para ella es lo mismo.",
            "Correr despeja la cabeza y pensar despeja el camino. Tú haces las dos cosas: imparable.",
            "Consejo de campeona: beber agua, dormir bien y reírse mucho. La tercera es la más importante.",
            "Hoy es buen día para hacer algo por primera vez. Lo difícil siempre es la primera vez; luego ya es tuyo.",
        ],
        1_750_000_000);

    public static readonly Nina Marcos = new(Rol.Marcos, "Marcos", "marcos", new DateOnly(2013, 11, 26), 13, "Javi",
        "Marcos es un chico; nació el 26 de noviembre de 2013. Le encantan los videojuegos y el deporte, y hasta ahora anda un poco despistadillo con los estudios. " +
        "Javi (que firma) quiere que vaya cambiando el chip y se dé cuenta de que estudiar le renta: hazlo con mucha mano izquierda, nunca con sermones ni reproches. " +
        "Ideas que funcionan: estudiar como subir de nivel, desbloquear logros, entrenar para el partido, el que sabe más gana la discusión, " +
        "la libertad de tener los deberes hechos antes, el cerebro como el mejor mando, el boss final del examen. Habla en masculino. Briska es la perra de Javi. " +
        "Guiños posibles: videojuegos (sin nombrar juegos concretos), partidas, niveles, logros, el deporte, entrenamientos, goles, el examen como jefe final, Briska. " +
        "Más o menos una de cada tres frases con la idea de que estudiar renta; el resto, solo humor.",
        [
            "Cada tema aprendido es un nivel que nadie te quita",
            "El examen es el jefe final y tú tienes la guía",
            "Briska no estudia y mira cómo vive de dormida",
            "Los logros de verdad se desbloquean con los apuntes",
            "Un gol en el último minuto también se entrena",
            "Con los deberes hechos la tarde es todo tuya",
            "El cerebro es el mejor mando que vas a tener",
            "Quien entiende las reglas gana todas las partidas",
            "Media hora de estudio vale por tres de agobio",
            "Las notas buenas son el mejor modo multijugador",
            "Los campeones repiten la jugada hasta clavarla",
            "Saber más es la mejor mejora para tu personaje",
            "El que lee las instrucciones nunca pierde la vida",
            "Un buen entrenador nunca se salta el calentamiento",
            "Las matemáticas son trucos para ganar más partidas",
            "Briska sigue intentando ser la portera del equipo",
            "Guardar la partida también vale para lo aprendido",
            "La experiencia sube estudiando un poco cada día",
            "Un sobresaliente sabe mejor que una victoria fácil",
            "Hasta los mejores jugadores empiezan en el nivel uno",
            "Los profes son jefes que sueltan pistas si atiendes",
            "Concentrarse diez minutos es un superpoder raro",
            "Las agujetas del cerebro también acaban pasando",
            "Quien juega con cabeza nunca juega sin cabeza",
        ],
        [
            "Dato de jugador: el examen es un jefe final con los ataques anunciados. Quien se lee la guía (los apuntes) gana sin despeinarse.",
            "Truco poco conocido: con los deberes hechos antes de merendar, la tarde se desbloquea entera. Sin anuncios.",
            "Briska ha empezado a estudiar. Lleva tres horas mirando un libro. Bueno, está dormida encima.",
            "Ningún jugador llega al nivel máximo sin morir unas cuantas veces. Con los exámenes pasa igual: se reintenta y se sube.",
            "Hoy toca entrenar como un profesional: calentar, darlo todo y estirar. En el campo y con los apuntes.",
            "Pregunta seria: si fueras un personaje, ¿qué habilidad subirías hoy? Pista: se sube leyendo.",
            "Récord del día: aguantar veinte minutos concentrado sin mirar el móvil. Hay pocos que lo consiguen.",
            "Los mejores delanteros no tiran a puerta a lo loco: miran, piensan y luego la clavan.",
            "Lo bueno de aprender algo: no se pierde aunque se apague la consola.",
            "Hoy es buen día para ganar una discusión en casa con un dato que nadie sabe. Para eso hay que saberlo antes.",
            "Logro desbloqueado: leer esta frase. Siguiente logro: abrir la mochila sin que te lo digan.",
            "Briska sostiene que ella también tiene examen: el de pedir comida poniendo cara de pena. Sobresaliente.",
            "Cada día un poquito es el truco secreto de los que parecen listos sin esfuerzo. No lo cuentes.",
            "Si hoy el estudio se hace cuesta arriba, piensa que la cuesta abajo viene en junio.",
            "Un equipo con un buen capitán gana más. Y el capitán suele ser el que se ha leído la táctica.",
        ],
        2_000_000_000);

    public static readonly Nina[] Todas = [Miranda, Aroa, Elsa, Marcos];

    public static Nina? De(Rol rol) => Todas.FirstOrDefault(n => n.Rol == rol);
    public static Nina? PorSlug(string? slug) => Todas.FirstOrDefault(n => n.Slug == slug);
    public static Nina? De(ClaimsPrincipal u) => Todas.FirstOrDefault(n => u.IsInRole(n.Rol.ToString()));
}
