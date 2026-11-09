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

public enum TipoTareaAgente { FichaPelicula, Receta, RecetaFoto }
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
    public int Rondas { get; set; }
    public int DuracionMs { get; set; }
    public decimal CosteUsd { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime? Terminada { get; set; }
    public DateTime? Aplicada { get; set; }
}
