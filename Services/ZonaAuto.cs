using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>Producto que ya conocemos: para sugerir al escribir y saber en qué zona va.</summary>
public record ProductoConocido(string Nombre, Zona Zona, int Veces);

/// <summary>
/// Clasifica productos en zonas de la compra. Primero lo que ya sabemos (ingredientes de recetas, lo que se
/// añadió a mano otras semanas, los tickets); si es nuevo, por palabras clave ("manzanas" → frutería).
/// La misma tabla de palabras se manda al navegador para clasificar mientras se escribe.
/// </summary>
public class ZonaAuto(AppDb db)
{
    /// <summary>Palabras (sin acentos, en singular) que delatan la zona.</summary>
    public static readonly Dictionary<Zona, string[]> Palabras = new()
    {
        [Zona.Fruteria] = [
            "fruta", "verdura", "manzana", "pera", "platano", "banana", "naranja", "mandarina", "clementina", "limon", "lima",
            "pomelo", "fresa", "freson", "uva", "melon", "sandia", "kiwi", "pina", "melocoton", "nectarina", "paraguayo",
            "albaricoque", "ciruela", "cereza", "picota", "mango", "papaya", "aguacate", "higo", "granada", "caqui", "frambuesa",
            "arandano", "mora", "coco", "chirimoya", "tomate", "lechuga", "cebolla", "cebolleta", "chalota", "ajo", "patata",
            "zanahoria", "pimiento", "calabacin", "calabaza", "berenjena", "pepino", "puerro", "apio", "espinaca", "acelga",
            "brocoli", "coliflor", "col", "repollo", "lombarda", "alcachofa", "esparrago", "judia", "champinon", "seta",
            "guisante", "haba", "rabano", "remolacha", "nabo", "perejil", "cilantro", "albahaca", "hierbabuena", "menta",
            "rucula", "canonigo", "endibia", "escarola", "jengibre", "boniato", "batata", "chirivia", "ensalada", "brote"],
        [Zona.Charcuteria] = [
            "jamon", "queso", "chorizo", "salchichon", "fuet", "mortadela", "salami", "sobrasada", "cecina", "chopped",
            "fiambre", "bacon", "lomo embuchado", "pechuga de pavo", "fiambre de pavo", "mozzarella", "burrata", "requeson",
            "embutido", "paleta", "york", "serrano"],
        [Zona.Carniceria] = [
            "carne", "pollo", "pechuga", "muslo", "contramuslo", "alita", "ternera", "cerdo", "cordero", "conejo", "pavo",
            "filete", "chuleta", "costilla", "solomillo", "lomo", "magro", "secreto", "presa", "pluma", "entrecot",
            "hamburguesa", "albondiga", "salchicha", "morcilla", "longaniza", "butifarra", "panceta", "picada", "carrillera",
            "osobuco", "codillo", "higado", "rabo", "cochinillo", "churrasco", "pinchito", "zorza", "falda", "aguja", "babilla"],
        [Zona.Pescaderia] = [
            "pescado", "merluza", "pescadilla", "bacalao", "salmon", "atun", "bonito", "dorada", "lubina", "sardina", "boqueron",
            "rape", "lenguado", "gallo", "trucha", "caballa", "jurel", "emperador", "pez espada", "gamba", "langostino",
            "cigala", "mejillon", "almeja", "berberecho", "navaja", "calamar", "chipiron", "sepia", "pulpo", "choco", "mero",
            "rodaballo", "marisco", "bogavante", "necora", "cangrejo", "corvina", "besugo", "salmonete"],
    };

    /// <summary>Si aparece alguna, es de bote, lata o congelado: va a "Otros" aunque diga tomate o atún.</summary>
    public static readonly string[] Envasado = [
        "lata", "conserva", "frito", "zumo", "salsa", "tarro", "bote", "congelado", "congelada", "polvo", "mermelada",
        "yogur", "helado", "caldo", "pate", "chips", "sabor", "tomate triturado", "pure", "deshidratad", "seco", "seca"];

