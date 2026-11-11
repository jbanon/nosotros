using System.ComponentModel.DataAnnotations;

namespace Love.Models;

public enum Rol { Admin, Raquel }
public enum TipoMensaje { Frase, Poema }
public enum Zona { Fruteria, Charcuteria, Carniceria, Pescaderia, Otros }
public enum TipoGasto { Veva, Compra, Hipoteca }

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string PinHash { get; set; } = "";
    public Rol Rol { get; set; }
}

/// <summary>Mensaje del calendario: uno por día del año (mes/día), se repite cada año.</summary>
public class Mensaje
{
    public int Id { get; set; }
    public int Mes { get; set; }
    public int Dia { get; set; }
    public TipoMensaje Tipo { get; set; }
    [Required] public string Texto { get; set; } = "";
    public string? Titulo { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Fecha señalada que prevalece sobre el mensaje del calendario y puede cambiar el tema.</summary>
public class FechaEspecial
{
    public int Id { get; set; }
    public int Mes { get; set; }
    public int Dia { get; set; }
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Texto { get; set; } = "";
    /// <summary>Año de origen para contadores (aniversario 2024, cumpleaños 1980). Null si no aplica.</summary>
    public int? AnioOrigen { get; set; }
    /// <summary>Texto del contador, con {n} como número de años. Ej: "{n} años juntos".</summary>
    public string? TextoContador { get; set; }
    /// <summary>Tema visual (clave de TemaService) o null para usar el de la época.</summary>
    public string? Tema { get; set; }
    public string? Emoji { get; set; }
    public bool CuentaAtras { get; set; } = true;
    public bool Activo { get; set; } = true;
}

public class Ingrediente
{
    public int Id { get; set; }
    [Required] public string Nombre { get; set; } = "";
    public Zona Zona { get; set; } = Zona.Otros;
    /// <summary>De despensa: sal, aceite... No entra en la lista por defecto.</summary>
    public bool Despensa { get; set; }
    public List<RecetaIngrediente> Recetas { get; set; } = new();
}

public class Receta
{
    public int Id { get; set; }
    [Required] public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int Raciones { get; set; } = 12;
    public string? Etiquetas { get; set; }
    public string? Preparacion { get; set; }
    public string? FotoPath { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public List<RecetaIngrediente> Ingredientes { get; set; } = new();
}

public class RecetaIngrediente
{
    public int Id { get; set; }
    public int RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;
    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;
    public decimal? Cantidad { get; set; }
    public string? Unidad { get; set; }
    public string? Nota { get; set; }
    public int Orden { get; set; }
}

public class Semana
{
    public int Id { get; set; }
    public DateOnly LunesInicio { get; set; }
    public string? Notas { get; set; }
    /// <summary>Momento (UTC) en que se marcó el primer producto como comprado (arranca el cronómetro); se vacía al apuntar el ticket.</summary>
    public DateTime? CompraInicio { get; set; }
    public List<SemanaReceta> Recetas { get; set; } = new();
    public List<ListaItem> Lista { get; set; } = new();
}

public class SemanaReceta
{
    public int Id { get; set; }
    public int SemanaId { get; set; }
    public Semana Semana { get; set; } = null!;
    public int RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;
    public int Veces { get; set; } = 1;
}

public class ListaItem
{
    public int Id { get; set; }
    public int SemanaId { get; set; }
    public Semana Semana { get; set; } = null!;
    public int? IngredienteId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? Cantidad { get; set; }
    public string? Unidad { get; set; }
    public Zona Zona { get; set; }
    /// <summary>False = desmarcado (no se compra esta vez).</summary>
    public bool Incluido { get; set; } = true;
    public bool Comprado { get; set; }
    public bool Manual { get; set; }
    /// <summary>Recetas de las que procede, para mostrar "para: ...".</summary>
    public string? Origen { get; set; }
}

public class Gasto
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public TipoGasto Tipo { get; set; }
    public decimal Importe { get; set; }
    public string? Nota { get; set; }
    public string? AdjuntoPath { get; set; }
    public string? AdjuntoNombre { get; set; }
    public string? AdjuntoMime { get; set; }
    public bool EsFijo { get; set; }
    /// <summary>Minutos en el súper: desde el primer producto marcado hasta apuntar el ticket.</summary>
    public int? DuracionMin { get; set; }
    public DateTime Creado { get; set; } = DateTime.UtcNow;
    public List<TicketLinea> Lineas { get; set; } = new();
}

/// <summary>Una línea de un ticket de compra (producto e importe), para detectar compras recurrentes.</summary>
public class TicketLinea
{
    public int Id { get; set; }
    public int GastoId { get; set; }
    public Gasto Gasto { get; set; } = null!;
    public string Producto { get; set; } = "";
    public decimal? Importe { get; set; }
    public int Orden { get; set; }
}

/// <summary>Cosas que Raquel (o el admin) quieren que se hagan solas: ideas para automatizar.</summary>
public class Idea
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public string? Nota { get; set; }
    public string Autor { get; set; } = "";
    public bool Hecha { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime? HechaEl { get; set; }
    /// <summary>Qué se automatizó y cómo funciona, para que Raquel lo vea cuando la idea está hecha.</summary>
    public string? Resumen { get; set; }
}

/// <summary>Películas que queremos ver juntos.</summary>
public class Pelicula
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Nota { get; set; }
    public string Autor { get; set; } = "";
    public bool Vista { get; set; }
    public DateTime Anadida { get; set; } = DateTime.UtcNow;
    public DateTime? VistaEl { get; set; }
    // Mini ficha creada por el agente
    public string? TituloOriginal { get; set; }
    public int? Anio { get; set; }
    public string? Director { get; set; }
    public string? Reparto { get; set; }
    public string? Genero { get; set; }
    public int? Duracion { get; set; }
    public string? Sinopsis { get; set; }
    public string? Curiosidad { get; set; }
    public string? PosterPath { get; set; }
    public DateTime? FichaEl { get; set; }
    public bool TieneFicha => FichaEl != null;
}

/// <summary>Una lectura que Javi le deja a Raquel: artículo, cuento, poema… con su texto completo.</summary>
public class Lectura
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Autor { get; set; }
    /// <summary>De dónde sale: "El País, 2008", "Libro X"…</summary>
    public string? Fuente { get; set; }
    /// <summary>Unas líneas de Javi explicando por qué se la deja.</summary>
    public string? Dedicatoria { get; set; }
    /// <summary>Texto completo. Párrafos separados por línea en blanco; "## " subtítulo; "> " cita.</summary>
    public string Texto { get; set; } = "";
    public bool Publicada { get; set; } = true;
    public bool Leida { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime? LeidaEl { get; set; }
    public int Palabras => string.IsNullOrWhiteSpace(Texto) ? 0 : Texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    public int Minutos => Math.Max(1, (int)Math.Round(Palabras / 200.0));
}

/// <summary>Meses en los que ya se crearon los fijos (para no recrearlos si el admin los borra).</summary>
public class MesInicializado
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
}

