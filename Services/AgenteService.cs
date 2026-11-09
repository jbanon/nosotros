using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Agente de la casa: lanza <c>claude -p</c> en segundo plano para crear fichas de películas y recetas
/// (a partir de un nombre o de una foto), con rondas de cambios pedidas por el usuario.
/// Cola en memoria; las tareas pendientes se retoman al arrancar.
/// </summary>
public class AgenteService(IServiceScopeFactory scopes, IConfiguration cfg, FileStorage files, IHttpClientFactory http, ILogger<AgenteService> log) : BackgroundService
{
    readonly Channel<int> cola = Channel.CreateUnbounded<int>();
    static readonly JsonSerializerOptions JsonOpc = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    public string? RutaClaude { get; } = BuscarClaude(cfg["Agente:Claude"]);
    public bool Disponible => RutaClaude != null;
    public string Modelo => cfg["Agente:Modelo"] ?? "sonnet";
    public string Esfuerzo => cfg["Agente:Esfuerzo"] ?? "medium";
    int MaxTurnos => int.TryParse(cfg["Agente:MaxTurnos"], out var n) ? n : 10;
    int TimeoutSeg => int.TryParse(cfg["Agente:TimeoutSegundos"], out var n) ? n : 240;

    static string? BuscarClaude(string? configurado)
    {
        if (!string.IsNullOrWhiteSpace(configurado)) return File.Exists(configurado) ? configurado : null;
        var home = Environment.GetEnvironmentVariable("HOME") ?? "";
        var candidatos = new List<string> { Path.Combine(home, ".local/bin/claude"), "/usr/local/bin/claude", "/usr/bin/claude", "/opt/love/bin/claude" };
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries))
            candidatos.Add(Path.Combine(dir, "claude"));
        return candidatos.FirstOrDefault(File.Exists);
    }

    // ---------- API para las páginas ----------
    public async Task<TareaAgente> Encolar(AppDb db, TareaAgente t)
    {
        db.TareasAgente.Add(t);
        await db.SaveChangesAsync();
        cola.Writer.TryWrite(t.Id);
        return t;
    }

    /// <summary>Nueva ronda: el usuario pide un cambio sobre la propuesta actual.</summary>
    public async Task Cambiar(AppDb db, TareaAgente t, string texto)
    {
        var h = Historial(t);
        h.Add(new JsonObject { ["rol"] = "usuario", ["texto"] = texto.Trim(), ["fecha"] = DateTime.UtcNow });
        t.Historial = h.ToJsonString(JsonOpc);
        t.Estado = EstadoTarea.Pendiente; t.Error = null;
        await db.SaveChangesAsync();
        cola.Writer.TryWrite(t.Id);
    }

    public async Task Reintentar(AppDb db, TareaAgente t)
    {
        t.Estado = EstadoTarea.Pendiente; t.Error = null;
        await db.SaveChangesAsync();
        cola.Writer.TryWrite(t.Id);
    }

    public static JsonArray Historial(TareaAgente t)
    {
        try { return JsonNode.Parse(string.IsNullOrWhiteSpace(t.Historial) ? "[]" : t.Historial) as JsonArray ?? new JsonArray(); }
        catch { return new JsonArray(); }
    }

    public static JsonObject? Propuesta(TareaAgente t)
    {
        try { return string.IsNullOrWhiteSpace(t.Propuesta) ? null : JsonNode.Parse(t.Propuesta) as JsonObject; }
        catch { return null; }
    }

    // ---------- Bucle de fondo ----------
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!Disponible) { log.LogWarning("Agente desactivado: no se encuentra el binario claude (Agente:Claude)"); }
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            foreach (var id in await db.TareasAgente.Where(t => t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnCurso).Select(t => t.Id).ToListAsync(ct))
                cola.Writer.TryWrite(id);
        }
        catch (Exception e) { log.LogError(e, "No se pudieron recuperar las tareas pendientes del agente"); }

        await foreach (var id in cola.Reader.ReadAllAsync(ct))
        {
            try { await Procesar(id, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) { log.LogError(e, "Fallo procesando la tarea {Id} del agente", id); }
        }
    }

    async Task Procesar(int id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var t = await db.TareasAgente.FindAsync([id], ct);
        if (t == null || t.Estado == EstadoTarea.Lista) return;
        if (!Disponible) { t.Estado = EstadoTarea.Error; t.Error = "El agente no está configurado en este servidor (falta el binario claude)."; await db.SaveChangesAsync(ct); return; }

        t.Estado = EstadoTarea.EnCurso; t.Error = null;
        await db.SaveChangesAsync(ct);

        var raciones = int.TryParse((await db.Configs.FindAsync([ConfigClaves.RacionesLista], ct))?.Valor, out var r) ? r : 12;
        var maestro = await db.Ingredientes.OrderBy(i => i.Nombre).Select(i => i.Nombre).ToListAsync(ct);
        var (prompt, esquema, herramientas, dirExtra) = t.Tipo switch
        {
            TipoTareaAgente.FichaPelicula => (PromptPelicula(t), EsquemaPelicula, "WebSearch,WebFetch", (string?)null),
            TipoTareaAgente.Receta => (PromptReceta(t, raciones, maestro, null), EsquemaReceta, "", null),
            TipoTareaAgente.RecetaFoto => (PromptReceta(t, raciones, maestro, files.RutaAbsoluta(t.FotoPath!)), EsquemaReceta, "Read", Path.GetDirectoryName(files.RutaAbsoluta(t.FotoPath!))),
            _ => throw new InvalidOperationException()
        };

        var sw = Stopwatch.StartNew();
        var res = await Ejecutar(prompt, esquema, herramientas, dirExtra, ct);
        sw.Stop();
        t.DuracionMs += (int)sw.ElapsedMilliseconds; t.CosteUsd += res.Coste; t.Rondas++;
        if (res.Error != null || res.Salida == null)
        {
            t.Estado = EstadoTarea.Error; t.Error = res.Error ?? "El agente no devolvió una respuesta válida."; t.Terminada = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            log.LogWarning("Agente: tarea {Id} con error: {Error}", t.Id, t.Error);
            return;
        }

        t.Propuesta = res.Salida.ToJsonString(JsonOpc);
        var h = Historial(t);
        var mensaje = res.Salida["mensaje"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(mensaje)) h.Add(new JsonObject { ["rol"] = "agente", ["texto"] = mensaje, ["fecha"] = DateTime.UtcNow });
        t.Historial = h.ToJsonString(JsonOpc);
        t.Estado = EstadoTarea.Lista; t.Terminada = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (t.Tipo == TipoTareaAgente.FichaPelicula && t.PeliculaId != null)
            await AplicarFicha(db, t, res.Salida, ct);
        log.LogInformation("Agente: tarea {Id} ({Tipo}) lista en {Ms} ms", t.Id, t.Tipo, sw.ElapsedMilliseconds);
    }

    /// <summary>La ficha de película se aplica directamente (el usuario puede pedir cambios después).</summary>
    async Task AplicarFicha(AppDb db, TareaAgente t, JsonObject f, CancellationToken ct)
    {
        var p = await db.Peliculas.FindAsync([t.PeliculaId!.Value], ct);
        if (p == null) return;
        string? S(string k) => f[k]?.GetValue<string>() is { } v && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
        int? I(string k) => f[k] is JsonValue v && v.TryGetValue<int>(out var n) && n > 0 ? n : null;
        var titulo = S("titulo");
        if (titulo != null) p.Titulo = titulo;
        p.TituloOriginal = S("tituloOriginal"); p.Anio = I("anio"); p.Director = S("director"); p.Reparto = S("reparto");
        p.Genero = S("genero"); p.Duracion = I("duracionMin"); p.Sinopsis = S("sinopsis"); p.Curiosidad = S("curiosidad");
        p.FichaEl = DateTime.UtcNow;
        // Póster: primero el que proponga el agente; si no, lo buscamos en Wikipedia (en, luego es).
        var candidatos = new List<string>();
        if (S("posterUrl") is { } u) candidatos.Add(u);
        var busqueda = $"{p.TituloOriginal ?? p.Titulo} {p.Anio}";
        foreach (var idioma in new[] { "en", "es" })
            if (await PosterWikipedia(idioma, busqueda + (idioma == "en" ? " film" : " película"), ct) is { } w) candidatos.Add(w);
        foreach (var url in candidatos.Distinct())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) continue;
            var rel = await DescargarImagen(uri, "posters", ct);
            if (rel != null) { files.Borrar(p.PosterPath); p.PosterPath = rel; break; }
        }
        t.Aplicada = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    HttpClient Cliente()
    {
        var cli = http.CreateClient();
        cli.Timeout = TimeSpan.FromSeconds(20);
        cli.DefaultRequestHeaders.UserAgent.ParseAdd("NosotrosApp/1.0 (app familiar privada)");
        return cli;
    }

    async Task<string?> PosterWikipedia(string idioma, string busqueda, CancellationToken ct)
    {
        try
        {
            var url = $"https://{idioma}.wikipedia.org/w/api.php?action=query&prop=pageimages&format=json&pithumbsize=600&pilicense=any&generator=search&gsrlimit=1&gsrsearch={Uri.EscapeDataString(busqueda)}";
            using var doc = JsonDocument.Parse(await Cliente().GetStringAsync(url, ct));
            if (!doc.RootElement.TryGetProperty("query", out var q) || !q.TryGetProperty("pages", out var pages)) return null;
            foreach (var pg in pages.EnumerateObject())
                if (pg.Value.TryGetProperty("thumbnail", out var th) && th.TryGetProperty("source", out var src)) return src.GetString();
        }
        catch (Exception e) { log.LogInformation("Wikipedia ({Idioma}) sin póster para {Busqueda}: {Msg}", idioma, busqueda, e.Message); }
        return null;
    }

    async Task<string?> DescargarImagen(Uri uri, string subcarpeta, CancellationToken ct)
    {
        try
        {
            using var resp = await Cliente().GetAsync(uri, ct);
            var mime = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!resp.IsSuccessStatusCode || !mime.StartsWith("image/") || resp.Content.Headers.ContentLength is > 8_000_000) return null;
            var ext = mime switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
            var rel = subcarpeta + "/" + Guid.NewGuid().ToString("N") + ext;
            Directory.CreateDirectory(Path.Combine(files.Raiz, subcarpeta));
            await using var fs = File.Create(Path.Combine(files.Raiz, rel));
            await resp.Content.CopyToAsync(fs, ct);
            return rel;
        }
        catch (Exception e) { log.LogInformation("Imagen no descargada ({Url}): {Msg}", uri, e.Message); return null; }
    }

    // ---------- Ejecución de claude -p ----------
    public record Resultado(JsonObject? Salida, decimal Coste, string? Error);

    async Task<Resultado> Ejecutar(string prompt, string esquema, string herramientas, string? dirExtra, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(RutaClaude!)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, UseShellExecute = false,
            WorkingDirectory = Path.Combine(files.Raiz, "agente"),
        };
        Directory.CreateDirectory(psi.WorkingDirectory);
        foreach (var a in new[] { "-p", prompt, "--model", Modelo, "--effort", Esfuerzo, "--output-format", "json", "--json-schema", esquema,
                                  "--no-session-persistence", "--max-turns", MaxTurnos.ToString(), "--permission-mode", "dontAsk" })
            psi.ArgumentList.Add(a);
        if (!string.IsNullOrEmpty(herramientas)) { psi.ArgumentList.Add("--allowedTools"); psi.ArgumentList.Add(herramientas); }
        else { psi.ArgumentList.Add("--disallowedTools"); psi.ArgumentList.Add("Bash,Edit,Write,Read,WebSearch,WebFetch,Agent"); }
        if (dirExtra != null) { psi.ArgumentList.Add("--add-dir"); psi.ArgumentList.Add(dirExtra); }
        // Si la app se lanzó desde una sesión de Claude Code, estas variables confunden al CLI hijo.
        psi.Environment.Remove("CLAUDECODE"); psi.Environment.Remove("CLAUDE_CODE_ENTRYPOINT");
        // Siempre con la suscripción de Javier (sesión / token de `claude setup-token`), nunca con la API de pago.
        psi.Environment.Remove("ANTHROPIC_API_KEY"); psi.Environment.Remove("ANTHROPIC_AUTH_TOKEN");
        var token = cfg["Agente:Token"];
        if (!string.IsNullOrWhiteSpace(token)) psi.Environment["CLAUDE_CODE_OAUTH_TOKEN"] = token;

        using var proc = new Process { StartInfo = psi };
        try { proc.Start(); }
        catch (Exception e) { return new(null, 0, "No se pudo lanzar claude: " + e.Message); }
        proc.StandardInput.Close();
        var salida = proc.StandardOutput.ReadToEndAsync(ct);
        var error = proc.StandardError.ReadToEndAsync(ct);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeg));
        try { await proc.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException)
        {
            try { proc.Kill(true); } catch { }
            return new(null, 0, ct.IsCancellationRequested ? "Cancelado" : $"El agente tardó más de {TimeoutSeg} s y se canceló.");
        }
        var txt = await salida; var err = await error;
        if (string.IsNullOrWhiteSpace(txt))
            return new(null, 0, "claude no devolvió nada" + (string.IsNullOrWhiteSpace(err) ? "" : ": " + Recortar(err)));
        JsonObject? raiz;
        try { raiz = JsonNode.Parse(txt) as JsonObject; }
        catch { return new(null, 0, "Respuesta no es JSON: " + Recortar(txt)); }
        if (raiz == null) return new(null, 0, "Respuesta vacía");
        decimal coste = raiz["total_cost_usd"] is JsonValue c && c.TryGetValue<double>(out var d) ? (decimal)d : 0;
        if (raiz["is_error"]?.GetValue<bool>() == true)
            return new(null, coste, "Error del agente: " + Recortar(raiz["result"]?.ToString() ?? raiz["subtype"]?.ToString() ?? err));
        var so = raiz["structured_output"] as JsonObject;
        if (so == null && raiz["result"]?.GetValue<string>() is { } res)
        {
            try { so = JsonNode.Parse(res) as JsonObject; } catch { }
        }
        if (so == null) return new(null, coste, "El agente no devolvió la ficha estructurada" + (raiz["subtype"] is { } st ? $" ({st})" : "") + ".");
        // Copia independiente (el nodo no puede tener dos padres)
        return new(JsonNode.Parse(so.ToJsonString()) as JsonObject, coste, null);
    }

    static string Recortar(string s) => s.Length > 300 ? s[..300] + "…" : s;

    // ---------- Prompts y esquemas ----------
    const string Contexto = "Eres el asistente de «Nosotros», una app privada de una familia española de siete (Javi, Raquel y cinco hijos) y su perra. Escribe siempre en castellano de España, con tono cercano y breve. Devuelve únicamente el JSON pedido.";

    static string Conversacion(TareaAgente t)
    {
        var h = Historial(t);
        if (h.Count == 0) return "";
        var lineas = h.Select(m => $"- {(m!["rol"]?.GetValue<string>() == "usuario" ? "Usuario" : "Tú")}: {m["texto"]}");
        return "\n\nConversación hasta ahora:\n" + string.Join("\n", lineas);
    }

    static string Ronda(TareaAgente t, string queEs)
    {
        if (string.IsNullOrWhiteSpace(t.Propuesta)) return "";
        var ultimo = Historial(t).LastOrDefault(m => m!["rol"]?.GetValue<string>() == "usuario")?["texto"]?.GetValue<string>() ?? "";
        return $"\n\nYa propusiste {queEs} (JSON):\n{t.Propuesta}{Conversacion(t)}\n\nEl usuario pide ahora: «{ultimo}». Devuelve {queEs} COMPLETA actualizada con el mismo esquema, aplicando solo los cambios pedidos y manteniendo el resto, y explica en «mensaje» qué has cambiado (una o dos frases).";
    }

    static string PromptPelicula(TareaAgente t) =>
        $"{Contexto}\n\nCrea una mini ficha de la película «{t.Entrada}»{(string.IsNullOrWhiteSpace(t.Notas) ? "" : $" (nota del usuario: {t.Notas})")}. " +
        "Si no estás seguro de los datos o hay varias películas con ese título, usa WebSearch para confirmarlos. Campos: titulo (título con el que se estrenó en España), tituloOriginal, anio, director, reparto (3 o 4 nombres separados por comas), genero (dos o tres palabras), duracionMin, sinopsis (3 o 4 frases, sin destripar el final), curiosidad (una frase curiosa sobre la película), posterUrl (déjala vacía salvo que ya conozcas una URL directa a la imagen del póster; la app lo busca por su cuenta), mensaje (una frase para el usuario sobre lo que has encontrado)." +
        (string.IsNullOrWhiteSpace(t.Propuesta) ? "" : Ronda(t, "la ficha"));

    static string PromptReceta(TareaAgente t, int raciones, List<string> maestro, string? rutaFoto)
    {
        var origen = rutaFoto == null
            ? $"Escribe la receta «{t.Entrada}»{(string.IsNullOrWhiteSpace(t.Notas) ? "" : $". Indicaciones del usuario: {t.Notas}")}."
            : $"Lee la imagen {rutaFoto} con la herramienta Read. Contiene una receta (puede estar en inglés u otro idioma, en una página de libro, una captura o una nota a mano). Transcríbela fielmente y adáptala al castellano: traduce nombres de ingredientes a los usados en España, convierte unidades imperiales a métricas (cups, oz, lb, °F → g, ml, °C) y redondea con sentido. Si la imagen no contiene una receta legible, deja «nombre» vacío y explica en «mensaje» qué ves.{(string.IsNullOrWhiteSpace(t.Notas) ? "" : $" Indicaciones del usuario: {t.Notas}.")}";
        return $"{Contexto}\n\n{origen}\n\n" +
            $"La receta es para {raciones} raciones (somos una familia grande): escala las cantidades a {raciones} raciones salvo que el usuario diga otra cosa, y pon ese número en «raciones». " +
            "Campos: nombre; descripcion (una frase apetecible); raciones; etiquetas (dos a cuatro palabras separadas por comas, por ejemplo: comida, cena, rápido, legumbres, pescado, pasta, arroz, horno, postre); " +
            "ingredientes: lista con nombre (singular, sencillo, sin cantidades ni adjetivos innecesarios: «cebolla», «pechuga de pollo», «aceite de oliva»), cantidad (número o null si es «al gusto»), unidad (g, kg, ml, l, ud, cucharada, cucharadita, diente, hoja, pizca, rama, lata, manojo…; vacía si no aplica), zona de compra (Fruteria: fruta, verdura y hortalizas frescas; Charcuteria: embutidos, quesos y fiambres; Carniceria: carnes y aves; Pescaderia: pescado y marisco; Otros: despensa, lácteos, huevos, pan, congelados, bebidas) y nota (opcional, por ejemplo «picada» o «sin piel»; vacía si no hace falta); " +
            "preparacion (pasos numerados «1. …», uno por línea, claros y completos); mensaje (una frase para el usuario). " +
            (maestro.Count > 0 ? $"Cuando un ingrediente ya exista en la despensa de la app, usa exactamente ese nombre. Nombres que ya existen: {string.Join(", ", maestro)}." : "") +
            (string.IsNullOrWhiteSpace(t.Propuesta) ? "" : Ronda(t, "la receta"));
    }

    public const string EsquemaPelicula = """
        {"type":"object","additionalProperties":false,
         "properties":{"titulo":{"type":"string"},"tituloOriginal":{"type":"string"},"anio":{"type":"integer"},"director":{"type":"string"},
                       "reparto":{"type":"string"},"genero":{"type":"string"},"duracionMin":{"type":"integer"},"sinopsis":{"type":"string"},
                       "curiosidad":{"type":"string"},"posterUrl":{"type":"string"},"mensaje":{"type":"string"}},
         "required":["titulo","tituloOriginal","anio","director","reparto","genero","duracionMin","sinopsis","curiosidad","posterUrl","mensaje"]}
        """;

    public const string EsquemaReceta = """
        {"type":"object","additionalProperties":false,
         "properties":{"nombre":{"type":"string"},"descripcion":{"type":"string"},"raciones":{"type":"integer"},"etiquetas":{"type":"string"},
                       "ingredientes":{"type":"array","items":{"type":"object","additionalProperties":false,
                           "properties":{"nombre":{"type":"string"},"cantidad":{"type":["number","null"]},"unidad":{"type":"string"},
                                         "zona":{"type":"string","enum":["Fruteria","Charcuteria","Carniceria","Pescaderia","Otros"]},"nota":{"type":"string"}},
                           "required":["nombre","cantidad","unidad","zona","nota"]}},
                       "preparacion":{"type":"string"},"mensaje":{"type":"string"}},
         "required":["nombre","descripcion","raciones","etiquetas","ingredientes","preparacion","mensaje"]}
        """;

    /// <summary>Convierte la propuesta del agente en una receta de la app (reutilizando ingredientes existentes).</summary>
    public static async Task<Receta> CrearReceta(AppDb db, JsonObject p)
    {
        var maestro = await db.Ingredientes.ToListAsync();
        var r = new Receta
        {
            Nombre = (p["nombre"]?.GetValue<string>() ?? "Receta").Trim(),
            Descripcion = p["descripcion"]?.GetValue<string>()?.Trim(),
            Raciones = p["raciones"] is JsonValue rv && rv.TryGetValue<int>(out var n) && n > 0 ? n : 12,
            Etiquetas = p["etiquetas"]?.GetValue<string>()?.Trim(),
            Preparacion = p["preparacion"]?.GetValue<string>()?.Trim(),
        };
        int orden = 0;
        foreach (var i in p["ingredientes"] as JsonArray ?? new JsonArray())
        {
            var nombre = i?["nombre"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(nombre)) continue;
            var zona = Enum.TryParse<Zona>(i!["zona"]?.GetValue<string>(), true, out var z) ? z : Zona.Otros;
            var ing = maestro.FirstOrDefault(x => string.Equals(x.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            if (ing == null) { ing = new Ingrediente { Nombre = nombre, Zona = zona }; db.Ingredientes.Add(ing); maestro.Add(ing); }
            decimal? cant = i["cantidad"] is JsonValue cv && cv.TryGetValue<double>(out var d) ? (decimal)Math.Round(d, 2) : null;
            var unidad = i["unidad"]?.GetValue<string>()?.Trim(); var nota = i["nota"]?.GetValue<string>()?.Trim();
            r.Ingredientes.Add(new RecetaIngrediente { Ingrediente = ing, Cantidad = cant, Unidad = string.IsNullOrEmpty(unidad) ? null : unidad, Nota = string.IsNullOrEmpty(nota) ? null : nota, Orden = orden++ });
        }
        db.Recetas.Add(r);
        await db.SaveChangesAsync();
        return r;
    }
}
