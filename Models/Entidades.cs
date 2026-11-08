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
    public int Raciones { get; set; } = 2;
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
}