public class Config
{
    [Key] public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
}

public static class ConfigClaves
{
    /// <summary>Declaración permanente de la página principal (líneas separadas por salto de línea).</summary>
    public const string DeclaracionPorDefecto = "Eres el amor de mi vida.\nHasta que te conocí había perdido la esperanza.\nLlegaste tú, y volví a creer.\nEres completamente especial:\nno hay otra mujer como tú.";

    public const string ObjetivoCompra = "ObjetivoCompra";
    public const string ImporteVeva = "ImporteVeva";
    public const string ImporteHipoteca = "ImporteHipoteca";
    public const string Firma = "Firma";
    public const string NombreElla = "NombreElla";
    public const string RacionesLista = "RacionesLista";
    public const string Declaracion = "Declaracion";
    /// <summary>Marca de que las recetas existentes ya se escalaron a las raciones de la lista.</summary>
    public const string RecetasEscaladas = "RecetasEscaladas";
    /// <summary>Frase final del calendario de adviento (con espacios; las letras salen de las casillas).</summary>
    public const string AdvientoFrase = "AdvientoFrase";
    /// <summary>Viñetas: cuántos días por semana sale una (por defecto 3).</summary>
    public const string VinetasPorSemana = "VinetasPorSemana";
    /// <summary>Viñetas: desde qué fecha pueden salir las de libros que Raquel ya conoce (yyyy-MM-dd).</summary>
    public const string VinetasConocidasDesde = "VinetasConocidasDesde";
    /// <summary>Viñetas: de cada 10 viñetas, cuántas llegan como juego (por defecto 5).</summary>
    public const string VinetasJuegoDe10 = "VinetasJuegoDe10";
    /// <summary>Desbloqueos por corazones: umbrales y textos.</summary>
    public const string LogroGrande = "LogroGrande";           // corazones para el rompecabezas grande (3)
    public const string LogroVale = "LogroVale";               // corazones para el vale (6)
    public const string LogroValeTexto = "LogroValeTexto";
    public const string LogroCarta = "LogroCarta";             // corazones para la carta (10)
    public const string LogroCartaTexto = "LogroCartaTexto";
    public const string LogroValeCanjeado = "LogroValeCanjeado";
}

