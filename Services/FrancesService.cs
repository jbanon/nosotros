using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Píldoras de francés para Raquel (B2-C1): el agente escribe una lectura divertida con ejercicios
/// los lunes, miércoles y viernes; ella responde y el agente corrige con cariño.
/// </summary>
public class FrancesService(AppDb db, AgenteService agente)
{
    /// <summary>Días en que sale una píldora nueva.</summary>
    public static readonly DayOfWeek[] Dias = { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
    /// <summary>Con cuántos días de antelación se prepara la siguiente (para que esté lista al amanecer).</summary>
    const int Antelacion = 1;

    /// <summary>Temas que se van turnando; el agente elige el asunto concreto.</summary>
    public static readonly (string Clave, string Nombre, string Emoji, string Encargo)[] Temas =
    {
        ("texte", "Texto", "📜", "un texto breve y divertido: una escena cotidiana, una carta absurda, un diálogo con malentendidos, un microrrelato con giro final"),
        ("recette", "Receta", "🍳", "una receta francesa contada con humor (la abuela que no da cantidades, el chef ofendido, la receta que siempre sale mal)"),
        ("actualite", "Noticia", "🗞️", "una noticia curiosa o simpática de la actualidad francesa o francófona de las últimas semanas (búscala con WebSearch; nada de política dura ni tragedias), contada con guasa"),
        ("curiosite", "Curiosidad", "🧐", "una curiosidad de Francia o de la lengua francesa: costumbres raras, historia de una palabra, inventos, manías nacionales"),
        ("expression", "Expresión", "💬", "una expresión idiomática o de argot francés: su origen, cómo y cuándo se usa de verdad, con una historieta que la ponga en escena"),
    };

    public static (string Clave, string Nombre, string Emoji, string Encargo) Tema(string clave) =>
        Temas.FirstOrDefault(t => t.Clave == clave) is { Clave: not null } t ? t : Temas[0];

    public Task<List<PildoraFrances>> Publicadas(DateOnly hoy) =>
        db.Pildoras.Where(p => p.Fecha <= hoy).OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id).ToListAsync();

    /// <summary>Fecha de la siguiente píldora: hoy si aún no hay ninguna; si no, el siguiente día de publicación libre.</summary>
    public async Task<DateOnly> ProximaFecha(DateOnly hoy)
    {
        if (!await db.Pildoras.AnyAsync()) return hoy;
        var ultima = await db.Pildoras.MaxAsync(p => p.Fecha);
        var d = ultima >= hoy ? ultima.AddDays(1) : hoy;
        while (!Dias.Contains(d.DayOfWeek)) d = d.AddDays(1);
        return d;
    }

