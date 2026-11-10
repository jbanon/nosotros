using System.Globalization;
using System.Text;

namespace Love.Services;

/// <summary>Criptofrase: cada letra se sustituye por un número; la clave es fija para una semilla.</summary>
public static class Criptofrase
{
    public const string Alfabeto = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    /// <summary>Mayúsculas sin acentos (la Ñ se conserva).</summary>
    public static string Normalizar(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in (s ?? "").ToUpperInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                if (ch == '̃' && sb.Length > 0 && sb[^1] == 'N') sb[^1] = 'Ñ';
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    public static string SoloLetras(string? s) => new(Normalizar(s ?? "").Where(char.IsLetter).ToArray());
    public static bool Coincide(string? a, string? b) => SoloLetras(a) == SoloLetras(b) && SoloLetras(a).Length > 0;

    public static Dictionary<char, int> Clave(int semilla)
    {
        var nums = Enumerable.Range(1, Alfabeto.Length).ToArray();
        var rnd = new Random(semilla);
        for (int i = nums.Length - 1; i > 0; i--) { int j = rnd.Next(i + 1); (nums[i], nums[j]) = (nums[j], nums[i]); }
        return Alfabeto.Select((l, i) => (l, n: nums[i])).ToDictionary(x => x.l, x => x.n);
    }

    /// <summary>Lo que ve el navegador: palabras como listas de celdas {n} (letra) o {s} (signo).</summary>
    public static object Vista(string frase, int semilla, string? pista)
    {
        var clave = Clave(semilla);
        var norm = Normalizar(frase);
        var palabras = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Select(ch =>
            clave.TryGetValue(ch, out var n) ? (object)new { n } : new { s = ch.ToString() }).ToArray()).ToArray();
        return new { palabras, pista, letras = norm.Where(char.IsLetter).Distinct().Count() };
    }

    public static bool Comprobar(string frase, int semilla, Dictionary<string, string> asignacion)
    {
        var clave = Clave(semilla);
        foreach (var ch in Normalizar(frase).Where(char.IsLetter).Distinct())
            if (!clave.ContainsKey(ch) || !asignacion.TryGetValue(clave[ch].ToString(), out var l) || Normalizar(l) != ch.ToString()) return false;
        return true;
    }

    /// <summary>Una pista: la clave de la letra más frecuente que aún no está bien.</summary>
    public static object? Pista(string frase, int semilla, Dictionary<string, string> asignacion)
    {
        var clave = Clave(semilla);
        var pendiente = Normalizar(frase).Where(ch => char.IsLetter(ch) && clave.ContainsKey(ch)).GroupBy(ch => ch).OrderByDescending(g => g.Count())
            .Select(g => g.Key).FirstOrDefault(ch => !asignacion.TryGetValue(clave[ch].ToString(), out var l) || Normalizar(l) != ch.ToString());
        return pendiente == default ? null : new { n = clave[pendiente], letra = pendiente.ToString() };
    }
}
