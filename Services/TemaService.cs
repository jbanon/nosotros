namespace Love.Services;

public record Tema(string Clave, string Nombre, string Particulas, string Emoji);

/// <summary>Resuelve el tema visual según la época del año.</summary>
public static class TemaService
{
    public static readonly Dictionary<string, Tema> Temas = new()
    {
        ["navidad"]     = new("navidad", "Navidad", "nieve luces", "🎄"),
        ["invierno"]    = new("invierno", "Invierno", "nieve", "❄️"),
        ["valentin"]    = new("valentin", "San Valentín", "corazones", "💘"),
        ["primavera"]   = new("primavera", "Primavera", "petalos", "🌸"),
        ["aniversario"] = new("aniversario", "Aniversario", "petalos brillos", "💍"),
        ["verano"]      = new("verano", "Verano", "luciernagas", "🌊"),
        ["cumple"]      = new("cumple", "Cumpleaños", "confeti", "🎂"),
        ["otono"]       = new("otono", "Otoño", "hojas", "🍂"),
        ["halloween"]   = new("halloween", "Halloween", "murcielagos", "🎃"),
    };

    public static Tema PorFecha(DateOnly f)
    {
        int m = f.Month, d = f.Day;
        bool entre(int m1, int d1, int m2, int d2)
        {
            int v = m * 100 + d, a = m1 * 100 + d1, b = m2 * 100 + d2;
            return a <= b ? v >= a && v <= b : v >= a || v <= b;
        }
        if (entre(12, 1, 1, 6)) return Temas["navidad"];
        if (entre(10, 25, 11, 1)) return Temas["halloween"];
        if (entre(1, 7, 3, 19)) return Temas["invierno"];
        if (entre(3, 20, 6, 20)) return Temas["primavera"];
        if (entre(6, 21, 9, 21)) return Temas["verano"];
        return Temas["otono"];
    }

    public static Tema PorClave(string? clave) =>
        clave != null && Temas.TryGetValue(clave, out var t) ? t : throw new KeyNotFoundException(clave);
}