public enum TipoCasilla { Foto, Criptofrase, Murdoku, Crucigrama, Vale, Final }

/// <summary>Una casilla del calendario de adviento (1..24 de diciembre de un año).</summary>
public class AdvientoCasilla
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public int Dia { get; set; }
    public TipoCasilla Tipo { get; set; }
    public string Titulo { get; set; } = "";
    /// <summary>Texto de Javi para ese día: presentación, pie de foto, dedicatoria del vale…</summary>
    public string? Intro { get; set; }
    /// <summary>Contenido del pasatiempo en JSON (frase, pistas, palabras, solución…).</summary>
    public string Datos { get; set; } = "{}";
    /// <summary>Letra de la frase final que se gana al resolver la casilla.</summary>
    public string? Letra { get; set; }
    public string? FotoPath { get; set; }
    /// <summary>Vídeo de la casilla (mp4/mov/webm). En una casilla Foto sustituye a la foto, que pasa a ser su portada.</summary>
    public string? VideoPath { get; set; }
    /// <summary>Viñeta que se regala además de la letra al resolver la casilla.</summary>
    public int? VinetaId { get; set; }
    /// <summary>Pregunta opcional de una palabra para desvelar la foto.</summary>
    public string? Pregunta { get; set; }
    public string? Respuesta { get; set; }
    /// <summary>Progreso guardado del pasatiempo (JSON del navegador).</summary>
    public string? Estado { get; set; }
    public int Pistas { get; set; }
    public DateTime? AbiertaEl { get; set; }
    public DateTime? ResueltaEl { get; set; }
    public DateTime? CanjeadoEl { get; set; }

    public bool Resuelta => ResueltaEl != null;
    public bool DaLetra => !string.IsNullOrWhiteSpace(Letra);
}

public enum TipoTareaAgente { FichaPelicula, Receta, RecetaFoto, Vineta }
public enum EstadoTarea { Pendiente, EnCurso, Lista, Error }