    public Task<bool> Preparando() =>
        db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.Pildora && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso));

    /// <summary>Encarga al agente la siguiente píldora si toca (lo llama el programador cada hora).</summary>
    public async Task Asegurar(DateOnly hoy)
    {
        if (!agente.Disponible || await Preparando()) return;
        // Tras un fallo, no reintentar en bucle: se espera unas horas (o Javier lo relanza a mano)
        var desde = DateTime.UtcNow.AddHours(-6);
        if (await db.TareasAgente.AnyAsync(t => t.Tipo == TipoTareaAgente.Pildora && t.Estado == EstadoTarea.Error && t.Creada > desde)) return;
        var f = await ProximaFecha(hoy);
        if (f > hoy.AddDays(Antelacion)) return;
        var n = await db.Pildoras.CountAsync();
        await Encargar(f, Temas[n % Temas.Length].Clave, null, "programador");
    }

    /// <summary>Las píldoras sin traducción al castellano (las de antes del 2026-11-25) se la encargan al agente.</summary>
    public async Task AsegurarTraducciones()
    {
        if (!agente.Disponible) return;
        var pendientes = await db.TareasAgente.Where(t => t.Tipo == TipoTareaAgente.PildoraTraduccion && t.PildoraId != null
            && (t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso || t.Creada > DateTime.UtcNow.AddHours(-6))).Select(t => t.PildoraId!.Value).ToListAsync();
        foreach (var p in await db.Pildoras.ToListAsync())
            if (Txt(Json(p.Contenido), "traduccion") == "" && !pendientes.Contains(p.Id))
                await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.PildoraTraduccion, Entrada = p.Titulo, PildoraId = p.Id, Autor = "traducción" });
    }

    public Task<TareaAgente> Encargar(DateOnly fecha, string tema, string? idea, string autor) =>
        agente.Encolar(db, new TareaAgente
        {
            Tipo = TipoTareaAgente.Pildora, Entrada = fecha.ToString("yyyy-MM-dd"), Autor = autor,
            Notas = Tema(tema).Clave + (string.IsNullOrWhiteSpace(idea) ? "" : "|" + idea.Trim()),
        });

    public async Task<PildoraEntrega> Entregar(PildoraFrances p, int usuario, JsonObject respuestas)
    {
        var e = new PildoraEntrega { PildoraId = p.Id, UsuarioId = usuario, Respuestas = respuestas.ToJsonString(), Estado = EstadoEntrega.Corrigiendo };
        db.PildoraEntregas.Add(e);
        await db.SaveChangesAsync();
        await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.PildoraCorreccion, Entrada = p.Titulo, PildoraId = p.Id, EntregaId = e.Id, Autor = "corrección" });
        return e;
    }

    public async Task ReintentarCorreccion(PildoraEntrega e)
    {
        var t = await db.TareasAgente.Where(t => t.EntregaId == e.Id).OrderByDescending(t => t.Id).FirstOrDefaultAsync();
        e.Estado = EstadoEntrega.Corrigiendo;
        if (t == null) { await db.SaveChangesAsync(); await agente.Encolar(db, new TareaAgente { Tipo = TipoTareaAgente.PildoraCorreccion, Entrada = "corrección", PildoraId = e.PildoraId, EntregaId = e.Id, Autor = "corrección" }); }
        else await agente.Reintentar(db, t);
    }

    // ---------- Lectura de los JSON ----------
    public static JsonObject Json(string? s)
    {
        try { return string.IsNullOrWhiteSpace(s) ? new JsonObject() : JsonNode.Parse(s) as JsonObject ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }
    public static string Txt(JsonNode? n, string k) => n?[k] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";
    public static int Int(JsonNode? n, string k, int def = -1) => n?[k] is JsonValue v && v.TryGetValue<int>(out var i) ? i : def;
    public static List<JsonNode> Lista(JsonNode? n, string k) => n?[k] is JsonArray a ? a.Where(x => x != null).Select(x => x!).ToList() : new();
    public static List<string> Textos(JsonNode? n, string k) => n?[k] is JsonArray a ? a.Select(x => x is JsonValue v && v.TryGetValue<string>(out var s) ? s : "").ToList() : new();

    /// <summary>Texto en párrafos con las palabras del glosario marcadas (primera aparición; al tocar se ve la traducción).</summary>
    public static string Html(string texto, IEnumerable<(string fr, string es)> glosario)
    {
        var marcas = new List<string>();
        var sb = new StringBuilder();
        var terminos = glosario.Where(g => g.fr.Trim().Length > 1).OrderByDescending(g => g.fr.Length).ToList();
        foreach (var bruto in texto.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var par = WebUtility.HtmlEncode(bruto.Trim().Replace("\n", " "));
            if (par == "") continue;
            sb.Append("<p>").Append(par).Append("</p>");
        }
        var html = sb.ToString();
        foreach (var (fr, es) in terminos)
        {
            var patron = new Regex(@"(?<![\p{L}\u0001-\u0003])" + Regex.Escape(WebUtility.HtmlEncode(fr.Trim())) + @"(?![\p{L}])", RegexOptions.IgnoreCase);
            var m = patron.Match(html);
            if (!m.Success) continue;
            marcas.Add($"<button type=\"button\" class=\"fr-glosa\" data-es=\"{WebUtility.HtmlEncode(es.Trim())}\">{m.Value}</button>");
            html = html[..m.Index] + "\u0001" + (marcas.Count - 1) + "\u0002" + html[(m.Index + m.Length)..];
        }
        return Regex.Replace(html, "\u0001(\\d+)\u0002", m => marcas[int.Parse(m.Groups[1].Value)]);
    }

    /// <summary>Compara un hueco sin mayúsculas, espacios dobles ni apóstrofos tipográficos (las tildes sí cuentan).</summary>
    public static bool Igual(string a, string b)
    {
        static string N(string s) => Regex.Replace(s.Trim().ToLowerInvariant().Replace('’', '\'').Replace('‘', '\''), @"\s+", " ").Trim('.', ' ');
        return N(a) == N(b);
    }
}

/// <summary>Cada hora comprueba si hay que encargar la siguiente píldora de francés.</summary>
public class FrancesProgramador(IServiceScopeFactory scopes, ILogger<FrancesProgramador> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var fr = scope.ServiceProvider.GetRequiredService<FrancesService>();
                await fr.Asegurar(Reloj.Hoy);
                await fr.AsegurarTraducciones();
            }
            catch (Exception e) { log.LogError(e, "No se pudo programar la píldora de francés"); }
        }
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
    }
}
