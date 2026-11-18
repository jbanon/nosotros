using System.Globalization;
using System.Text.RegularExpressions;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Menu;

/// <summary>
/// Importar una lista pegada (la de Alexa, copiada de la app o de la web de Amazon; o cualquier otra) a la compra de la semana.
/// Amazon no deja a apps externas leer las listas de Alexa desde 2024: se pega o se comparte hacia la app (share target).
/// Primero se revisa (zona automática, lo que ya está en la lista va desmarcado) y luego se añade.
/// </summary>
public class ImportarModel(AppDb db, ListaCompraService lista, ZonaAuto zonas) : PageModel
{
    public record Linea(string Nombre, string? Cantidad, Zona Zona, bool YaEsta);
    public string Texto { get; set; } = "";
    public List<Linea> Lineas { get; set; } = new();
    public DateOnly Lunes { get; set; }

    /// <summary>GET normal, o desde "Compartir" del móvil (title/text/url).</summary>
    public async Task OnGet(string? text, string? title, string? url)
    {
        Lunes = Reloj.Lunes(Reloj.Hoy);
        Texto = string.Join("\n", new[] { text, url }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (Texto != "") await Revisar();
    }

    public async Task OnPostRevisar(string? texto)
    {
        Lunes = Reloj.Lunes(Reloj.Hoy);
        Texto = texto ?? "";
        await Revisar();
    }

    async Task Revisar()
    {
        var s = await lista.SemanaDe(Lunes);
        var enLista = s.Lista.Where(i => i.Incluido && !i.Comprado).Select(i => RecurrenteService.Clave(i.Nombre)).ToHashSet();
        foreach (var (nombre, cant) in Leer(Texto))
            Lineas.Add(new Linea(nombre, cant, await zonas.ZonaDe(nombre), enLista.Contains(RecurrenteService.Clave(nombre))));
    }

    public async Task<IActionResult> OnPostAnadir(int n)
    {
        var lunes = Reloj.Lunes(Reloj.Hoy);
        var s = await lista.SemanaDe(lunes);
        int anadidos = 0;
        for (int i = 0; i < Math.Min(n, 200); i++)
        {
            if (Request.Form[$"s{i}"] != "on") continue;
            var nombre = Request.Form[$"n{i}"].ToString().Trim();
            if (nombre == "") continue;
            var zona = Enum.TryParse<Zona>(Request.Form[$"z{i}"], out var z) ? z : await zonas.ZonaDe(nombre);
            var (cant, unidad) = Cantidad(Request.Form[$"c{i}"].ToString());
            var ing = await db.Ingredientes.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower());
            s.Lista.Add(new ListaItem { Nombre = ing?.Nombre ?? nombre, IngredienteId = ing?.Id, Zona = zona, Cantidad = cant, Unidad = unidad, Manual = true, Incluido = true });
            anadidos++;
        }
        await db.SaveChangesAsync();
        TempData["Importados"] = anadidos;
        return Redirect($"/Menu/Lista?lunes={lunes:yyyy-MM-dd}");
    }

    static (decimal?, string?) Cantidad(string txt)
    {
        txt = txt.Trim();
        if (txt == "") return (null, null);
        var p = txt.Split(' ', 2);
        return decimal.TryParse(p[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var c) ? (c, p.Length > 1 ? p[1] : null) : (null, txt);
    }

    // ---------- Lectura del texto pegado ----------
    static readonly string[] Ruido = { "lista de la compra", "lista de compras", "lista de compra", "compras", "compra", "shopping list", "shopping",
        "anadir articulo", "anadir un articulo", "agregar articulo", "agregar un articulo", "anadir elemento", "anadir", "agregar", "ver todo", "ver mas",
        "editar", "compartir", "ordenar", "alexa", "listas", "mis listas", "tareas pendientes", "to-do", "eliminar", "borrar" };
    static readonly string[] Fin = { "completados", "completado", "comprados", "completed", "elementos completados", "articulos completados" };
    static readonly Regex RxVineta = new(@"^[\s•·\-\*–—☐☑✓✔○●▢□■◻◽▪️]+|^\d+[.)]\s+", RegexOptions.Compiled);
    static readonly Regex RxCant = new(@"^(\d+(?:[.,]\d+)?)\s*(kg|kilos?|g|gr|gramos|l|litros?|ml|ud|uds|unidades|paquetes?|botes?|latas?|docenas?|bolsas?|botellas?|cajas?)?\s+(?:de\s+)?(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex RxPor = new(@"^(.+?)\s*(?:[x×]\s*(\d+)|\((\d+)\))$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static string Plano(string s) => RecurrenteService.Clave(s).Replace("-", " ");

    public static List<(string nombre, string? cantidad)> Leer(string texto)
    {
        var res = new List<(string, string?)>();
        var lineas = texto.Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l != "").ToList();
        // Todo en una línea separada por comas ("leche, huevos y pan")
        if (lineas.Count == 1 && Regex.IsMatch(lineas[0], "[,;]"))
            lineas = Regex.Split(lineas[0], @"\s*[,;]\s*|\s+y\s+(?=[^,;]+$)").Where(x => x.Trim() != "").ToList();
        var vistos = new HashSet<string>();
        foreach (var bruta in lineas)
        {
            var l = RxVineta.Replace(bruta, "").Trim().TrimEnd('.', ',', ';');
            if (l.Length < 2 || l.Length > 80 || l.StartsWith("http")) continue;
            var plano = Plano(l);
            if (Fin.Contains(plano)) break;
            if (Ruido.Contains(plano)) continue;
            string? cant = null;
            if (RxCant.Match(l) is { Success: true } m) { cant = (m.Groups[1].Value + " " + m.Groups[2].Value).Trim(); l = m.Groups[3].Value.Trim(); }
            else if (RxPor.Match(l) is { Success: true } p) { cant = (p.Groups[2].Success ? p.Groups[2].Value : p.Groups[3].Value) + " ud"; l = p.Groups[1].Value.Trim(); }
            if (l.Length < 2) continue;
            l = char.ToUpper(l[0]) + l[1..];
            if (vistos.Add(RecurrenteService.Clave(l))) res.Add((l, cant));
        }
        return res;
    }
}
