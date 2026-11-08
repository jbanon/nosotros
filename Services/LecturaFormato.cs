using System.Net;
using System.Text;

namespace Love.Services;

/// <summary>Convierte el texto plano de una lectura en HTML sencillo y seguro.</summary>
public static class LecturaFormato
{
    public static string Html(string texto)
    {
        var sb = new StringBuilder();
        var bloques = texto.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var bloqueRaw in bloques)
        {
            var bloque = bloqueRaw.Trim('\n');
            if (string.IsNullOrWhiteSpace(bloque)) continue;
            if (bloque.StartsWith("## "))
                sb.Append("<h2>").Append(WebUtility.HtmlEncode(bloque[3..].Trim())).Append("</h2>");
            else if (bloque.StartsWith("> "))
                sb.Append("<blockquote>").Append(WebUtility.HtmlEncode(string.Join(' ', bloque.Split('\n').Select(l => l.TrimStart('>', ' '))))).Append("</blockquote>");
            else
            {
                var lineas = bloque.Split('\n');
                // Varias líneas cortas: es verso, se respetan los saltos. Si no, es prosa con saltos de línea duros.
                var esVerso = lineas.Length > 1 && lineas.Average(l => l.Length) < 60;
                sb.Append(esVerso ? "<p class=\"verso\">" : "<p>")
                  .Append(WebUtility.HtmlEncode(esVerso ? string.Join('\n', lineas.Select(l => l.Trim())) : string.Join(' ', lineas.Select(l => l.Trim()))))
                  .Append("</p>");
            }
        }
        return sb.ToString();
    }
}