    /// <summary>Zona por palabras clave; null si no hay pista.</summary>
    public static Zona? Adivinar(string nombre)
    {
        var n = RecurrenteService.Clave(nombre);
        if (n == "") return null;
        var palabras = n.Split(' ');
        if (palabras.Any(p => Envasado.Any(e => p.StartsWith(e))) || Envasado.Any(e => e.Contains(' ') && n.Contains(e))) return null;
        // Frases de varias palabras primero ("pechuga de pavo" antes que "pechuga")
        foreach (var (z, ps) in Palabras)
            foreach (var p in ps.Where(p => p.Contains(' ')))
                if (n.StartsWith(p)) return z;
        // La primera palabra que dé pista manda: "pechuga de pollo", "queso de cabra"
        foreach (var w in palabras)
            foreach (var (z, ps) in Palabras)
                if (ps.Any(p => !p.Contains(' ') && Singulares(w).Contains(p))) return z;
        return null;
    }

    static IEnumerable<string> Singulares(string w)
    {
        yield return w;
        if (w.Length > 3 && w.EndsWith('s')) yield return w[..^1];
        if (w.Length > 4 && w.EndsWith("es")) yield return w[..^2];
    }

    /// <summary>Todo lo que ya hemos comprado o tenemos en recetas, con su zona y cuántas veces ha salido.</summary>
    public async Task<List<ProductoConocido>> Catalogo()
    {
        var res = new Dictionary<string, (string nombre, Zona? zona, int prioridad, int veces)>();
        void Sumar(string nombre, Zona? zona, int prioridad, int veces)
        {
            var k = RecurrenteService.Clave(nombre);
            if (k.Length < 2) return;
            if (res.TryGetValue(k, out var cur))
                res[k] = (prioridad >= cur.prioridad ? nombre.Trim() : cur.nombre, prioridad >= cur.prioridad && zona != null ? zona : cur.zona ?? zona,
                          Math.Max(prioridad, cur.prioridad), cur.veces + veces);
            else res[k] = (nombre.Trim(), zona, prioridad, veces);
        }
        foreach (var t in await db.TicketLineas.GroupBy(l => l.Producto).Select(g => new { g.Key, N = g.Count() }).ToListAsync())
            Sumar(t.Key, null, 0, t.N);
        foreach (var i in await db.Ingredientes.Select(i => new { i.Nombre, i.Zona }).ToListAsync())
            Sumar(i.Nombre, i.Zona, 1, 0);
        // Lo añadido a mano: la última vez manda (si se corrigió la zona, aprende)
        var manuales = await db.ListaItems.Where(l => l.Manual).OrderBy(l => l.Semana.LunesInicio).ThenBy(l => l.Id)
                               .Select(l => new { l.Nombre, l.Zona }).ToListAsync();
        foreach (var m in manuales) Sumar(m.Nombre, m.Zona, 2, 1);
        // Lo que sale en la lista por las recetas también cuenta como "comprado"
        foreach (var a in await db.ListaItems.Where(l => !l.Manual && l.Incluido).GroupBy(l => l.Nombre).Select(g => new { g.Key, N = g.Count() }).ToListAsync())
            Sumar(a.Key, null, 0, a.N);
        return res.Values.Select(v => new ProductoConocido(v.nombre, v.zona ?? Adivinar(v.nombre) ?? Zona.Otros, v.veces))
                  .OrderByDescending(p => p.Veces).ThenBy(p => p.Nombre).ToList();
    }

    /// <summary>Zona para un nombre: lo conocido primero, luego las palabras clave.</summary>
    public async Task<Zona> ZonaDe(string nombre)
    {
        var k = RecurrenteService.Clave(nombre);
        var conocido = (await Catalogo()).FirstOrDefault(p => RecurrenteService.Clave(p.Nombre) == k);
        return conocido?.Zona ?? Adivinar(nombre) ?? Zona.Otros;
    }
}