/// <summary>Una petición al agente (claude -p): crear una ficha de película o una receta, con rondas de cambios.</summary>
public class TareaAgente
{
    public int Id { get; set; }
    public TipoTareaAgente Tipo { get; set; }
    public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;
    /// <summary>Lo que pidió el usuario al principio: título de la peli, nombre de la receta o notas de la foto.</summary>
    public string Entrada { get; set; } = "";
    public string? Notas { get; set; }
    public string? FotoPath { get; set; }
    /// <summary>Última propuesta del agente (JSON con el esquema del tipo).</summary>
    public string? Propuesta { get; set; }
    /// <summary>Conversación: [{"rol":"usuario"|"agente","texto":"…","fecha":"…"}].</summary>
    public string Historial { get; set; } = "[]";
    public string? Error { get; set; }
    public string Autor { get; set; } = "";
    public int? PeliculaId { get; set; }
    public int? RecetaId { get; set; }
    public int? VinetaId { get; set; }
    public int Rondas { get; set; }
    public int DuracionMs { get; set; }
    /// <summary>Estimación que imprime el CLI (total_cost_usd). Con suscripción no es un cargo; solo informativo, no se muestra.</summary>
    public decimal CosteUsd { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime? Terminada { get; set; }
    public DateTime? Aplicada { get; set; }
}

public enum JuegoVineta { Ninguno, Rompecabezas, Parejas, Revelar, Criptofrase, OrdenaFrase }

/// <summary>Una ilustración de la colección (72 Kilos y otros), con la frase de Javi y su catalogación.</summary>
public class Vineta
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string Autor { get; set; } = "72 Kilos";
    public string? Libro { get; set; }
    /// <summary>Texto que lleva la propia ilustración.</summary>
    public string? FraseAutor { get; set; }
    /// <summary>La frase de Javi para Raquel que acompaña a la viñeta.</summary>
    public string? FraseMia { get; set; }
    /// <summary>Intenciones separadas por comas: amor, apoyo, reconciliación, gratitud…</summary>
    public string? Intenciones { get; set; }
    public string? Tono { get; set; }
    /// <summary>Momentos de uso sugeridos, separados por comas.</summary>
    public string? Momentos { get; set; }
    public string? Descripcion { get; set; }
    public JuegoVineta JuegoSugerido { get; set; }
    /// <summary>La imagen deja hueco limpio para superponer una frase.</summary>
    public bool EspacioFrase { get; set; }
    /// <summary>Raquel ya conoce el libro: se reparte más tarde y con menos frecuencia.</summary>
    public bool Conocida { get; set; }
    public bool Activa { get; set; } = true;
    public bool UsoPortada { get; set; } = true;
    public bool UsoJuego { get; set; } = true;
    /// <summary>Día concreto en que debe salir (vacío = reparto automático).</summary>
    public DateOnly? FechaFija { get; set; }
    /// <summary>Pregunta y respuesta opcionales para el juego "Revelar".</summary>
    public string? Pregunta { get; set; }
    public string? Respuesta { get; set; }
    public DateOnly? MostradaEl { get; set; }
    public bool Favorita { get; set; }
    public string? RespuestaRaquel { get; set; }
    public DateTime? RespondidaEl { get; set; }
    public bool RespuestaVista { get; set; }
    /// <summary>Nombre del fichero original (para no importar dos veces).</summary>
    public string? Origen { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public List<VinetaDia> Dias { get; set; } = new();
}

/// <summary>La viñeta asignada a un día de la portada, con su juego y progreso.</summary>
public class VinetaDia
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int VinetaId { get; set; }
    public Vineta Vineta { get; set; } = null!;
    public JuegoVineta Juego { get; set; }
    /// <summary>Progreso del juego guardado por el navegador (JSON).</summary>
    public string? Estado { get; set; }
    public DateTime? AbiertaEl { get; set; }
    public DateTime? ResueltaEl { get; set; }
    public int Pistas { get; set; }
    /// <summary>Corazones ganados (1 por juego resuelto).</summary>
    public int Corazones { get; set; }
    public bool Desvelada => Juego == JuegoVineta.Ninguno || ResueltaEl != null;
}

/// <summary>"Disfruta la vista": una obra de arte con un recorrido de paradas sobre sus detalles.</summary>
public class Obra
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Autor { get; set; }
    public string? Anio { get; set; }
    public string? Museo { get; set; }
    /// <summary>Página de origen de la imagen (Commons, museo…).</summary>
    public string? FuenteUrl { get; set; }
    public string? Licencia { get; set; }
    /// <summary>Unas líneas de entrada antes del recorrido.</summary>
    public string? Intro { get; set; }
    /// <summary>La frase de Javi para Raquel en la última parada (vuelta a la vista completa).</summary>
    public string? FraseMia { get; set; }
    /// <summary>Imagen grande (máx. 4096 px) y reducida (1600 px), en adjuntos/arte.</summary>
    public string? ImagenPath { get; set; }
    public string? ReducidaPath { get; set; }
    public int Ancho { get; set; }
    public int Alto { get; set; }
    /// <summary>Domingo en que se publica (vacío = siguiente hueco de la cola).</summary>
    public DateOnly? Publicacion { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime? VistaEl { get; set; }
    public bool Favorita { get; set; }
    public string? RespuestaRaquel { get; set; }
    public DateTime? RespondidaEl { get; set; }
    public bool RespuestaVista { get; set; }
    /// <summary>Clave del seed o nombre de fichero original, para no importar dos veces.</summary>
    public string? Origen { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public List<ParadaObra> Paradas { get; set; } = new();
}

/// <summary>Una parada del recorrido: punto de la obra (fracción 0–1), zoom y bocadillo.</summary>
public class ParadaObra
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public Obra Obra { get; set; } = null!;
    public int Orden { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    /// <summary>Aumento respecto a la obra entera encajada en pantalla (1 = entera).</summary>
    public double Zoom { get; set; } = 3;
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
}
